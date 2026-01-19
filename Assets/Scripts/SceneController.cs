using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public void LoadGamePlay()
    {
        SceneManager.LoadScene("GamePlay");
    }
}
