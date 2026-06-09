using UnityEngine;

public class BossWolfMissionTarget : MonoBehaviour
{
    public MissionSequenceManager missionSequenceManager;

    private bool killCounted = false;

    public void CountBossWolfKill()
    {
        if (killCounted) return;

        killCounted = true;

        if (missionSequenceManager != null)
        {
            missionSequenceManager.RegisterBossWolfKill();
        }
        else
        {
            Debug.LogWarning("BossWolfMissionTarget: MissionSequenceManager is not assigned.");
        }
    }
}