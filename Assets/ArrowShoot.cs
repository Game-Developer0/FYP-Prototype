using UnityEngine;

public class ArrowShoot : MonoBehaviour
{
    public enum ArrowType
    {
        Simple,
        Fire,
        Ice,
        Acid,
        Volcano
    }

    [Header("Current Arrow")]
    public ArrowType currentArrowType = ArrowType.Simple;

    [Header("Arrow Prefabs To Shoot")]
    public GameObject SimpleArrowPrefab;
    public GameObject FireArrowPrefab;
    public GameObject IceArrowPrefab;
    public GameObject AcidArrowPrefab;
    public GameObject VolcanoArrowPrefab;

    [Header("Shoot Settings")]
    public Transform ArrowSpawnPosition;
    public Camera playerCam;

    public float range = 1000f;
    public float arrowForce = 40f;

    private RaycastHit hit;
    private PlayerMovement playerMovement;

    private void Awake()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();

        if (playerCam == null)
        {
            playerCam = Camera.main;
        }
    }

    public void SetArrowType(ArrowType newArrowType)
    {
        currentArrowType = newArrowType;
    }

    private GameObject GetCurrentArrowPrefab()
    {
        switch (currentArrowType)
        {
            case ArrowType.Fire:
                return FireArrowPrefab;

            case ArrowType.Ice:
                return IceArrowPrefab;

            case ArrowType.Acid:
                return AcidArrowPrefab;

            case ArrowType.Volcano:
                return VolcanoArrowPrefab;

            default:
                return SimpleArrowPrefab;
        }
    }

    public void Shoot()
    {
        if (playerMovement == null)
        {
            playerMovement = GetComponentInParent<PlayerMovement>();
        }

        if (playerMovement == null)
        {
            playerMovement = FindObjectOfType<PlayerMovement>();
        }

        if (playerMovement != null)
        {
            playerMovement.HideHandArrowBecauseShot();
        }

        GameObject arrowPrefab = GetCurrentArrowPrefab();

        if (arrowPrefab == null)
        {
            Debug.LogWarning("No arrow prefab assigned for: " + currentArrowType);
            return;
        }

        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 targetPoint;

        if (Physics.Raycast(ray, out hit, range))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.origin + ray.direction * range;
        }

        Vector3 shootDirection = (targetPoint - ArrowSpawnPosition.position).normalized;
        Quaternion shootRotation = Quaternion.LookRotation(shootDirection);

        GameObject arrowInstance = Instantiate(
            arrowPrefab,
            ArrowSpawnPosition.position,
            shootRotation
        );

        Rigidbody rb = arrowInstance.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = shootDirection * arrowForce;
        }
    }
}