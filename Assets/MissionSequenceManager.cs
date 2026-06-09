using System.Collections;
using UnityEngine;

public class MissionSequenceManager : MonoBehaviour
{
    [Header("Boss Wolf Direction Guide")]
    public BossWolfDirectionCanvas bossWolfDirectionCanvas;

    [Header("UI References")]
    public ObjectiveUI objectiveUI;
    public EcosystemWarningUI warningUI;

    [Header("Mission 1")]
    public float mission1ShowDuration = 5f;
    public string mission1Title = "Objective Started";

    [TextArea(2, 4)]
    public string mission1Description = "Explore the forest and investigate the ecosystem disturbance.";

    [Header("Mission 2 Warning")]
    public float delayBeforeWarning = 1f;
    public float warningShowDuration = 5f;

    [TextArea(2, 5)]
    public string warningMessage = "ECOSYSTEM WARNING!\n\nBoss Wolves are hunting the deer population. If they are not stopped, the forest balance will collapse.";

    [Header("Mission 2 Objective")]
    public float delayBeforeMission2Objective = 0.5f;
    public int bossWolvesRequired = 4;
    public string mission2Title = "Current Objective";

    private int bossWolvesKilled = 0;
    private bool mission2Active = false;
    private bool mission2Completed = false;
    private Coroutine missionFlowCoroutine;

    public void StartMissionFlowAfterStory()
    {
        if (missionFlowCoroutine != null)
        {
            StopCoroutine(missionFlowCoroutine);
        }

        missionFlowCoroutine = StartCoroutine(MissionFlow());
    }

    private IEnumerator MissionFlow()
    {
        if (objectiveUI == null)
        {
            Debug.LogError("MissionSequenceManager: ObjectiveUI is not assigned.");
            yield break;
        }

        if (warningUI == null)
        {
            warningUI = EcosystemWarningUI.Instance;
        }

        // Mission 1 objective on Objective Canvas
        objectiveUI.ShowObjective(mission1Title, mission1Description, mission1ShowDuration);

        yield return new WaitForSeconds(mission1ShowDuration);
        yield return new WaitForSeconds(delayBeforeWarning);

        // Mission 2 warning on Warning Canvas
        if (warningUI != null)
        {
            warningUI.ShowWarning(warningMessage, warningShowDuration);

            float typingTime = 0f;

            if (warningUI.useTypewriterEffect)
            {
                typingTime = warningMessage.Length * warningUI.typeSpeed;
            }

            yield return new WaitForSeconds(typingTime + warningShowDuration);
        }
        else
        {
            Debug.LogWarning("MissionSequenceManager: WarningUI is not assigned and Instance was not found.");
            yield return new WaitForSeconds(warningShowDuration);
        }

        yield return new WaitForSeconds(delayBeforeMission2Objective);

        // Mission 2 objective on Objective Canvas
        StartMission2();
    }

    private void StartMission2()
    {
        bossWolvesKilled = 0;
        mission2Active = true;
        mission2Completed = false;

        if (bossWolfDirectionCanvas != null)
        {
            bossWolfDirectionCanvas.ShowGuide();
        }

        UpdateMission2UI();
    }

    public void RegisterBossWolfKill()
    {
        if (!mission2Active) return;
        if (mission2Completed) return;

        bossWolvesKilled++;

        if (bossWolvesKilled > bossWolvesRequired)
        {
            bossWolvesKilled = bossWolvesRequired;
        }

        UpdateMission2UI();

        if (bossWolvesKilled >= bossWolvesRequired)
        {
            CompleteMission2();
        }
    }

    private void UpdateMission2UI()
    {
        string description =
            "Kill " + bossWolvesRequired + " Boss Wolves.\n" +
            "Progress: " + bossWolvesKilled + " / " + bossWolvesRequired;

        objectiveUI.ShowObjectivePermanent(mission2Title, description);
    }

    private void CompleteMission2()
    {
        mission2Completed = true;
        mission2Active = false;

        if (bossWolfDirectionCanvas != null)
        {
            bossWolfDirectionCanvas.HideGuide();
        }

        objectiveUI.ShowObjective(
            "Objective Complete",
            "The deer population is safe for now.",
            5f
        );
    }
}