using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject aboutPanel;
    public GameObject controlsPanel;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip selectClip;

    private void Awake()
    {
        // At start, only show the main menu
        mainMenuPanel.SetActive(true);
        aboutPanel.SetActive(false);
        controlsPanel.SetActive(false);
    }

    // Called by Play button
    public void OnPlayPressed()
    {
        PlaySelectSFX();
        SceneManager.LoadScene("Game");
    }

    // Called by Settings button
    public void OnAboutPressed()
    {
        PlaySelectSFX();
        mainMenuPanel.SetActive(false);
        aboutPanel.SetActive(true);
    }

    // Called by Controls button
    public void OnControlsPressed()
    {
        PlaySelectSFX();
        mainMenuPanel.SetActive(false);
        controlsPanel.SetActive(true);
    }

    // Called by Back button on Settings panel
    public void OnBackFromAbout()
    {
        PlaySelectSFX();
        aboutPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    // Called by Back button on Controls panel
    public void OnBackFromControls()
    {
        PlaySelectSFX();
        controlsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    private void PlaySelectSFX()
    {
        if (sfxSource != null && selectClip != null)
            sfxSource.PlayOneShot(selectClip);
    }
}
