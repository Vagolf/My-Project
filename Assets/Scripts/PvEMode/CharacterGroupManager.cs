using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CharacterGroupManager : MonoBehaviour      
{
    public List<CharacterContainer> characterContainers;
    public CharacterDisplay characterDisplay; // อ้างอิง DisplayCharacterContainer

    // เรียกเมื่อเลือกตัวละคร (index = 0-5, isPlayer = true/false)
    public void OnCharacterSelected(int index, bool isPlayer, Sprite characterSprite)
    {
        if (characterDisplay != null && characterSprite != null)
        {
            if (isPlayer)
                characterDisplay.ShowPlayer(index, characterSprite);
            else
                characterDisplay.ShowEnemy(index, characterSprite);
        }
    }
}