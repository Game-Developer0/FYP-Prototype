using System.Collections;
using UnityEngine;

public class IntroStoryDialogue : MonoBehaviour
{
    [Header("Dialogue Window")]
    public DialogueWindow dialogueWindow;

    [Header("Player")]
    public PlayerMovement playerMovement;

    [Header("Settings")]
    public bool startOnPlay = true;
    public float startDelay = 4f;
    public KeyCode nextKey = KeyCode.Space;

    [TextArea(2, 5)]
    public string[] storyLines;

    private int currentLineIndex = 0;
    private bool dialogueStarted = false;

    void Start()
    {
        if (startOnPlay)
        {
            StartCoroutine(StartDialogueAfterDelay());
        }
    }

    private IEnumerator StartDialogueAfterDelay()
    {
        yield return new WaitForSecondsRealtime(startDelay);
        StartDialogue();
    }

    void Update()
    {
        if (!dialogueStarted)
        {
            return;
        }

        if (Input.GetKeyDown(nextKey))
        {
            if (dialogueWindow.IsTyping)
            {
                dialogueWindow.CompleteText();
            }
            else
            {
                ShowNextLine();
            }
        }
    }

    public void StartDialogue()
    {
        if (dialogueWindow == null)
        {
            Debug.LogError("IntroStoryDialogue: Dialogue Window is not assigned.");
            return;
        }

        if (storyLines == null || storyLines.Length == 0)
        {
            Debug.LogError("IntroStoryDialogue: Story Lines are empty.");
            return;
        }

        dialogueStarted = true;
        currentLineIndex = 0;

        if (playerMovement != null)
        {
            playerMovement.SetJumpBlocked(true);
        }

        dialogueWindow.Show(storyLines[currentLineIndex]);
    }

    private void ShowNextLine()
    {
        currentLineIndex++;

        if (currentLineIndex >= storyLines.Length)
        {
            EndDialogue();
            return;
        }

        dialogueWindow.Show(storyLines[currentLineIndex]);
    }

    private void EndDialogue()
    {
        dialogueStarted = false;

        dialogueWindow.Close();

        if (playerMovement != null)
        {
            playerMovement.SetJumpBlocked(false);
        }
    }
}