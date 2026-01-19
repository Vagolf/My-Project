using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonColor : MonoBehaviour, ISelectableButton, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IPointerClickHandler
{
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;
    public Color selectedColor = Color.cyan;
    public Image buttonImage;

    public ButtonGroupManager groupManager;
    [Tooltip("Buttons with the same groupKey belong to the same selection set. Leave empty to auto-group by parent container.")]
    public string groupKey { get; set; } = string.Empty;
    [Tooltip("Optional explicit grouping root. Buttons referencing the same container are in one set.")]
    public Transform groupContainer { get; set; }
    private bool isSelected = false;

    private void Start()
    {
        if (buttonImage == null)
            buttonImage = GetComponent<Image>();
            
        if (buttonImage != null)
            buttonImage.color = normalColor;
            
        if (groupManager == null)
        {
            groupManager = GetComponentInParent<ButtonGroupManager>();
            if (groupManager != null)
            {
                groupManager.AddButton(this);
            }
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        if (buttonImage != null)
            buttonImage.color = selectedColor;
        groupManager?.OnButtonChosen(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isSelected && buttonImage != null)
            buttonImage.color = highlightColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isSelected && buttonImage != null)
            buttonImage.color = normalColor;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        // Ignore global deselect; manager handles resetting within group
        // This allows multiple groups to keep their own selected state concurrently
        if (groupManager != null && groupManager.IsCurrentSelected(this))
        {
            isSelected = true;
            if (buttonImage != null)
                buttonImage.color = selectedColor;
            return;
        }
        // If we reach here, this button is not the selected one in its group
        // Reset to normal state
        isSelected = false;
        if (buttonImage != null)
            buttonImage.color = normalColor;
    }

    public void SetToNormal()
    {
        isSelected = false;
        if (buttonImage != null)
            buttonImage.color = normalColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        isSelected = true;
        if (buttonImage != null)
            buttonImage.color = selectedColor;
        groupManager?.OnButtonChosen(this);
    }
}
