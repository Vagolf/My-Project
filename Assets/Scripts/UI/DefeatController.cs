using UnityEngine;

// Controls the defeat panel visibility and ensures timeScale is set to 0 when shown.
// Hook this script to the defeat UI root and drive it from RoundManager.onPlayerLoseObject.
public class DefeatController : MonoBehaviour
{
    [Header("Show / Hide Time Scale")] 
    [SerializeField] private bool pauseTimeOnShow = true;
    [SerializeField] private bool resumeTimeOnHide = false;

    private void OnEnable()
    {
        if (pauseTimeOnShow)
            Time.timeScale = 0f;
    }

    private void OnDisable()
    {
        if (resumeTimeOnHide)
            Time.timeScale = 1f;
    }

    // Optional helper for buttons to close panel without scene reload
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
