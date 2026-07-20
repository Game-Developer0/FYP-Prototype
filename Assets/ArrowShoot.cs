using TMPro;
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

    [Header("Arrow Ammunition")]
    [Min(1)]
    public int maximumArrows = 20;

    [Min(0)]
    public int startingArrows = 20;

    [SerializeField]
    private int currentArrows;

    [Header("Arrow UI")]
    public TMP_Text arrowCountText;

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

    public int CurrentArrows
    {
        get { return currentArrows; }
    }

    public int MaximumArrows
    {
        get { return maximumArrows; }
    }

    private void Awake()
    {
        maximumArrows = Mathf.Max(1, maximumArrows);

        currentArrows = Mathf.Clamp(
            startingArrows,
            0,
            maximumArrows
        );

        playerMovement = GetComponentInParent<PlayerMovement>();

        if (playerCam == null)
        {
            playerCam = Camera.main;
        }

        UpdateArrowUI();
    }

    private void OnEnable()
    {
        UpdateArrowUI();
    }

    public void SetArrowType(ArrowType newArrowType)
    {
        currentArrowType = newArrowType;
    }

    public void AddArrows(int arrowsToAdd)
    {
        if (arrowsToAdd <= 0)
        {
            return;
        }

        currentArrows = Mathf.Clamp(
            currentArrows + arrowsToAdd,
            0,
            maximumArrows
        );

        UpdateArrowUI();
    }
    public void ResetArrowsToStartingAmount()
    {
        maximumArrows = Mathf.Max(1, maximumArrows);

        currentArrows = Mathf.Clamp(
            startingArrows,
            0,
            maximumArrows
        );

        UpdateArrowUI();
    }
    private void UpdateArrowUI()
    {
        if (arrowCountText != null)
        {
            arrowCountText.text = currentArrows.ToString();
        }
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
        // Do not shoot when the player has no arrows.
        if (currentArrows <= 0)
        {
            Debug.Log("The player has no arrows.");
            UpdateArrowUI();
            return;
        }

        if (ArrowSpawnPosition == null)
        {
            Debug.LogWarning("Arrow Spawn Position is not assigned.");
            return;
        }

        if (playerCam == null)
        {
            playerCam = Camera.main;
        }

        if (playerCam == null)
        {
            Debug.LogWarning("Player Camera is not assigned.");
            return;
        }

        GameObject arrowPrefab = GetCurrentArrowPrefab();

        if (arrowPrefab == null)
        {
            Debug.LogWarning(
                "No arrow prefab assigned for: " + currentArrowType
            );

            return;
        }

        if (playerMovement == null)
        {
            playerMovement = GetComponentInParent<PlayerMovement>();
        }

        if (playerMovement == null)
        {
            playerMovement = FindObjectOfType<PlayerMovement>();
        }

        Ray ray = playerCam.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        Vector3 targetPoint;

        if (Physics.Raycast(ray, out hit, range))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.origin + ray.direction * range;
        }

        Vector3 shootDirection =
            (targetPoint - ArrowSpawnPosition.position).normalized;

        Quaternion shootRotation =
            Quaternion.LookRotation(shootDirection);

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
        else
        {
            Debug.LogWarning(
                "The spawned arrow does not have a Rigidbody."
            );
        }

        // The arrow was successfully created, so use one arrow.
        currentArrows--;

        currentArrows = Mathf.Clamp(
            currentArrows,
            0,
            maximumArrows
        );

        UpdateArrowUI();

        if (playerMovement != null)
        {
            playerMovement.HideHandArrowBecauseShot();
        }
    }
}