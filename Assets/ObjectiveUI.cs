using System.Collections;
using UnityEngine;
using TMPro;

public class ObjectiveUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject objectivePanel;
    public TMP_Text objectiveTitleText;
    public TMP_Text objectiveDescriptionText;

    private Coroutine hideCoroutine;

    void Awake()
    {
        HideObjective();
    }

    public void ShowObjective(string title, string description, float showDuration)
    {
        if (objectivePanel == null)
        {
            Debug.LogError("ObjectiveUI: Objective Panel is not assigned.");
            return;
        }

        if (objectiveTitleText != null)
        {
            objectiveTitleText.text = title;
        }

        if (objectiveDescriptionText != null)
        {
            objectiveDescriptionText.text = description;
        }

        objectivePanel.SetActive(true);

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        hideCoroutine = StartCoroutine(HideAfterDelay(showDuration));
    }

    public void HideObjective()
    {
        if (objectivePanel != null)
        {
            objectivePanel.SetActive(false);
        }
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideObjective();
    }
}