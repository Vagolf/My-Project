using UnityEngine;
using UnityEngine.UI;

// Use this when you have 4 objects total (2 for Player, 2 for Enemy).
// Each time a side wins a round, this script colors ONE object on that side.
// Assign two objects in PlayerObjects and two in EnemyObjects.
public class WinSideColorPips : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("RoundManager in the scene. If null, will auto-find.")]
    [SerializeField] private RoundManager roundManager;

    [Header("Objects Per Side (2 each)")]
    [SerializeField] private GameObject[] playerObjects; // size 2
    [SerializeField] private GameObject[] enemyObjects;  // size 2

    [Header("Colors")]
    [SerializeField] private Color playerWinColor = Color.green;
    [SerializeField] private Color enemyWinColor = Color.red;

    [Header("Reset Options")]
    [Tooltip("If true, reset all slots back to original color when a match ends.")]
    [SerializeField] private bool resetOnMatchEnd = false;

    // cached original colors to allow reset
    private Color[] playerOriginalColors;
    private Color[] enemyOriginalColors;

    // track last seen win counts
    private int lastPlayerWins = -1;
    private int lastEnemyWins = -1;
    private bool matchEndedApplied = false;

    private void Awake()
    {
        CacheOriginalColors();
    }

    private void Reset()
    {
        if (roundManager == null)
            roundManager = FindObjectOfType<RoundManager>();
    }

    private void OnEnable()
    {
        // Ensure base colors are cached and apply current wins (if any)
        if (playerOriginalColors == null || playerOriginalColors.Length != playerObjects.Length
            || enemyOriginalColors == null || enemyOriginalColors.Length != enemyObjects.Length)
        {
            CacheOriginalColors();
        }
        ApplyInitialFromCurrentWins();
    }

    private void Update()
    {
        if (roundManager == null)
        {
            roundManager = FindObjectOfType<RoundManager>();
            if (roundManager == null) return;
        }

        // Player side changed?
        if (roundManager.playerWinCount != lastPlayerWins)
        {
            if (roundManager.playerWinCount > lastPlayerWins)
            {
                ColorOneForPlayer(roundManager.playerWinCount - 1);
            }
            lastPlayerWins = roundManager.playerWinCount;
        }

        // Enemy side changed?
        if (roundManager.enemyWinCount != lastEnemyWins)
        {
            if (roundManager.enemyWinCount > lastEnemyWins)
            {
                ColorOneForEnemy(roundManager.enemyWinCount - 1);
            }
            lastEnemyWins = roundManager.enemyWinCount;
        }

        // End of match
        if (!matchEndedApplied && roundManager.roundsToWin > 0)
        {
            if (roundManager.playerWinCount >= roundManager.roundsToWin || roundManager.enemyWinCount >= roundManager.roundsToWin)
            {
                matchEndedApplied = true;
                if (resetOnMatchEnd)
                {
                    ResetAllToOriginal();
                    lastPlayerWins = roundManager.playerWinCount;
                    lastEnemyWins = roundManager.enemyWinCount;
                }
            }
        }
    }

    private void ApplyInitialFromCurrentWins()
    {
        if (roundManager == null)
        {
            roundManager = FindObjectOfType<RoundManager>();
        }
        ResetAllToOriginal();
        if (roundManager != null)
        {
            for (int i = 0; i < Mathf.Min(roundManager.playerWinCount, playerObjects.Length); i++)
                ColorOneForPlayer(i);
            for (int i = 0; i < Mathf.Min(roundManager.enemyWinCount, enemyObjects.Length); i++)
                ColorOneForEnemy(i);
            lastPlayerWins = roundManager.playerWinCount;
            lastEnemyWins = roundManager.enemyWinCount;
        }
        else
        {
            lastPlayerWins = -1;
            lastEnemyWins = -1;
        }
    }

    private void CacheOriginalColors()
    {
        playerOriginalColors = new Color[playerObjects != null ? playerObjects.Length : 0];
        for (int i = 0; i < playerOriginalColors.Length; i++)
            playerOriginalColors[i] = GetObjectColor(playerObjects[i]);

        enemyOriginalColors = new Color[enemyObjects != null ? enemyObjects.Length : 0];
        for (int i = 0; i < enemyOriginalColors.Length; i++)
            enemyOriginalColors[i] = GetObjectColor(enemyObjects[i]);
    }

    private void ResetAllToOriginal()
    {
        if (playerObjects != null)
        {
            for (int i = 0; i < playerObjects.Length; i++)
                SetObjectColor(playerObjects[i], SafeColor(playerOriginalColors, i));
        }
        if (enemyObjects != null)
        {
            for (int i = 0; i < enemyObjects.Length; i++)
                SetObjectColor(enemyObjects[i], SafeColor(enemyOriginalColors, i));
        }
    }

    private static Color SafeColor(Color[] arr, int index)
    {
        if (arr == null || index < 0 || index >= arr.Length) return Color.white;
        return arr[index];
    }

    private void ColorOneForPlayer(int index)
    {
        if (playerObjects == null || playerObjects.Length == 0) return;
        if (index < 0) return;
        if (index >= playerObjects.Length) index = playerObjects.Length - 1; // clamp
        SetObjectColor(playerObjects[index], playerWinColor);
    }

    private void ColorOneForEnemy(int index)
    {
        if (enemyObjects == null || enemyObjects.Length == 0) return;
        if (index < 0) return;
        if (index >= enemyObjects.Length) index = enemyObjects.Length - 1; // clamp
        SetObjectColor(enemyObjects[index], enemyWinColor);
    }

    // Color helpers supporting SpriteRenderer, UI Image, or Renderer.material
    private static Color GetObjectColor(GameObject go)
    {
        if (go == null) return Color.white;
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null) return sr.color;
        var img = go.GetComponent<Image>();
        if (img != null) return img.color;
        var rend = go.GetComponent<Renderer>();
        if (rend != null && rend.material != null && rend.material.HasProperty("_Color"))
            return rend.material.color;
        return Color.white;
    }

    private static void SetObjectColor(GameObject go, Color c)
    {
        if (go == null) return;
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null) { sr.color = c; return; }
        var img = go.GetComponent<Image>();
        if (img != null) { img.color = c; return; }
        var rend = go.GetComponent<Renderer>();
        if (rend != null && rend.material != null && rend.material.HasProperty("_Color"))
        {
            rend.material.color = c;
        }
    }
}
