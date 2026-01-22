using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class RomanBefore : MonoBehaviour
{
    public GameObject Fadescene;

    [Header("Characters (ตัวละคร)")]
    public GameObject ChaRoman;
    public GameObject ChKaisa;

    [Header("Dialogue UI")]
    public GameObject mainTextObject;
    public GameObject nextBotton;

    public GameObject textBox;                 // กล่องข้อความ (มี TMP_Text)
    public TMP_FontAsset thaiFont;

    [Header("Name Objects (วางคนละตำแหน่ง)")]
    public GameObject RomanNameObj;            // ✅ ชื่อ Roman (Object แยก)
    public GameObject KaisaNameObj;            // ✅ ชื่อ Kaisa (Object แยก)
    [SerializeField] private string romanName = "Roman";
    [SerializeField] private string kaisaName = "Kaisa";

    [Header("Cutscene State")]
    [SerializeField] public int eventPos = 0;

    // Text typing
    private TextCreater textCreater;
    private TMP_Text textTMP;

    [SerializeField] private int currentTextLength;
    [SerializeField] private int textLength;

    private int lineIndex = -1;
    private bool isPlayingLine = false;

    private enum Speaker { Roman, Kaisa }

    private Speaker[] speakers;
    private string[] lines;

    private void Update()
    {
        textLength = TextCreater.charCount;
    }

    private void Start()
    {
        // TMP text
        textTMP = textBox.GetComponent<TMP_Text>();
        textTMP.font = thaiFont;

        // TextCreater speed
        textCreater = textBox.GetComponent<TextCreater>();
        if (textCreater != null)
        {
            textCreater.normalDelay = 0.1f;
            textCreater.speedKey = KeyCode.LeftShift;
        }

        // ✅ ปิดชื่อทั้งสองก่อน
        if (RomanNameObj != null) RomanNameObj.SetActive(false);
        if (KaisaNameObj != null) KaisaNameObj.SetActive(false);

        // ✅ บทพูด
        speakers = new Speaker[]
        {
            Speaker.Kaisa,
            Speaker.Roman,
            Speaker.Kaisa,
            Speaker.Roman,
            Speaker.Kaisa,
            Speaker.Kaisa
        };

        lines = new string[]
        {
            "นายเกี่ยวอะไรกับคนที่เผาบ้านฉัน",
            "ข้าคือโรมัน… ผู้รับใช้ของเขา",
            "งั้นบอกมาว่าเขาอยู่ไหน",
            "นายของข้ารู้ว่าเธอจะตามมา",
            "ถ้าขวาง… ก็ต้องล้ม",
            "เข้ามาสิ นักดาบคู่"
        };

        StartCoroutine(CutsceneStart());
    }

    private IEnumerator CutsceneStart()
    {
        // มุมกว้างตอนเริ่ม
        eventPos = 0;

        Fadescene.SetActive(true);
        yield return new WaitForSeconds(2f);
        Fadescene.SetActive(false);

        ChaRoman.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        ChKaisa.SetActive(true);

        yield return new WaitForSeconds(0.8f);
        mainTextObject.SetActive(true);

        PlayNextLine();
    }

    public void NextBotton()
    {
        if (isPlayingLine) return;
        PlayNextLine();
    }

    private void PlayNextLine()
    {
        lineIndex++;

        // ✅ จบคัทซีน
        if (lineIndex >= lines.Length)
        {
            eventPos = 0;
            if (nextBotton != null) nextBotton.SetActive(false);

            // ปิดชื่อทั้งหมด
            ShowName(null);
            return;
        }

        StartCoroutine(PlayLine(lineIndex));
    }

    private IEnumerator PlayLine(int idx)
    {
        isPlayingLine = true;
        if (nextBotton != null) nextBotton.SetActive(false);

        // ✅ เปิดชื่อคนพูด (วางแยกตำแหน่งได้เลย)
        ShowName(speakers[idx]);

        // ✅ เปลี่ยน eventPos ให้กล้องซูมตามเงื่อนไข
        // Roman = เลขคี่ / Kaisa = เลขคู่
        eventPos = (speakers[idx] == Speaker.Roman) ? (idx * 2 + 1) : (idx * 2 + 2);

        // set text
        string textToSpeak = lines[idx];
        textTMP.text = textToSpeak;

        currentTextLength = textToSpeak.Length;
        TextCreater.runText = true;

        yield return new WaitUntil(() => textLength == currentTextLength);
        yield return new WaitForSeconds(0.2f);

        if (nextBotton != null) nextBotton.SetActive(true);

        // กันปุ่มถูกเลือกอัตโนมัติ
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        isPlayingLine = false;
    }

    // ==========================
    // ✅ แสดงชื่อแบบแยก 2 Object
    // ==========================
    private void ShowName(Speaker? who)
    {
        if (RomanNameObj != null) RomanNameObj.SetActive(false);
        if (KaisaNameObj != null) KaisaNameObj.SetActive(false);

        if (who == null) return;

        if (who == Speaker.Roman)
        {
            if (RomanNameObj != null)
            {
                RomanNameObj.SetActive(true);
                SetNameText(RomanNameObj, romanName);
            }
        }
        else
        {
            if (KaisaNameObj != null)
            {
                KaisaNameObj.SetActive(true);
                SetNameText(KaisaNameObj, kaisaName);
            }
        }
    }

    private void SetNameText(GameObject nameObj, string name)
    {
        // รองรับ TMP_Text อยู่ใน Object หรืออยู่ในลูก
        var tmp = nameObj.GetComponent<TMP_Text>();
        if (tmp == null) tmp = nameObj.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            tmp.font = thaiFont;
            tmp.text = name;
        }
    }
}
