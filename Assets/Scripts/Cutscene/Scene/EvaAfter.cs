using System.Collections;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class EvaAfter : MonoBehaviour
{
    public GameObject Fadescene;
    public GameObject BgScreen;

    [Header("Characters")]
    public GameObject ChEnemyTalk;
    public GameObject ChKaisaTalk;

    [Header("Dialogue UI")]
    public GameObject mainTextObject;
    public GameObject nextBotton;

    public GameObject textBox;                 // กล่องข้อความ (มี TMP_Text)
    public TMP_FontAsset thaiFont;

    [Header("Name Objects")]
    public GameObject EnemyNameObj;            // ✅ ชื่อ Roman (Object แยก)
    public GameObject KaisaNameObj;            // ✅ ชื่อ Kaisa (Object แยก)
    [SerializeField] private string enemyName = "Eva";
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
    bool isLastLine;

    private enum Speaker { Enemy, Kaisa }

    private Speaker[] speakers;
    private string[] lines;

    [Header("Cutscene Animators")]
    public EnemyCutscene enemyCutscene;
    public PlayerCutscene kaisaCutscene;

    //public GameObject textShow;


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
        if (EnemyNameObj != null) EnemyNameObj.SetActive(false);
        if (KaisaNameObj != null) KaisaNameObj.SetActive(false);

        // ✅ บทพูด
        speakers = new Speaker[]
        {
            Speaker.Kaisa,
            Speaker.Enemy,
            Speaker.Kaisa,
            Speaker.Enemy,
            Speaker.Kaisa,
            Speaker.Enemy
        };

        lines = new string[]
        {
            "ตอบมา… ใครเป็นคนสั่ง!",
            "เธอกำลังเดินไปสู่ความจริงที่ไม่อยากรู้…",
            "ความจริงอะไร!",
            "ครอบครัวของเธอ… ไม่ใช่เหยื่อ",
            "...",
            "แต่เป็นส่วนหนึ่งของมัน…"
        };

        StartCoroutine(CutsceneStart());

    }
    private IEnumerator CutsceneStart()
    {
        // มุมกว้างตอนเริ่ม
        eventPos = 0;
        ChEnemyTalk.SetActive(true);
        ChKaisaTalk.SetActive(true);

        Fadescene.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        Fadescene.SetActive(false);

        yield return new WaitForSeconds(1f);
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
            BgScreen.SetActive(false);

            //if (textShow != null) textShow.SetActive(true);

            if (ChEnemyTalk) ChEnemyTalk.SetActive(true);
            
            if (ChKaisaTalk) ChKaisaTalk.SetActive(true);
            mainTextObject.SetActive(false);
            StartCoroutine(EndCutsceneFade());


            
            if (nextBotton != null) nextBotton.SetActive(false);
            ShowName(null);

            return;
        }

        // ✅ ต้องเอาคนพูดจริงๆ จาก array
        Speaker who = speakers[lineIndex];
        BgScreen.SetActive(true);

        // ✅ สลับตัวละคร “คนพูด = Talk” / “อีกคน = ปกติ”
        if (who == Speaker.Kaisa)
        {
            if (kaisaCutscene != null) kaisaCutscene.isTalking = true;

            ChKaisaTalk.SetActive(true);

            ChEnemyTalk.SetActive(false);
        }
        else // Enemy
        {
            if (enemyCutscene != null) enemyCutscene.isTalking = true;

            ChEnemyTalk.SetActive(true);

            ChKaisaTalk.SetActive(false);
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
        eventPos = (speakers[idx] == Speaker.Enemy) ? (idx * 2 + 1) : (idx * 2 + 2);

        // set text
        string textToSpeak = lines[idx];
        textTMP.text = textToSpeak;

        currentTextLength = textToSpeak.Length;
        TextCreater.runText = true;

        yield return new WaitUntil(() => textLength == currentTextLength);
        yield return new WaitForSeconds(0.2f);

        if (enemyCutscene != null) enemyCutscene.isTalking = false;
        if (kaisaCutscene != null) kaisaCutscene.isTalking = false;
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
        if (EnemyNameObj != null) EnemyNameObj.SetActive(false);
        if (KaisaNameObj != null) KaisaNameObj.SetActive(false);

        if (who == null) return;

        if (who == Speaker.Enemy)
        {
            if (EnemyNameObj != null)
            {
                EnemyNameObj.SetActive(true);
                SetNameText(EnemyNameObj, enemyName);
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

    private IEnumerator EndCutsceneFade()
    {
        Fadescene.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        Fadescene.SetActive(false);
        yield return new WaitForSeconds(0.5f);
        enemyCutscene.Die();
    }
}


