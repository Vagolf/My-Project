using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// เอา ISelectHandler และ IDeselectHandler ออก เพื่อไม่ให้ Unity มายุ่งตอนเราคลิกที่ว่าง
public class ButtonColor : MonoBehaviour, ISelectableButton, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
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
        }

        if (groupManager != null)
        {
            // แอดปุ่มนี้เข้าไปใน Manager
            groupManager.AddButton(this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // ถ้ายังไม่ถูกเลือก ให้แสดงสีตอนเมาส์ชี้
        if (!isSelected && buttonImage != null)
            buttonImage.color = highlightColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // ถ้าเอาเมาส์ออกและยังไม่ถูกเลือก ให้กลับเป็นสีปกติ
        if (!isSelected && buttonImage != null)
            buttonImage.color = normalColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // เมื่อโดนคลิก ให้ล็อคตัวเองเป็นสถานะถูกเลือกทันที
        isSelected = true;
        if (buttonImage != null)
            buttonImage.color = selectedColor;

        // แจ้ง Manager ให้ไปไล่เปลี่ยนสีปุ่มอื่นในกลุ่มเดียวกันให้กลับเป็นสี Normal
        if (groupManager != null)
        {
            groupManager.OnButtonChosen(this);
        }
    }

    // ฟังก์ชันนี้ Manager จะเป็นคนเรียกใช้เมื่อมีปุ่มอื่นในกลุ่มเดียวกันถูกกด
    public void SetToNormal()
    {
        isSelected = false;
        if (buttonImage != null)
            buttonImage.color = normalColor;
    }
}