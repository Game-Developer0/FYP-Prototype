using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [Header("Pause UI")]
    public GameObject pauseText; // Text object saying "Game is paused"

    [Header("Optional")]
    public GameObject pauseButton; // If you have a pause UI button, drag it here

    public static bool isPaused = false;

    void Start()
    {
        ResumeGame();
    }

    void Update()
    {
        // Press ESC to pause/unpause
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;

        Time.timeScale = 0f;

        if (pauseText != null)
            pauseText.SetActive(true);

        if (pauseButton != null)
            pauseButton.SetActive(false);

        // Pause all normal game audio too
        AudioListener.pause = true;
    }

    public void ResumeGame()
    {
        isPaused = false;

        Time.timeScale = 1f;

        if (pauseText != null)
            pauseText.SetActive(false);

        if (pauseButton != null)
            pauseButton.SetActive(true);

        AudioListener.pause = false;
    }
}