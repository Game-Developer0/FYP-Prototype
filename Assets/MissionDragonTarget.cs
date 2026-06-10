using UnityEngine;

public class MissionDragonTarget : MonoBehaviour
{
    [Header("Target Settings")]
    public string targetID = "Dragon";

    [Header("Red Dot")]
    public GameObject redDotRoot;

    private DragonEnemy dragon;
    private bool killAlreadyNotified = false;

    private void Awake()
    {
        dragon = GetComponent<DragonEnemy>();

        if (dragon == null)
        {
            dragon = GetComponentInParent<DragonEnemy>();
        }

        ShowRedDot(false);
    }

    public bool IsAlive()
    {
        if (!gameObject.activeInHierarchy)
        {
            return false;
        }

        if (dragon == null)
        {
            return true;
        }

        return dragon.currentHealth > 0;
    }

    public void ShowRedDot(bool show)
    {
        if (redDotRoot != null)
        {
            redDotRoot.SetActive(show);
        }
    }

    public void ResetTargetForMission()
    {
        killAlreadyNotified = false;
        ShowRedDot(false);
    }

    public void NotifyKilled()
    {
        if (killAlreadyNotified) return;

        killAlreadyNotified = true;
        ShowRedDot(false);

        MissionSequenceManager missionManager = FindObjectOfType<MissionSequenceManager>();

        if (missionManager != null)
        {
            missionManager.RegisterDragonKill();
        }
        else
        {
            Debug.LogWarning("MissionDragonTarget: MissionSequenceManager not found.");
        }
    }
}