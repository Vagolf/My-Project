using System.Collections;
using UnityEngine;
using TMPro;

public class TextCreater : MonoBehaviour
{
    public static TMP_Text viewText;
    public static bool runText;
    public static int charCount;

    [SerializeField] string transferText;

    [Header("Typing Speed")]
    public float normalDelay = 0.01f;
    public float fastDelay = 0.002f;              // กดค้างเพื่อเร่ง
    public KeyCode speedKey = KeyCode.LeftShift;  // ปุ่มเร่ง (กดค้าง)

    [Header("Skip (Press Once)")]
    public KeyCode skipKey = KeyCode.Space;       // กด 1 ครั้งให้ขึ้นครบ

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip typeSound;

    private Coroutine typingCoroutine;
    private bool isTyping = false;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        typeSound = Resources.Load<AudioClip>("Cutscene Sound/typewriter");
        audioSource.loop = true;
        audioSource.playOnAwake = false;
    }

    void Update()
    {
        // ✅ ระหว่างพิมพ์ ถ้ากดครั้งเดียว -> ขึ้นครบทันที
        if (isTyping && (Input.GetKeyDown(skipKey) || Input.GetMouseButtonDown(0) || TouchBegan()))
        {
            SkipTyping();
            return;
        }

        // อัปเดตจำนวนตัวอักษรที่แสดงตอนนี้
        charCount = GetComponent<TMP_Text>().text.Length;

        if (runText == true)
        {
            runText = false;

            viewText = GetComponent<TMP_Text>();
            transferText = viewText.text;
            viewText.text = "";

            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            if (typeSound != null)
            {
                audioSource.clip = typeSound;
                audioSource.Play();
            }

            typingCoroutine = StartCoroutine(RunText());
        }
    }

    IEnumerator RunText()
    {
        isTyping = true;

        foreach (char c in transferText)
        {
            viewText.text += c;
            charCount = viewText.text.Length;

            // ✅ กดค้างเพื่อเร่งสปีด (ไม่ใช่ Next)
            bool speeding = Input.GetKey(speedKey);
            yield return new WaitForSeconds(speeding ? fastDelay : normalDelay);

            // ถ้าระหว่าง loop มีการสั่ง skip แล้ว ให้หยุดเลย
            if (!isTyping) yield break;
        }

        FinishTyping();
    }

    void SkipTyping()
    {
        if (!isTyping) return;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        viewText.text = transferText;
        charCount = viewText.text.Length;

        FinishTyping();
    }

    void FinishTyping()
    {
        isTyping = false;
        typingCoroutine = null;

        if (typeSound != null)
            audioSource.Stop();
    }

    bool TouchBegan()
    {
        if (Input.touchCount <= 0) return false;
        return Input.GetTouch(0).phase == TouchPhase.Began;
    }
}
