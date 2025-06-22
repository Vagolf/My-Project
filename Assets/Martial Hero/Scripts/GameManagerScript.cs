using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManagerScript : MonoBehaviour
{
    public GameObject gameOverUI;

    private bool isGamePaused = false;
    private Scene currentScene;

    void Start()
    {
        currentScene = SceneManager.GetActiveScene(); // Get the current scene

        /*
        augmentImgComponent = FindObjectOfType<AugmentImg>();

        if (augmentImgComponent == null)
        {
            Debug.LogError("AugmentImg component not found!");
        }
        */
    }

    public void gameOver()
    {
        // set gaveOverUI after die
        gameOverUI.SetActive(true);
    }

    public void RestartClickedUI()
    {
        // restart the game
        SceneManager.LoadScene(currentScene.buildIndex); // Load the current scene by its build index
    }

    public void MainMenuButton()
    {
        // go to main menu button after die
        SceneManager.LoadScene(currentScene.buildIndex); // wait for mainmenu scene
    }

    private IEnumerator WaitForRestartCanvasEnd()
    {
        // WaitForSeconds() <-- die animation lenght
        yield return new WaitForSeconds(0.64f);
        RestartClickedUI();
    }
}