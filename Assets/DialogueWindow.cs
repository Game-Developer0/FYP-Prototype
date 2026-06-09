using System.Collections;
using UnityEngine;
using TMPro;

public class DialogueWindow : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text Text;

    [Header("Typing Settings")]
    public float letterDelay = 0.03f;

    [Header("Mission After Story")]
    public MissionSequenceManager missionSequenceManager;
    public bool startMissionAfterStoryEnds = true;
    public float missionDelayAfterStory = 0.5f;

    private CanvasGroup group;
    private Coroutine typingCoroutine;
    private string currentFullText = "";
    private bool isTyping = false;
    private bool missionAlreadyStarted = false;

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

        if (startMissionAfterStoryEnds && !missionAlreadyStarted)
        {
            missionAlreadyStarted = true;
            StartCoroutine(StartMissionAfterDelay());
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

    private IEnumerator StartMissionAfterDelay()
    {
        yield return new WaitForSeconds(missionDelayAfterStory);

        if (missionSequenceManager != null)
        {
            missionSequenceManager.StartMissionFlowAfterStory();
        }
        else
        {
            Debug.LogWarning("DialogueWindow: MissionSequenceManager is not assigned.");
        }
    }
}