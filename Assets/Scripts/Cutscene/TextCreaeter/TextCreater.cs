using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextCreater : MonoBehaviour
{
    public static TMPro.TMP_Text viewText;
    public static bool runText;
    public static int charCount;
    [SerializeField] string transferText;
    [SerializeField] int internalCount;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip typeSound;

    void Awake()
    {
        // ตรวจสอบว่ามี AudioSource อยู่แล้วหรือยัง ถ้าไม่มีให้เพิ่มใหม่
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        // โหลดเสียงใหม่ทุกครั้งที่ Awake
        typeSound = Resources.Load<AudioClip>("Cutscene Sound/typewriter");
        audioSource.loop = true;
        audioSource.playOnAwake = false;
    }

    // Update is called once per frame
    void Update()
    {
        internalCount = charCount;
        charCount = GetComponent<TMPro.TMP_Text>().text.Length;
        if (runText == true)
        {
            runText = false;
            viewText = GetComponent<TMPro.TMP_Text>();
            transferText = viewText.text;
            viewText.text = "";
            if (typeSound != null)
            {
                audioSource.clip = typeSound;
                audioSource.Play();
            }
            StartCoroutine(RunText());
        }
    }

    IEnumerator RunText()
    {
        foreach (char c in transferText)
        {
            viewText.text += c;
            yield return new WaitForSeconds(0.01f); // Faster text display
        }
        if (typeSound != null)
        {
            audioSource.Stop(); // หยุดเสียง typewriter เมื่อเสร็จสิ้น
        }
    }
}
