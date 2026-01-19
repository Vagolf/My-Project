using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    // Global short gate: true = block other systems
    public static bool GateBlocked { get; private set; }
    public bool IsCountingDown => countdownTime > 0f;
    public bool IsRunning => countdownTime <= 0f && remainingTime > 0f;
    // Public read-only accessors for other systems
    public float RemainingTime => remainingTime;
    public float DefaultRemainingSeconds => defaultRemainingSeconds;
    public float ElapsedMainTime => Mathf.Max(0f, defaultRemainingSeconds - remainingTime);
    // Unscaled elapsed since main timer started (excludes countdown, ignores timeScale)
    public float UnscaledElapsedMainTime => _mainStarted ? Mathf.Max(0f, Time.unscaledTime - _mainStartUnscaled) : 0f;

    private bool _mainStarted = false;
    private float _mainStartUnscaled = 0f;
    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] float remainingTime;
    [SerializeField] TextMeshProUGUI countdownText;
    [SerializeField] float countdownTime = 3f; // 3-second pre-countdown
    [SerializeField] float defaultRemainingSeconds = 120f; // default main timer length
    

    private void OnEnable()
    {
        // Fully reset when enabled
        Restart(3f, defaultRemainingSeconds);
    }

    void Update()
    {
        // short countdown gate (independent from UI assignment)
        if (countdownTime > 0f)
        {
            countdownTime -= Time.deltaTime;
            if (countdownText)
                countdownText.text = Mathf.CeilToInt(Mathf.Max(0f, countdownTime)).ToString();
            if (countdownTime > 0f)
            {
                GateBlocked = true; // block others while counting
                return; // still counting down
            }
            if (countdownText) countdownText.enabled = false; // hide when finished
            GateBlocked = false; // release gate when countdown done
            // Mark main timer start (unscaled reference)
            _mainStarted = true;
            _mainStartUnscaled = Time.unscaledTime;
            // proceed into main timer logic this frame
        }

        if (remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;
        }
        else if (remainingTime < 0)
        {
            remainingTime = 0;
            // Timer has finished, you can add additional logic here if needed
        }
        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);


    }

    // Reset and start countdown + main timer
    public void Restart(float countdownSeconds = 3f, float remainingSeconds = 120f)
    {
        countdownTime = Mathf.Max(0f, countdownSeconds);
        remainingTime = Mathf.Max(0f, remainingSeconds);
        _mainStarted = false;
        _mainStartUnscaled = 0f;
        GateBlocked = countdownTime > 0f;
        if (countdownText)
        {
            countdownText.enabled = countdownTime > 0f;
            if (countdownTime > 0f)
                countdownText.text = Mathf.CeilToInt(countdownTime).ToString();
        }
        // update main timer display immediately
        if (timerText)
        {
            int minutes = Mathf.FloorToInt(remainingTime / 60f);
            int seconds = Mathf.FloorToInt(remainingTime % 60f);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
}
