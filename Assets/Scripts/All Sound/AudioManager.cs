using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("------------Audio Sources --------------")]
    [SerializeField] AudioSource musicSoure;
    [SerializeField] AudioSource SFXSoure;


    [Header("------------Audio Clip --------------")]
    public AudioClip background;
    public AudioClip background2;

    private void Start()
    {
        musicSoure.clip = background;
        musicSoure.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        SFXSoure.PlayOneShot(clip);
    }
}
