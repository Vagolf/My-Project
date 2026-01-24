using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public class textShow : MonoBehaviour
{
    public GameObject textFight;
    public bool isAction = false;

    // ✅ เรียกใช้เมื่อไหร่ -> ไป Scene ถัดไปทันที
    private void Update()
    {
        textFight.SetActive(isAction);
    }
    public void NextScene()
    {
        SceneManager.LoadScene("GamePlay");
    }
}
