using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class textShow : MonoBehaviour
{
    public string playScene;
    public void GoNext()
    {
        SceneManager.LoadScene(playScene);
    }
}
