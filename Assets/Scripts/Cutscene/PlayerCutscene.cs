using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public class PlayerCutscene : MonoBehaviour
{
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
}
