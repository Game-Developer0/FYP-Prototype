using System.Collections;
using UnityEngine;
using TMPro;

public class DialogueWindow : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text Text;

    [Header("Typing Settings")]
    public float letterDelay = 0.03f;

    [Header("Objective After Story")]
    public ObjectiveUI objectiveUI;
    public bool showObjectiveAfterStoryEnds = true;
    public float objectiveDelayAfterStory = 0.5f;
    public float objectiveShowDuration = 5f;

    [TextArea(2, 4)]
    public string objectiveTitle = "Objective Started";

    [TextArea(2, 4)]
    public string objectiveDescription = "Explore the forest.";

    private CanvasGroup group;
    private Coroutine typingCoroutine;
    private string currentFullText = "";
    private bool isTyping = false;
    private bool objectiveAlreadyShown = false;

    public bool IsTyping
    {
        get { return isTyping; }
    }

    void Awake()
    {
        group = GetComponent<CanvasGroup>();

        if (group == null)
        {
            group = gameObject.AddComponent<CanvasGroup>();
        }

        group.alpha = 0;
        group.interactable = false;
        group.blocksRaycasts = false;

        if (Text != null)
        {
            Text.text = "";
        }
    }

    public void Show(string text)
    {
        if (Text == null)
        {
            Debug.LogError("DialogueWindow: Text is not assigned.");
            return;
        }

        currentFullText = text;

        group.alpha = 1;
        group.interactable = true;
        group.blocksRaycasts = true;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeText(text));
    }

    public void CompleteText()
    {
        if (Text == null)
        {
            return;
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        Text.text = currentFullText;
        isTyping = false;
    }

    public void Close()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        isTyping = false;

        if (Text != null)
        {
            Text.text = "";
        }

        group.alpha = 0;
        group.interactable = false;
        group.blocksRaycasts = false;

        if (showObjectiveAfterStoryEnds && !objectiveAlreadyShown)
        {
            objectiveAlreadyShown = true;
            StartCoroutine(ShowObjectiveAfterDelay());
        }
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        Text.text = "";

        foreach (char letter in text)
        {
            Text.text += letter;
            yield return new WaitForSecondsRealtime(letterDelay);
        }

        isTyping = false;
    }

    private IEnumerator ShowObjectiveAfterDelay()
    {
        yield return new WaitForSeconds(objectiveDelayAfterStory);

        if (objectiveUI != null)
        {
            objectiveUI.ShowObjective(objectiveTitle, objectiveDescription, objectiveShowDuration);
        }
        else
        {
            Debug.LogWarning("DialogueWindow: ObjectiveUI is not assigned.");
        }
    }
}