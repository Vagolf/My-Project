using UnityEngine;
using System.Collections;

public class RoundManager : MonoBehaviour
{
    public int playerWinCount = 0;
    public int enemyWinCount = 0;
    public int roundsToWin = 2; // ต้องชนะ 2 รอบถึงจะชนะเกม

    [Header("Match Result Actions")]
    [Tooltip("วัตถุที่จะทำงานเมื่อผู้เล่นชนะครบตามรอบ")] public GameObject onPlayerWinObject;
    [Tooltip("วัตถุที่จะทำงานเมื่อผู้เล่นแพ้")] public GameObject onPlayerLoseObject;
    [Tooltip("ชื่อเมธอดบนวัตถุผู้ชนะที่จะรับค่าเวลา (float seconds)")] public string winTimeMethodName = "OnMatchWinTime";
    [Tooltip("ปิดการทำงานของ Player/Enemy หลังจบแมตช์")] public bool disableFightersOnEnd = true;
    [Header("End Match Freeze")]
    [Tooltip("หยุดเวลาเกมทั้งหมดเมื่อจบแมตช์ (Time.timeScale = 0)")] public bool pauseTimeOnEnd = true;
    [Tooltip("ให้ Animator บน Victory/Defeat ใช้ UnscaledTime เพื่อให้ UI ยังเล่นอนิเมชันได้ตอน pause")] public bool setUIAnimatorsUnscaled = true;

    [Header("Refs")]
    public Transform playerSpawn;
    public Transform enemySpawn;
    public GameObject player;
    public GameObject enemy;

    private HealthCh playerHealth;
    private HealthEnemy enemyHealth;
    private bool roundTransitioning;
    private float matchStartTime;
    private float accumulatedMainTime = 0f; // sum of main-time over finished rounds

    public static float LastPlayerWinTime { get; private set; }

    private void Update()
    {
        // lazy acquire refs
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        if (enemy == null) enemy = GameObject.FindGameObjectWithTag("Enemy");
        if (playerHealth == null && player != null) playerHealth = player.GetComponent<HealthCh>();
        if (enemyHealth == null && enemy != null) enemyHealth = enemy.GetComponent<HealthEnemy>();

        if (roundTransitioning) return;

        // เช็คว่าเลือดหมดหรือยัง แล้วตัดสินรอบ
        if (playerHealth != null && playerHealth.currentHealth <= 0f)
        {
            roundTransitioning = true;
            EnemyWinsRound();
            return;
        }
        if (enemyHealth != null && enemyHealth.currentHealth <= 0f)
        {
            roundTransitioning = true;
            PlayerWinsRound();
            return;
        }
    }

    private void OnEnable()
    {
        // เริ่มจับเวลาแมตช์ตั้งแต่เปิดใช้งาน (fallback)
        matchStartTime = Time.time;
        accumulatedMainTime = 0f; // reset total for a fresh match
    }

    public void PlayerWinsRound()
    {
        playerWinCount++;
        Debug.Log("Player wins round! Total = " + playerWinCount);

        if (playerWinCount >= roundsToWin)
        {
            Debug.Log("PLAYER WINS THE MATCH!");
            EndMatch(true);
        }
        else
        {
            // รวมเวลาของรอบที่เพิ่งจบเข้ากับผลรวมทั้งหมด
            AccumulateCurrentRoundElapsed();
            StartNextRound();
        }
    }

    public void EnemyWinsRound()
    {
        enemyWinCount++;
        Debug.Log("Enemy wins round! Total = " + enemyWinCount);

        if (enemyWinCount >= roundsToWin)
        {
            Debug.Log("ENEMY WINS THE MATCH!");
            EndMatch(false);
        }
        else
        {
            // รวมเวลาของรอบที่เพิ่งจบเข้ากับผลรวมทั้งหมด
            AccumulateCurrentRoundElapsed();
            StartNextRound();
        }
    }

    void StartNextRound()
    {
        Debug.Log("Starting next round...");
        StartCoroutine(DoStartNextRound());
    }

    IEnumerator DoStartNextRound()
    {
        // locate refs if not assigned
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        if (enemy == null) enemy = GameObject.FindGameObjectWithTag("Enemy");

        // 1) Reset HP
        if (player != null)
            player.GetComponent<HealthCh>()?.ResetForNewRound();
        if (enemy != null)
            enemy.GetComponent<HealthEnemy>()?.ResetForNewRound();

        // 2) Reset positions
        if (player != null && playerSpawn != null)
        {
            var p = player.transform.position;
            var sp = playerSpawn.position;
            player.transform.position = new Vector3(sp.x, sp.y, p.z); // preserve z
        }
        if (enemy != null && enemySpawn != null)
        {
            var p = enemy.transform.position;
            var sp = enemySpawn.position;
            enemy.transform.position = new Vector3(sp.x, sp.y, p.z); // preserve z
        }

        // 3) Optional: short countdown via Timer if present
        var timer = FindObjectOfType<Timer>();
        if (timer != null)
        {
            // re-enable timer component to restart its OnEnable() logic
            timer.enabled = false;
            yield return null; // wait a frame
            timer.enabled = true;
        }

        roundTransitioning = false;
    }

    void EndMatch(bool playerWin)
    {
        roundTransitioning = true;
        if (pauseTimeOnEnd)
        {
            Time.timeScale = 0f; // freeze gameplay time & animations
        }
        if (playerWin)
        {
            // แสดง Victory UI หรือไปฉากต่อไป
            Debug.Log("Show Victory Screen");
            // จับเวลาและส่งไปยังวัตถุที่กำหนด (อิงเวลาจาก Timer โดยไม่รวม countdown)
            float elapsed = 0f;
            var timer = FindObjectOfType<Timer>();
            if (timer != null)
                elapsed = accumulatedMainTime + timer.UnscaledElapsedMainTime + 2f; // รวมทุก round และบวก 2 วิ
            else
                elapsed = accumulatedMainTime + (Time.time - matchStartTime) + 2f; // fallback
            LastPlayerWinTime = elapsed;
            if (onPlayerWinObject != null)
            {
                onPlayerWinObject.SetActive(true);
                if (setUIAnimatorsUnscaled) ForceUnscaledAnimators(onPlayerWinObject);
                // ส่งเวลาไปยังสคริปต์ปลายทาง (รองรับถ้าไม่มีเมธอด)
                onPlayerWinObject.SendMessage(winTimeMethodName, elapsed, SendMessageOptions.DontRequireReceiver);
            }
        }
        else
        {
            // แสดง Game Over
            Debug.Log("Show Defeat Screen");
            if (onPlayerLoseObject != null)
            {
                onPlayerLoseObject.SetActive(true);
                if (setUIAnimatorsUnscaled) ForceUnscaledAnimators(onPlayerLoseObject);
            }

        }

        if (disableFightersOnEnd)
        {
            if (player != null) player.SetActive(false);
            if (enemy != null) enemy.SetActive(false);
        }
    }

    // รวมเวลาเฉพาะส่วน main timer ของรอบปัจจุบันเข้า accumulatedMainTime (ไม่นับ countdown)
    private void AccumulateCurrentRoundElapsed()
    {
        var timer = FindObjectOfType<Timer>();
        if (timer != null)
        {
            accumulatedMainTime += Mathf.Max(0f, timer.UnscaledElapsedMainTime);
        }
        else
        {
            accumulatedMainTime += Mathf.Max(0f, Time.time - matchStartTime);
        }
    }

    // เปลี่ยน Animator ใน UI ที่เลือกให้ใช้เวลาแบบ Unscaled เพื่อให้เล่นได้ตอนเกม pause
    private void ForceUnscaledAnimators(GameObject root)
    {
        if (root == null) return;
        var animators = root.GetComponentsInChildren<Animator>(true);
        foreach (var a in animators)
        {
            a.updateMode = AnimatorUpdateMode.UnscaledTime;
        }
        // ปรับ ParticleSystem ให้ใช้ unscaled ถ้ามี
        var particles = root.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particles)
        {
            var main = ps.main;
            main.useUnscaledTime = true;
        }
    }
}
