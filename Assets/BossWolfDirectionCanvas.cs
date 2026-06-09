using UnityEngine;

public class BossWolfDirectionCanvas : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public GameObject arrowVisualRoot;

    [Header("Target Settings")]
    public string targetID = "BossWolf";
    public float refreshEvery = 0.25f;

    [Header("Position")]
    public float heightAbovePlayer = 18f;
    public float behindDistance = 4f;

    [Header("Rotation")]
    [Tooltip("Use 90 or -90 depending on your minimap camera direction.")]
    public float flatXRotation = 90f;

    [Tooltip("If arrow points wrong way, try 90, -90, or 180.")]
    public float rotationOffset = 0f;

    private bool guideActive = false;
    private Transform currentDestination;
    private float nextRefreshTime = 0f;

    private void Awake()
    {
        if (arrowVisualRoot != null)
        {
            arrowVisualRoot.SetActive(false);
        }
    }

    private void Update()
    {
        if (!guideActive)
        {
            return;
        }

        FindPlayerIfMissing();

        if (player == null)
        {
            return;
        }

        Vector3 behindOffset = -player.forward * behindDistance;
        transform.position = player.position + Vector3.up * heightAbovePlayer + behindOffset;

        if (Time.time >= nextRefreshTime)
        {
            RefreshDestination();
            nextRefreshTime = Time.time + refreshEvery;
        }

        if (currentDestination == null)
        {
            if (arrowVisualRoot != null)
            {
                arrowVisualRoot.SetActive(false);
            }

            return;
        }

        if (arrowVisualRoot != null && !arrowVisualRoot.activeSelf)
        {
            arrowVisualRoot.SetActive(true);
        }

        Vector3 direction = currentDestination.position - player.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.1f)
        {
            return;
        }

        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(flatXRotation, angle + rotationOffset, 0f);
    }

    public void ShowGuide()
    {
        guideActive = true;

        if (arrowVisualRoot != null)
        {
            arrowVisualRoot.SetActive(true);
        }

        RefreshDestination();
    }

    public void HideGuide()
    {
        guideActive = false;
        currentDestination = null;

        if (arrowVisualRoot != null)
        {
            arrowVisualRoot.SetActive(false);
        }

        HideAllBossWolfRedDots();
    }

    public void RefreshDestination()
    {
        FindPlayerIfMissing();

        if (player == null)
        {
            currentDestination = null;
            return;
        }

        MissionBossWolfTarget nearestAliveBossWolf = GetNearestAliveBossWolf();

        if (nearestAliveBossWolf != null)
        {
            currentDestination = nearestAliveBossWolf.transform;
            return;
        }

        BossWolfAreaPoint nearestUnclearedArea = GetNearestUnclearedArea();

        if (nearestUnclearedArea != null)
        {
            currentDestination = nearestUnclearedArea.transform;
            return;
        }

        currentDestination = null;
    }

    private MissionBossWolfTarget GetNearestAliveBossWolf()
    {
        MissionBossWolfTarget[] targets = FindObjectsOfType<MissionBossWolfTarget>();

        MissionBossWolfTarget nearestTarget = null;
        float nearestDistance = Mathf.Infinity;

        foreach (MissionBossWolfTarget target in targets)
        {
            if (target == null) continue;
            if (target.targetID != targetID) continue;

            if (!target.IsAlive())
            {
                target.ShowRedDot(false);
                continue;
            }

            target.ShowRedDot(true);

            float distance = Vector3.Distance(player.position, target.transform.position);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestTarget = target;
            }
        }

        return nearestTarget;
    }

    private BossWolfAreaPoint GetNearestUnclearedArea()
    {
        BossWolfAreaPoint[] areaPoints = FindObjectsOfType<BossWolfAreaPoint>();

        BossWolfAreaPoint nearestArea = null;
        float nearestDistance = Mathf.Infinity;

        foreach (BossWolfAreaPoint area in areaPoints)
        {
            if (area == null) continue;
            if (area.targetID != targetID) continue;
            if (area.IsCleared()) continue;

            float distance = Vector3.Distance(player.position, area.transform.position);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestArea = area;
            }
        }

        return nearestArea;
    }

    private void HideAllBossWolfRedDots()
    {
        MissionBossWolfTarget[] targets = FindObjectsOfType<MissionBossWolfTarget>();

        foreach (MissionBossWolfTarget target in targets)
        {
            if (target != null)
            {
                target.ShowRedDot(false);
            }
        }
    }

    private void FindPlayerIfMissing()
    {
        if (player != null) return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }
}