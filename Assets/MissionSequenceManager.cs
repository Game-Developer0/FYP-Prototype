using System.Collections;
using UnityEngine;

public class MissionSequenceManager : MonoBehaviour
{
    public enum DebugStartMode
    {
        Normal,
        Mission2,
        Mission3,
        Mission4
    }

    [Header("Debug Start")]
    public DebugStartMode debugStartMode = DebugStartMode.Normal;
    public float debugStartDelay = 1f;

    [Header("Mission Direction Guide")]
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
    public string warningMessage = "ECOSYSTEM WARNING!\n\nBoss Wolves are hunting the deer population. Stop them before the deer population collapses.";

    [Header("Mission 2 Objective")]
    public float delayBeforeMission2Objective = 0.5f;
    public int bossWolvesRequired = 4;
    public string mission2Title = "Current Objective";

    [Header("Mission 2 Timer")]
    public bool useMission2TimeLimit = true;
    public float mission2TimeLimit = 180f;

    [TextArea(2, 5)]
    public string mission2FailWarningMessage = "ECOSYSTEM FAILURE!\n\nThe deer population has gone extinct because the Boss Wolves were not stopped in time.";

    public float mission2FailObjectiveShowTime = 6f;
    public bool continueToMission3AfterMission2Fail = true;

    private int bossWolvesKilled = 0;
    private bool mission2Active = false;
    private bool mission2Completed = false;
    private bool mission2Failed = false;
    private float mission2TimeRemaining = 0f;
    private Coroutine mission2TimerCoroutine;

    [Header("Mission 3 Dragon Warning")]
    public float delayBeforeMission3Warning = 1f;
    public float mission3WarningShowDuration = 5f;

    [TextArea(2, 5)]
    public string mission3WarningMessage = "DRAGON THREAT DETECTED!\n\nTwo elemental dragons have appeared in different regions. Follow the minimap direction marker and defeat them.";

    [Header("Mission 3 Dragons")]
    public GameObject[] mission3Dragons;
    public bool hideMission3DragonsOnStart = true;

    [Header("Mission 3 Objective")]
    public int dragonsRequired = 2;
    public string mission3Title = "Current Objective";

    private int dragonsKilled = 0;
    private bool mission3Active = false;
    private bool mission3Completed = false;

    [Header("Mission 4 Bear Warning")]
    public float delayBeforeMission4Warning = 1f;
    public float mission4WarningShowDuration = 5f;

    [TextArea(2, 5)]
    public string mission4WarningMessage = "DANGEROUS WILDLIFE ALERT!\n\nA group of aggressive bears is threatening the forest. Follow the minimap direction marker and reduce the bear threat.";

    [Header("Mission 4 Objective")]
    public int bearsRequired = 4;
    public string mission4Title = "Current Objective";

    private int bearsKilled = 0;
    private bool mission4Active = false;
    private bool mission4Completed = false;

    private Coroutine missionFlowCoroutine;

    private void Start()
    {
        if (warningUI == null)
        {
            warningUI = EcosystemWarningUI.Instance;
        }

        if (hideMission3DragonsOnStart)
        {
            SetMission3DragonsActive(false);
        }

        if (debugStartMode != DebugStartMode.Normal)
        {
            StartCoroutine(DebugStartRoutine());
        }
    }

    private IEnumerator DebugStartRoutine()
    {
        yield return new WaitForSeconds(debugStartDelay);

        if (objectiveUI != null)
        {
            objectiveUI.HideObjective();
        }

        if (bossWolfDirectionCanvas != null)
        {
            bossWolfDirectionCanvas.HideGuide();
        }

        if (debugStartMode == DebugStartMode.Mission2)
        {
            StartMission2();
        }
        else if (debugStartMode == DebugStartMode.Mission3)
        {
            StartMission3();
        }
        else if (debugStartMode == DebugStartMode.Mission4)
        {
            StartMission4();
        }
    }

    public void StartMissionFlowAfterStory()
    {
        if (debugStartMode != DebugStartMode.Normal)
        {
            return;
        }

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

        objectiveUI.ShowObjective(mission1Title, mission1Description, mission1ShowDuration);

        yield return new WaitForSeconds(mission1ShowDuration);
        yield return new WaitForSeconds(delayBeforeWarning);

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
            yield return new WaitForSeconds(warningShowDuration);
        }

        yield return new WaitForSeconds(delayBeforeMission2Objective);

        StartMission2();
    }

    public void StartMission2()
    {
        bossWolvesKilled = 0;
        mission2Active = true;
        mission2Completed = false;
        mission2Failed = false;

        mission3Active = false;
        mission3Completed = false;
        mission4Active = false;
        mission4Completed = false;

        mission2TimeRemaining = mission2TimeLimit;

        StopMission2Timer();

        if (useMission2TimeLimit && mission2TimeLimit > 0f)
        {
            mission2TimerCoroutine = StartCoroutine(Mission2TimerRoutine());
        }

        if (bossWolfDirectionCanvas != null)
        {
            bossWolfDirectionCanvas.ShowGuide("BossWolf");
        }

        UpdateMission2UI();
    }

    private IEnumerator Mission2TimerRoutine()
    {
        while (mission2Active && !mission2Completed && !mission2Failed)
        {
            mission2TimeRemaining -= 1f;

            if (mission2TimeRemaining < 0f)
            {
                mission2TimeRemaining = 0f;
            }

            UpdateMission2UI();

            if (mission2TimeRemaining <= 0f)
            {
                FailMission2();
                yield break;
            }

            yield return new WaitForSeconds(1f);
        }
    }

    private void StopMission2Timer()
    {
        if (mission2TimerCoroutine != null)
        {
            StopCoroutine(mission2TimerCoroutine);
            mission2TimerCoroutine = null;
        }
    }

    public void RegisterBossWolfKill()
    {
        if (!mission2Active) return;
        if (mission2Completed) return;
        if (mission2Failed) return;

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
        if (objectiveUI == null) return;

        string description =
            "Kill " + bossWolvesRequired + " Boss Wolves.\n" +
            "Progress: " + bossWolvesKilled + " / " + bossWolvesRequired;

        if (useMission2TimeLimit && mission2Active && !mission2Completed && !mission2Failed)
        {
            description += "\n\nTime Left: " + FormatTime(mission2TimeRemaining);
        }

        objectiveUI.ShowObjectivePermanent(mission2Title, description);
    }

    private string FormatTime(float time)
    {
        int totalSeconds = Mathf.CeilToInt(time);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        return minutes.ToString("00") + ":" + seconds.ToString("00");
    }

    private void CompleteMission2()
    {
        mission2Completed = true;
        mission2Active = false;
        mission2Failed = false;

        StopMission2Timer();

        if (bossWolfDirectionCanvas != null)
        {
            bossWolfDirectionCanvas.HideGuide();
        }

        objectiveUI.ShowObjective(
            "Objective Complete",
            "The deer population is safe for now.",
            5f
        );

        if (debugStartMode == DebugStartMode.Normal)
        {
            StartCoroutine(StartMission3AfterMission2Complete());
        }
    }

    private void FailMission2()
    {
        if (mission2Completed) return;
        if (mission2Failed) return;

        mission2Failed = true;
        mission2Active = false;

        StopMission2Timer();

        if (bossWolfDirectionCanvas != null)
        {
            bossWolfDirectionCanvas.HideGuide();
        }

        if (warningUI == null)
        {
            warningUI = EcosystemWarningUI.Instance;
        }

        if (warningUI != null)
        {
            warningUI.ShowWarning(mission2FailWarningMessage, mission2FailObjectiveShowTime);
        }

        if (objectiveUI != null)
        {
            objectiveUI.ShowObjective(
                "Mission Failed",
                "The deer population is extinct.",
                mission2FailObjectiveShowTime
            );
        }

        if (continueToMission3AfterMission2Fail && debugStartMode == DebugStartMode.Normal)
        {
            StartCoroutine(StartMission3AfterMission2Fail());
        }
    }

    private IEnumerator StartMission3AfterMission2Fail()
    {
        yield return new WaitForSeconds(mission2FailObjectiveShowTime + 1f);

        if (warningUI == null)
        {
            warningUI = EcosystemWarningUI.Instance;
        }

        if (warningUI != null)
        {
            warningUI.ShowWarning(mission3WarningMessage, mission3WarningShowDuration);

            float typingTime = 0f;

            if (warningUI.useTypewriterEffect)
            {
                typingTime = mission3WarningMessage.Length * warningUI.typeSpeed;
            }

            yield return new WaitForSeconds(typingTime + mission3WarningShowDuration);
        }
        else
        {
            yield return new WaitForSeconds(mission3WarningShowDuration);
        }

        StartMission3();
    }

    private IEnumerator StartMission3AfterMission2Complete()
    {
        yield return new WaitForSeconds(5f);
        yield return new WaitForSeconds(delayBeforeMission3Warning);

        if (warningUI == null)
        {
            warningUI = EcosystemWarningUI.Instance;
        }

        if (warningUI != null)
        {
            warningUI.ShowWarning(mission3WarningMessage, mission3WarningShowDuration);

            float typingTime = 0f;

            if (warningUI.useTypewriterEffect)
            {
                typingTime = mission3WarningMessage.Length * warningUI.typeSpeed;
            }

            yield return new WaitForSeconds(typingTime + mission3WarningShowDuration);
        }
        else
        {
            yield return new WaitForSeconds(mission3WarningShowDuration);
        }

        StartMission3();
    }

    public void StartMission3()
    {
        mission2Active = false;
        mission2Completed = true;

        dragonsKilled = 0;
        mission3Active = true;
        mission3Completed = false;

        mission4Active = false;
        mission4Completed = false;

        SetMission3DragonsActive(true);
        ResetMissionDragonTargets();

        if (bossWolfDirectionCanvas != null)
        {
            bossWolfDirectionCanvas.ShowGuide("Dragon");
        }

        UpdateMission3UI();
    }

    public void RegisterDragonKill()
    {
        if (!mission3Active) return;
        if (mission3Completed) return;

        dragonsKilled++;

        if (dragonsKilled > dragonsRequired)
        {
            dragonsKilled = dragonsRequired;
        }

        UpdateMission3UI();

        if (dragonsKilled >= dragonsRequired)
        {
            CompleteMission3();
        }
    }

    private void UpdateMission3UI()
    {
        string description =
            "Defeat " + dragonsRequired + " elemental dragons.\n" +
            "Progress: " + dragonsKilled + " / " + dragonsRequired;

        objectiveUI.ShowObjectivePermanent(mission3Title, description);
    }

    private void CompleteMission3()
    {
        mission3Completed = true;
        mission3Active = false;

        if (bossWolfDirectionCanvas != null)
        {
            bossWolfDirectionCanvas.HideGuide();
        }

        objectiveUI.ShowObjective(
            "Objective Complete",
            "The elemental dragons have been defeated.",
            6f
        );

        if (debugStartMode == DebugStartMode.Normal)
        {
            StartCoroutine(StartMission4AfterMission3Complete());
        }
    }

    private IEnumerator StartMission4AfterMission3Complete()
    {
        yield return new WaitForSeconds(6f);
        yield return new WaitForSeconds(delayBeforeMission4Warning);

        if (warningUI == null)
        {
            warningUI = EcosystemWarningUI.Instance;
        }

        if (warningUI != null)
        {
            warningUI.ShowWarning(mission4WarningMessage, mission4WarningShowDuration);

            float typingTime = 0f;

            if (warningUI.useTypewriterEffect)
            {
                typingTime = mission4WarningMessage.Length * warningUI.typeSpeed;
            }

            yield return new WaitForSeconds(typingTime + mission4WarningShowDuration);
        }
        else
        {
            yield return new WaitForSeconds(mission4WarningShowDuration);
        }

        StartMission4();
    }

    public void StartMission4()
    {
        mission2Active = false;
        mission2Completed = true;

        mission3Active = false;
        mission3Completed = true;

        bearsKilled = 0;
        mission4Active = true;
        mission4Completed = false;

        if (bossWolfDirectionCanvas != null)
        {
            bossWolfDirectionCanvas.ShowGuide("Bear");
        }

        UpdateMission4UI();
    }

    public void RegisterBearKill()
    {
        if (!mission4Active) return;
        if (mission4Completed) return;

        bearsKilled++;

        if (bearsKilled > bearsRequired)
        {
            bearsKilled = bearsRequired;
        }

        UpdateMission4UI();

        if (bearsKilled >= bearsRequired)
        {
            CompleteMission4();
        }
    }

    private void UpdateMission4UI()
    {
        string description =
            "Kill " + bearsRequired + " aggressive Bears.\n" +
            "Progress: " + bearsKilled + " / " + bearsRequired;

        objectiveUI.ShowObjectivePermanent(mission4Title, description);
    }

    private void CompleteMission4()
    {
        mission4Completed = true;
        mission4Active = false;

        if (bossWolfDirectionCanvas != null)
        {
            bossWolfDirectionCanvas.HideGuide();
        }

        objectiveUI.ShowObjective(
            "Objective Complete",
            "The bear threat has been controlled. The forest is safer now.",
            6f
        );
    }

    private void SetMission3DragonsActive(bool active)
    {
        if (mission3Dragons == null) return;

        foreach (GameObject dragon in mission3Dragons)
        {
            if (dragon != null)
            {
                dragon.SetActive(active);
            }
        }
    }

    private void ResetMissionDragonTargets()
    {
        if (mission3Dragons == null) return;

        foreach (GameObject dragonObject in mission3Dragons)
        {
            if (dragonObject == null) continue;

            MissionDragonTarget dragonTarget = dragonObject.GetComponent<MissionDragonTarget>();

            if (dragonTarget == null)
            {
                dragonTarget = dragonObject.GetComponentInChildren<MissionDragonTarget>(true);
            }

            if (dragonTarget != null)
            {
                dragonTarget.ResetTargetForMission();
            }
        }
    }
}