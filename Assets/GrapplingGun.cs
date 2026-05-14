using UnityEngine;

public class GrapplingGun : MonoBehaviour
{
    private LineRenderer lr;
    private Vector3 grapplePoint;

    public LayerMask whatIsGrappleable;
    public Transform gunTip, camera, player;

    private PlayerMovement playerMovement;

    [Header("Grapple Settings")]
    public float maxDistance = 100f;

    [Header("Joint Settings")]
    public float maxDistanceMultiplier = 0.45f;
    public float minDistanceMultiplier = 0.15f;
    public float spring = 9f;
    public float damper = 6f;
    public float massScale = 4.5f;

    [Header("Extra Pull Force")]
    public float pullForce = 15f;

    private SpringJoint joint;
    private Rigidbody playerRb;

    private Vector3 currentGrapplePosition;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        playerRb = player.GetComponent<Rigidbody>();
        playerMovement = player.GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && CanStartGrapple())
        {
            StartGrapple();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            StopGrapple();
        }
    }
    void FixedUpdate()
    {
        if (IsGrappling() && playerRb != null)
        {
            Vector3 directionToGrapple = (grapplePoint - player.position).normalized;
            playerRb.AddForce(directionToGrapple * pullForce, ForceMode.Acceleration);
        }
    }

    void LateUpdate()
    {
        DrawRope();
    }

    void StartGrapple()
    {
        RaycastHit hit;

        if (Physics.Raycast(camera.position, camera.forward, out hit, maxDistance, whatIsGrappleable))
        {
            grapplePoint = hit.point;

            joint = player.gameObject.AddComponent<SpringJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = grapplePoint;

            float distanceFromPoint = Vector3.Distance(player.position, grapplePoint);

            // Smaller value = stronger pull toward grapple point
            joint.maxDistance = distanceFromPoint * maxDistanceMultiplier;
            joint.minDistance = distanceFromPoint * minDistanceMultiplier;

            joint.spring = spring;
            joint.damper = damper;
            joint.massScale = massScale;

            lr.positionCount = 2;
            currentGrapplePosition = gunTip.position;
        }
    }

    void StopGrapple()
    {
        lr.positionCount = 0;

        if (joint != null)
        {
            Destroy(joint);
        }
    }

    void DrawRope()
    {
        if (!joint) return;

        currentGrapplePosition = Vector3.Lerp(
            currentGrapplePosition,
            grapplePoint,
            Time.deltaTime * 8f
        );

        lr.SetPosition(0, gunTip.position);
        lr.SetPosition(1, currentGrapplePosition);
    }

    public bool IsGrappling()
    {
        return joint != null;
    }

    public Vector3 GetGrapplePoint()
    {
        return grapplePoint;
    }
    public bool CanStartGrapple()
    {
        if (playerMovement == null) return false;

        // Only gun can grapple
        if (!playerMovement.isGunEquipped) return false;

        bool isInAir = !playerMovement.grounded;
        bool isAimingGun = playerMovement.IsGunAiming();

        // Player can grapple if aiming OR if in air
        return isAimingGun || isInAir;
    }
}