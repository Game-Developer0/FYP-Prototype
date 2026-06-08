using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuActions : MonoBehaviour
{
    public void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}