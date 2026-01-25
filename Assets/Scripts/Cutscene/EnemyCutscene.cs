using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public class EnemyCutscene : MonoBehaviour
{
    public GameObject enemy;
    private Animator anim;
    public bool isTalking = false;
    public bool isFade = false;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        anim.SetBool("talking", isTalking);
        anim.SetBool("fadeIn", isFade);
        
    }
    public void Die() {
        anim.SetTrigger("die");
    }

    public void unActive() {
        enemy.SetActive(false);
    }

}
