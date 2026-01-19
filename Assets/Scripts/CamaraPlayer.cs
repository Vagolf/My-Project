using System.Collections;
using UnityEngine;

public class CamaraPlayer : MonoBehaviour
{
    [Header("Ultimate Focus Settings")] public Vector3 focusOffset = new Vector3(0, 0, -10f); // position relative to player during ultimate
    [Min(0.1f)] public float zoomSizeDuringUltimate = 2.5f; // orthographic size (if orthographic)
    public float moveInTime = 0.35f;
    public float moveOutTime = 0.4f;
    public bool instantZoomIn = false; // ถ้าเปิดจะซูมเข้าและเลื่อนไปหาผู้เล่นทันที

    [Header("Perspective FOV")] public float focusFOV = 80f; // FOV to apply before moving (perspective only)
    public bool restoreFOVSmooth = true;
    public float fovRestoreTime = 0.4f;

    [Header("Player Acquire")] public bool autoFindPlayer = true; // หา player อัตโนมัติเมื่อเริ่มอัลติ
    public bool followDuringUltimate = true; // follow only while ultimate until damage event

    private Camera cam;
    private Vector3 originalPosition;
    private float originalSize;
    private float originalFOV;
    private bool inUltimateSequence;
    private bool trackingCurrentUltimate;
    private Transform currentUltimateTarget;
    private Coroutine transitionRoutine;
    private Coroutine fovRestoreRoutine;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        originalPosition = transform.position;
        if (cam != null)
        {
            if (cam.orthographic)
                originalSize = cam.orthographicSize;
            else
                originalFOV = cam.fieldOfView;
        }
    }

    private void OnEnable()
    {
        UltimateEventBus.OnUltimateStart += HandleUltimateStart;
        UltimateEventBus.OnUltimateDamage += HandleUltimateDamage; // new: restore when damage frame happens
        UltimateEventBus.OnUltimateFinish += HandleUltimateFinish; // still restore in case finish without damage
    }

    private void OnDisable()
    {
        UltimateEventBus.OnUltimateStart -= HandleUltimateStart;
        UltimateEventBus.OnUltimateDamage -= HandleUltimateDamage;
        UltimateEventBus.OnUltimateFinish -= HandleUltimateFinish;
    }

    private void LateUpdate()
    {
        if (autoFindPlayer && currentUltimateTarget == null)
        {
            // fallback: follow active character tagged Player if needed
            var p = CharacterSpawnManager.PlayerInstance;
            if (p != null && p.activeInHierarchy)
                currentUltimateTarget = p.transform;
        }
        if (trackingCurrentUltimate && followDuringUltimate && currentUltimateTarget != null)
        {
            // While focusing (before damage event) keep centering
            Vector3 basePos = currentUltimateTarget.position + focusOffset;
            transform.position = Vector3.Lerp(transform.position, basePos, 15f * Time.unscaledDeltaTime);
        }
    }

    private void HandleUltimateStart(Transform t)
    {
        Debug.Log($"[Camera] UltimateStart from {t?.name}");
        currentUltimateTarget = t;
        trackingCurrentUltimate = followDuringUltimate; // enable follow until damage

        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        if (fovRestoreRoutine != null) StopCoroutine(fovRestoreRoutine);

        if (cam != null && !cam.orthographic)
        {
            cam.fieldOfView = focusFOV;
        }

        if (instantZoomIn)
        {
            inUltimateSequence = true;
            Vector3 targetPos = (t ? t.position : transform.position) + focusOffset;
            transform.position = targetPos;
            if (cam.orthographic)
                cam.orthographicSize = zoomSizeDuringUltimate;
        }
        else
        {
            transitionRoutine = StartCoroutine(FocusToPlayer(t));
        }
    }

    private void HandleUltimateDamage(Transform t)
    {
        Debug.Log($"[Camera] UltimateDamage from {t?.name}");
        if (t != currentUltimateTarget) return; // ignore if other target's ultimate
        trackingCurrentUltimate = false; // stop following immediately
        // Start restore immediately
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(RestoreCamera());
        RestoreFOVIfNeeded();
    }

    private void HandleUltimateFinish(Transform t)
    {
        Debug.Log($"[Camera] UltimateFinish from {t?.name}");
        if (t != currentUltimateTarget) return;
        // If already restoring from damage event skip duplicate
        if (!inUltimateSequence) return;
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(RestoreCamera());
        RestoreFOVIfNeeded();
    }

    private void RestoreFOVIfNeeded()
    {
        if (cam != null && !cam.orthographic)
        {
            if (restoreFOVSmooth)
            {
                if (fovRestoreRoutine != null) StopCoroutine(fovRestoreRoutine);
                fovRestoreRoutine = StartCoroutine(RestoreFOV());
            }
            else
            {
                cam.fieldOfView = originalFOV;
            }
        }
    }

    private IEnumerator FocusToPlayer(Transform t)
    {
        inUltimateSequence = true;
        Vector3 startPos = transform.position;
        float startSize = cam.orthographic ? cam.orthographicSize : 0f;
        Vector3 targetPos = (t ? t.position : startPos) + focusOffset;
        float progress = 0f;
        float dur = Mathf.Max(0.0001f, moveInTime);
        while (progress < 1f)
        {
            progress += Time.unscaledDeltaTime / dur; // timeScale = 0 ระหว่างอัลติ
            float ease = Mathf.SmoothStep(0f, 1f, progress);
            transform.position = Vector3.Lerp(startPos, targetPos, ease);
            if (cam.orthographic)
                cam.orthographicSize = Mathf.Lerp(startSize, zoomSizeDuringUltimate, ease);
            yield return null;
        }
        transform.position = targetPos;
        if (cam.orthographic)
            cam.orthographicSize = zoomSizeDuringUltimate;
    }

    private IEnumerator RestoreCamera()
    {
        Vector3 startPos = transform.position;
        float startSize = cam.orthographic ? cam.orthographicSize : 0f;
        Vector3 endPos = originalPosition;
        float endSize = originalSize;
        float t = 0f;
        float dur = Mathf.Max(0.0001f, moveOutTime);
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            float ease = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector3.Lerp(startPos, endPos, ease);
            if (cam.orthographic)
                cam.orthographicSize = Mathf.Lerp(startSize, endSize, ease);
            yield return null;
        }
        transform.position = endPos;
        if (cam.orthographic)
            cam.orthographicSize = endSize;
        inUltimateSequence = false;
        currentUltimateTarget = null;
    }

    private IEnumerator RestoreFOV()
    {
        float start = cam.fieldOfView;
        float end = originalFOV;
        float t = 0f;
        float dur = Mathf.Max(0.0001f, fovRestoreTime);
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            float ease = Mathf.SmoothStep(0f, 1f, t);
            cam.fieldOfView = Mathf.Lerp(start, end, ease);
            yield return null;
        }
        cam.fieldOfView = end;
    }
}
