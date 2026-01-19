using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using TMPro;

public class ButtonTextColor : MonoBehaviour, ISelectableButton, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IPointerClickHandler
{
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;
    public Color selectedColor = Color.cyan; // Added selected color
    public TextMeshProUGUI buttonText;

    public ButtonGroupColorManager groupManager;
    [Tooltip("Buttons with the same groupKey belong to the same selection set. Leave empty to auto-group by parent container.")]
    public string groupKey { get; set; } = string.Empty;
    [Tooltip("Optional explicit grouping root. Buttons referencing the same container are in one set.")]
    public Transform groupContainer { get; set; }
    private bool isSelected = false;

    private void Awake()
    {
        // Auto-wire text component if not assigned
        if (buttonText == null)
            buttonText = GetComponentInChildren<TextMeshProUGUI>(true);

        // Auto-find group manager if not assigned
        if (groupManager == null)
        {
            groupManager = GetComponentInParent<ButtonGroupColorManager>();
            if (groupManager != null)
            {
                groupManager.AddButton(this);
            }
        }
    }

    private void Start()
    {
        ApplyColor(normalColor);
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        ApplyColor(selectedColor); // Use selected color
        groupManager?.OnButtonChosen(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isSelected)
            ApplyColor(highlightColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isSelected)
            ApplyColor(normalColor);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        // Ignore global deselect; manager handles resetting within group
        // This allows multiple groups to keep their own selected state concurrently
        if (groupManager != null && groupManager.IsCurrentSelected(this))
        {
            isSelected = true;
            ApplyColor(selectedColor);
            return;
        }
        // If we reach here, this button is not the selected one in its group
        // Reset to normal state
        isSelected = false;
        ApplyColor(normalColor);
    }

    public void SetToNormal()
    {
        isSelected = false;
        ApplyColor(normalColor);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        isSelected = true;
        ApplyColor(selectedColor);
        groupManager?.OnButtonChosen(this);
    }

    private void ApplyColor(Color c)
    {
        if (buttonText != null)
            buttonText.color = c;
    }
}