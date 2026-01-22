using UnityEngine;

public class CameraZoom : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private RomanBefore romanBefore;

    [Header("Camera Positions")]
    [Tooltip("ตำแหน่งตอน eventPos เป็นเลขคี่")]
    [SerializeField] private Transform positionOdd;   // ตำแหน่ง 1
    [Tooltip("ตำแหน่งตอน eventPos เป็นเลขคู่")]
    [SerializeField] private Transform positionEven;  // ตำแหน่ง 2
    [Tooltip("ตำแหน่งตอน eventPos = 0")]
    [SerializeField] private Transform positionZero;  // ตำแหน่ง 3

    [Header("Zoom (Orthographic Size)")]
    [SerializeField] private float zoomOdd = 3.2f;
    [SerializeField] private float zoomEven = 3.2f;
    [SerializeField] private float zoomZero = 4.5f;

    [Header("Smooth Settings")]
    [SerializeField] private float moveSmooth = 10f;
    [SerializeField] private float zoomSmooth = 10f;

    private Camera cam;

    private int lastEventPos = int.MinValue;
    private Transform targetPos;
    private float targetZoom;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        if (romanBefore == null)
            romanBefore = FindObjectOfType<RomanBefore>();

        // ตั้งค่าตาม eventPos ตอนเริ่มเกมทันที
        if (romanBefore != null)
        {
            lastEventPos = romanBefore.eventPos;
            ApplyRule(lastEventPos, immediate: true);
        }
    }

    private void Update()
    {
        if (romanBefore == null) return;

        // ✅ ทำงานเมื่อ eventPos เปลี่ยนเท่านั้น
        if (romanBefore.eventPos != lastEventPos)
        {
            lastEventPos = romanBefore.eventPos;
            ApplyRule(lastEventPos, immediate: false);
        }

        // ✅ Smooth move & zoom
        SmoothFollow();
    }

    private void ApplyRule(int eventPos, bool immediate)
    {
        if (eventPos == 0)
        {
            targetPos = positionZero;
            targetZoom = zoomZero;
        }
        else if (eventPos % 2 != 0) // เลขคี่
        {
            targetPos = positionOdd;
            targetZoom = zoomOdd;
        }
        else // เลขคู่
        {
            targetPos = positionEven;
            targetZoom = zoomEven;
        }

        if (immediate)
        {
            if (targetPos != null)
                transform.position = new Vector3(targetPos.position.x, targetPos.position.y, transform.position.z);

            if (cam != null)
                cam.orthographicSize = targetZoom;
        }
    }

    private void SmoothFollow()
    {
        if (targetPos != null)
        {
            Vector3 desired = new Vector3(targetPos.position.x, targetPos.position.y, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, desired, moveSmooth * Time.unscaledDeltaTime);
        }

        if (cam != null)
        {
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, zoomSmooth * Time.unscaledDeltaTime);
        }
    }
}
