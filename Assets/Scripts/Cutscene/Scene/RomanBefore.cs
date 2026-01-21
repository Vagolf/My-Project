using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class RomanBefore : MonoBehaviour
{
    public GameObject Fadescene;
    public GameObject ChaRoman;
    public GameObject ChKaisa;
    public GameObject textBox;
    public TMP_FontAsset thaiFont;

    [SerializeField] string textToSpeak;
    [SerializeField] int currentTextLength;
    [SerializeField] int textLength;
    [SerializeField] GameObject mainTextObject;

    [SerializeField] GameObject nextBotton;
    [SerializeField] int eventPos = 0;
    [SerializeField] GameObject charName;

    // ✅ ตัวควบคุมพิมพ์ข้อความ
    private TextCreater textCreater;

    private void Update()
    {
        textLength = TextCreater.charCount;
    }

    void Start()
    {
        // ✅ ใส่ฟอนต์ไทย
        textBox.GetComponent<TMP_Text>().font = thaiFont;
        charName.GetComponent<TMP_Text>().font = thaiFont;

        // ✅ ดึง TextCreater จาก textBox แล้วตั้งค่าความเร็ว/ปุ่มเร่ง
        textCreater = textBox.GetComponent<TextCreater>();
        if (textCreater != null)
        {
            textCreater.normalDelay = 0.01f;      // ปกติ
            textCreater.fastDelay = 0.002f;       // เร็วตอนกดค้าง
            textCreater.speedKey = KeyCode.LeftShift; // ✅ ใช้ Shift แทน Space (กันไปกด Next)
        }

        StartCoroutine(EvenStart());
    }

    IEnumerator EvenStart()
    {
        //Fade in scene
        Fadescene.SetActive(true);
        yield return new WaitForSeconds(2f);
        Fadescene.SetActive(false);

        //Set characters
        ChaRoman.SetActive(true);
        yield return new WaitForSeconds(2f);
        ChKaisa.SetActive(true);

        //Talk start
        yield return new WaitForSeconds(2f);
        mainTextObject.SetActive(true);

        charName.GetComponent<TMP_Text>().text = "Roman";
        textToSpeak = "กากยdddddddddddddddddd d          dddddddd wdwdwนย";
        textBox.GetComponent<TMP_Text>().text = textToSpeak;

        currentTextLength = textToSpeak.Length;
        TextCreater.runText = true;

        yield return new WaitUntil(() => textLength == currentTextLength);

        yield return new WaitForSeconds(0.3f);

        nextBotton.SetActive(true);
        eventPos = 1;

        // ✅ กันปุ่ม Next ถูกเลือกอัตโนมัติ (Space/Enter จะไม่กด Next เอง)
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    IEnumerator EventOne()
    {
        nextBotton.SetActive(false);
        yield return new WaitForSeconds(0.5f);

        charName.GetComponent<TMP_Text>().text = "Kaisa";
        textToSpeak = "I already know but I'll keep doing it.";
        textBox.GetComponent<TMP_Text>().text = textToSpeak;

        currentTextLength = textToSpeak.Length;
        TextCreater.runText = true;

        yield return new WaitUntil(() => textLength == currentTextLength);

        yield return new WaitForSeconds(0.3f);

        nextBotton.SetActive(true);
        eventPos = 2;

        // ✅ กันกด Next เองด้วย Space/Enter
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void NextBotton()
    {
        if (eventPos == 1)
        {
            StartCoroutine(EventOne());
        }
    }
}
