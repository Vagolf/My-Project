using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CharacterContainer : MonoBehaviour, IPointerClickHandler
{
    public Image characterImage; // ????????????????
    public Image displayImage;   // ????????????????? (?????????? Inspector)
    public Sprite characterSprite; // Sprite ?????????????

    private void Start()
    {
        if (characterImage != null && characterSprite != null)
            characterImage.sprite = characterSprite;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (displayImage != null && characterSprite != null)
            displayImage.sprite = characterSprite;
    }
}