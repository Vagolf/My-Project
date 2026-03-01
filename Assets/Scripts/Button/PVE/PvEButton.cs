using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PvEButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;
    public Color selectedColor = Color.cyan;
    public Image buttonImage;

    [Tooltip("ใส่ 'Player' สำหรับฝั่งซ้าย หรือ 'Enemy' สำหรับฝั่งขวา")]
    public string groupName;

    [Tooltip("ใส่ชื่อตัวละคร เช่น Kaisa, Roman, Eva, Dusan")]
    public string characterName; // 🔥 เพิ่มตัวแปรนี้เข้ามา

    private PvEButtonManager manager;
    private bool isSelected = false;

    private void Start()
    {
        if (buttonImage == null) buttonImage = GetComponent<Image>();
        if (buttonImage != null) buttonImage.color = normalColor;

        manager = FindObjectOfType<PvEButtonManager>();
        if (manager != null)
        {
            manager.RegisterButton(this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isSelected && buttonImage != null) buttonImage.color = highlightColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isSelected && buttonImage != null) buttonImage.color = normalColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        isSelected = true;
        if (buttonImage != null) buttonImage.color = selectedColor;

        if (manager != null)
        {
            manager.OnButtonPressed(this);
        }
    }

    public void ResetColor()
    {
        isSelected = false;
        if (buttonImage != null) buttonImage.color = normalColor;
    }
}