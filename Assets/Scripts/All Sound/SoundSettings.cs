using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class SoundSettings : MonoBehaviour
{
    [SerializeField] private AudioMixer myMixer;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;

    private void Start()
    {
        // ตั้งค่า default ถ้า PlayerPrefs ไม่มีค่า
        if (!PlayerPrefs.HasKey("masterVolume"))
            PlayerPrefs.SetFloat("masterVolume", 0.5f);
        if (!PlayerPrefs.HasKey("musicVolume"))
            PlayerPrefs.SetFloat("musicVolume", 0.5f);

        LoadSound();
    }
    public void SetMusicVolume()
    {
        float volume = musicSlider.value;
        if (volume <= 0.0001f) volume = 0.0001f; // ป้องกัน log10(0)
        myMixer.SetFloat("music", Mathf.Log10(volume)*20);
        PlayerPrefs.SetFloat("musicVolume", volume);
    }

    public void SetMasterVolume()
    {
        float volume = masterSlider.value;
        if (volume <= 0.0001f) volume = 0.0001f; // ป้องกัน log10(0)
        myMixer.SetFloat("master", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("masterVolume", volume);
    }   

    private void LoadSound()
    {
        masterSlider.value = PlayerPrefs.GetFloat("masterVolume", 0.5f);
        musicSlider.value = PlayerPrefs.GetFloat("musicVolume", 0.5f);
        SetMusicVolume();
        SetMasterVolume();
    }
}
