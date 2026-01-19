using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseGame : MonoBehaviour
{
    public static bool GameIsPause = false;

    public GameObject pauseMenuUI;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameIsPause)
            {
                Continue();
            }
            else
            {
                Pause();
            }
        }
    }
    public void Continue()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        GameIsPause = false;
    }
    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        GameIsPause = true;
    }
     
    /** public void GameGuide()
    {
        // Load the main menu scene (assuming it's named "MainMenu")
        //UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        Debug.Log("Load gameguide");
    }

    public void SoundSettings()
    {
               // Load the sound settings scene (assuming it's named "SoundSettings")
        //UnityEngine.SceneManagement.SceneManager.LoadScene("SoundSettings");
        Debug.Log("Load sound settings");
    }

    public void ExitGame()
    {
        // Quit the application
        Application.Quit();
        Debug.Log("Exit Game");
    } **/
}
