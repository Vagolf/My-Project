using System.Collections;
using UnityEngine;

public class ToggleOnHitCooldown : MonoBehaviour
{
    [Header("Targets to toggle")]
    [Tooltip("GameObjects that will be set active/inactive. Leave empty to control this object's children.")]
    [SerializeField] private GameObject[] targets;

    [Header("Source (Health)")]
    [Tooltip("Listen to HealthCh crouch-block events on this object. If null, GetComponent will be used.")]
    [SerializeField] private HealthCh healthSource;

    [Header("Options")]
    [Tooltip("If true, will search immediate children as targets when 'targets' is empty")]
    [SerializeField] private bool fallbackToChildrenIfEmpty = true;

    private int nextDisableIndex; // pointer to next target to disable on use
    private int nextEnableIndex;  // pointer to next target to enable on restore

    private void Awake()
    {
        // Protect against disabling self (would cancel coroutine). Warn and ignore self.
        if (targets != null)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == gameObject)
                {
                    Debug.LogWarning($"[ToggleOnHitCooldown] Target contains this GameObject. Ignoring to avoid disabling the script host.", this);
                    targets[i] = null;
                }
            }
        }
    }

    private void Start()
    {
        if ((targets == null || targets.Length == 0) && fallbackToChildrenIfEmpty)
        {
            var list = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in transform)
                list.Add(child.gameObject);
            targets = list.ToArray();
        }

        if (healthSource == null)
            healthSource = GetComponent<HealthCh>();
        if (healthSource != null)
        {
            healthSource.OnCrouchBlockUsed += HandleBlockUsed;
            healthSource.OnCrouchBlockRestored += HandleBlockRestored;
        }

        // initialize indices based on current active states
        nextDisableIndex = 0;
        nextEnableIndex = 0;
    }

    private void OnDestroy()
    {
        if (healthSource != null)
        {
            healthSource.OnCrouchBlockUsed -= HandleBlockUsed;
            healthSource.OnCrouchBlockRestored -= HandleBlockRestored;
        }
    }

    private void HandleBlockUsed(int available, int max)
    {
        // disable next active target in sequence
        if (targets == null || targets.Length == 0) return;
        // find next active from pointer
        for (int i = 0; i < targets.Length; i++)
        {
            int idx = (nextDisableIndex + i) % targets.Length;
            var go = targets[idx];
            if (go == null) continue;
            if (go.activeSelf)
            {
                go.SetActive(false);
                nextDisableIndex = (idx + 1) % targets.Length;
                // ensure enable pointer starts from this index for restore order
                nextEnableIndex = idx;
                break;
            }
        }
    }

    private void HandleBlockRestored(int available, int max)
    {
        // enable next inactive target in sequence
        if (targets == null || targets.Length == 0) return;
        for (int i = 0; i < targets.Length; i++)
        {
            int idx = (nextEnableIndex + i) % targets.Length;
            var go = targets[idx];
            if (go == null) continue;
            if (!go.activeSelf)
            {
                go.SetActive(true);
                nextEnableIndex = (idx + 1) % targets.Length;
                break;
            }
        }
    }

    private void SetTargetsActive(bool value)
    {
        if (targets == null || targets.Length == 0) return;
        foreach (var go in targets)
        {
            if (go == null) continue;
            go.SetActive(value);
        }
    }
}
