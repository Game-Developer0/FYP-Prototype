using UnityEngine;

public class GrapplingGun : MonoBehaviour
{
    private LineRenderer lr;
    private Vector3 grapplePoint;

    [Header("References")]
    public LayerMask whatIsGrappleable;
    public Transform gunTip;
    public Transform camera;
    public Transform player;

    private PlayerMovement playerMovement;
    private Rigidbody playerRb;

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

    [Header("Grapple Wind Screen Effect")]
    public GameObject windEffectRoot;
    public bool clearWindOnStop = true;
    public float windWarmupTime = 0.15f;

    private ParticleSystem[] windParticles;

    private SpringJoint joint;
    private Vector3 currentGrapplePosition;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();

        if (player != null)
        {
            playerRb = player.GetComponent<Rigidbody>();
            playerMovement = player.GetComponent<PlayerMovement>();
        }

        if (windEffectRoot != null)
        {
            windParticles = windEffectRoot.GetComponentsInChildren<ParticleSystem>(true);
        }
    }

    void Start()
    {
        StopGrappleWindEffect();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && CanStartGrapple())
        {
            StartGrapple();
        }

        if (Input.GetMouseButtonUp(0))
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
        if (camera == null || player == null || gunTip == null) return;

        RaycastHit hit;

        if (Physics.Raycast(camera.position, camera.forward, out hit, maxDistance, whatIsGrappleable))
        {
            grapplePoint = hit.point;

            if (joint != null)
            {
                Destroy(joint);
            }

            joint = player.gameObject.AddComponent<SpringJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = grapplePoint;

            float distanceFromPoint = Vector3.Distance(player.position, grapplePoint);

            joint.maxDistance = distanceFromPoint * maxDistanceMultiplier;
            joint.minDistance = distanceFromPoint * minDistanceMultiplier;

            joint.spring = spring;
            joint.damper = damper;
            joint.massScale = massScale;

            if (lr != null)
            {
                lr.positionCount = 2;
            }

            currentGrapplePosition = gunTip.position;

            PlayGrappleWindEffect();
        }
    }

    void StopGrapple()
    {
        if (lr != null)
        {
            lr.positionCount = 0;
        }

        if (joint != null)
        {
            Destroy(joint);
            joint = null;
        }

        StopGrappleWindEffect();
    }

    void DrawRope()
    {
        if (joint == null || lr == null || gunTip == null) return;

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

        if (!playerMovement.isGunEquipped) return false;

        bool isInAir = !playerMovement.grounded;
        bool isAimingGun = playerMovement.IsGunAiming();

        return isAimingGun || isInAir;
    }

    void PlayGrappleWindEffect()
    {
        if (windEffectRoot == null) return;

        // Keep root active so HS_ScreenEffect can work.
        if (!windEffectRoot.activeSelf)
        {
            windEffectRoot.SetActive(true);
        }

        if (windParticles == null || windParticles.Length == 0)
        {
            windParticles = windEffectRoot.GetComponentsInChildren<ParticleSystem>(true);
        }

        foreach (ParticleSystem particle in windParticles)
        {
            if (particle == null) continue;

            var emission = particle.emission;
            emission.enabled = true;

            particle.Clear(true);
            particle.Play(true);

            if (windWarmupTime > 0f)
            {
                particle.Simulate(windWarmupTime, true, false, true);
            }
        }
    }

    void StopGrappleWindEffect()
    {
        if (windEffectRoot == null) return;

        if (windParticles == null || windParticles.Length == 0)
        {
            windParticles = windEffectRoot.GetComponentsInChildren<ParticleSystem>(true);
        }

        foreach (ParticleSystem particle in windParticles)
        {
            if (particle == null) continue;

            var emission = particle.emission;
            emission.enabled = false;

            particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            if (clearWindOnStop)
            {
                particle.Clear(true);
            }
        }

        // Do NOT disable windEffectRoot.
        // HS_ScreenEffect must stay active.
    }
}