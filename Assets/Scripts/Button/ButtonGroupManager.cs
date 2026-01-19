using UnityEngine;
using System.Collections.Generic;

// Generic group manager ????????????? implement ISelectableButton
public class ButtonGroupManager : MonoBehaviour
{
    [Tooltip("Buttons that belong to this group. If empty and Auto Register is on, children will be registered automatically.")]
    public List<ISelectableButton> buttons = new List<ISelectableButton>();

    [Header("Setup")]
    [SerializeField] private bool autoRegisterChildren = true;
    [SerializeField] private bool includeInactiveChildren = true;

    // Internal map of groupKey -> buttons in that group
    private readonly Dictionary<string, List<ISelectableButton>> _groups = new Dictionary<string, List<ISelectableButton>>();
    // Track the currently selected button per group
    private readonly Dictionary<string, ISelectableButton> _selectedByGroup = new Dictionary<string, ISelectableButton>();

    private void Awake()
    {
        if (autoRegisterChildren)
        {
            buttons.Clear();
            // Find both ButtonTextColor and ButtonColor components
            var textButtons = GetComponentsInChildren<ButtonTextColor>(includeInactiveChildren);
            var colorButtons = GetComponentsInChildren<ButtonColor>(includeInactiveChildren);

            foreach (var b in textButtons)
            {
                if (b == null) continue;
                if (!buttons.Contains(b)) buttons.Add(b);
                b.groupManager = GetComponent<ButtonGroupColorManager>(); // Use specific manager for ButtonTextColor
                EnsureGroupMembership(b);
            }

            foreach (var b in colorButtons)
            {
                if (b == null) continue;
                if (!buttons.Contains(b)) buttons.Add(b);
                b.groupManager = this;
                EnsureGroupMembership(b);
            }
        }
    }

    public void AddButton(ISelectableButton btn)
    {
        if (btn == null) return;
        if (!buttons.Contains(btn)) buttons.Add(btn);
        
        // Set appropriate manager based on type
        if (btn is ButtonColor colorBtn)
            colorBtn.groupManager = this;
        else if (btn is ButtonTextColor textBtn)
            textBtn.groupManager = GetComponent<ButtonGroupColorManager>();
            
        EnsureGroupMembership(btn);
    }

    public void RemoveButton(ISelectableButton btn)
    {
        if (btn == null) return;
        buttons.Remove(btn);
        
        // Clear manager reference
        if (btn is ButtonColor colorBtn && colorBtn.groupManager == this)
            colorBtn.groupManager = null;
        else if (btn is ButtonTextColor textBtn && textBtn.groupManager != null)
            textBtn.groupManager = null;
            
        // Remove from group map
        string key = ResolveGroupKey(btn);
        if (!string.IsNullOrEmpty(key) && _groups.TryGetValue(key, out var list))
        {
            list.Remove(btn);
        }
    }

    // Commit selection for a button within its group
    public void OnButtonChosen(ISelectableButton selected)
    {
        // Reset only buttons inside the same group as 'selected'
        string key = ResolveGroupKey(selected);
        if (!string.IsNullOrEmpty(key) && _groups.TryGetValue(key, out var list))
        {
            foreach (var btn in list)
            {
                if (btn != null && btn != selected)
                    btn.SetToNormal();
            }
            _selectedByGroup[key] = selected;
            return;
        }
        // Fallback: if no group, operate on whole list
        foreach (var btn in buttons)
        {
            if (btn != null && btn != selected)
                btn.SetToNormal();
        }
    }

    // Query if the given button is currently selected in its group
    public bool IsCurrentSelected(ISelectableButton btn)
    {
        string key = ResolveGroupKey(btn);
        if (string.IsNullOrEmpty(key)) return false;
        return _selectedByGroup.TryGetValue(key, out var current) && current == btn;
    }

    private void EnsureGroupMembership(ISelectableButton btn)
    {
        string key = ResolveGroupKey(btn, assignIfEmpty: true);
        if (string.IsNullOrEmpty(key)) return;
        if (!_groups.TryGetValue(key, out var list))
        {
            list = new List<ISelectableButton>();
            _groups[key] = list;
        }
        if (!list.Contains(btn)) list.Add(btn);
    }

    private string ResolveGroupKey(ISelectableButton btn, bool assignIfEmpty = false)
    {
        if (btn == null) return string.Empty;
        // 1) If explicit container is set, use it
        if (btn.groupContainer != null)
        {
            return btn.groupContainer.GetInstanceID().ToString();
        }
        // 2) If string key provided, use it
        if (!string.IsNullOrEmpty(btn.groupKey)) return btn.groupKey;
        // 3) Auto: use the immediate child container under this manager that contains the button
        Transform under = null;
        var p = btn.transform.parent;
        while (p != null && p != this.transform)
        {
            under = p;
            p = p.parent;
        }
        string autoKey = under != null ? under.GetInstanceID().ToString() : this.GetInstanceID().ToString();
        if (assignIfEmpty) btn.groupKey = autoKey;
        return autoKey;
    }
}