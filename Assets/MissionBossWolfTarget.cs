using UnityEngine;

public class MissionBossWolfTarget : MonoBehaviour
{
    [Header("Target Settings")]
    public string targetID = "BossWolf";

    [Header("Red Dot")]
    public GameObject redDotRoot;

    private Enemy enemy;
    private bool killAlreadyNotified = false;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();

        if (enemy == null)
        {
            enemy = GetComponentInParent<Enemy>();
        }

        ShowRedDot(false);
    }

    public bool IsAlive()
    {
        if (!gameObject.activeInHierarchy)
        {
            return false;
        }

        if (enemy == null)
        {
            return true;
        }

        return enemy.currentHealth > 0;
    }

    public void ShowRedDot(bool show)
    {
        if (redDotRoot != null)
        {
            redDotRoot.SetActive(show);
        }
    }

    public void NotifyKilled()
    {
        if (killAlreadyNotified) return;

        killAlreadyNotified = true;

        ShowRedDot(false);

        BossWolfAreaPoint nearestArea = FindNearestAreaPoint();

        if (nearestArea != null)
        {
            nearestArea.RegisterKill();
        }
    }

    public void ResetTargetForSpawn()
    {
        killAlreadyNotified = false;
        ShowRedDot(false);
    }

    private BossWolfAreaPoint FindNearestAreaPoint()
    {
        BossWolfAreaPoint[] areaPoints = FindObjectsOfType<BossWolfAreaPoint>();

        BossWolfAreaPoint nearestArea = null;
        float nearestDistance = Mathf.Infinity;

        foreach (BossWolfAreaPoint area in areaPoints)
        {
            if (area == null) continue;
            if (area.targetID != targetID) continue;

            float distance = Vector3.Distance(transform.position, area.transform.position);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestArea = area;
            }
        }

        return nearestArea;
    }
}