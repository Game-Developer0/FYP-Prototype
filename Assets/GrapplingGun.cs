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

    [Header("Grapple Aim Assist")]
    public bool useAimAssist = true;

    [Tooltip("How close the grapple object must be to the center aim. Bigger value = easier grapple.")]
    public float aimAssistMaxAngle = 8f;

    [Tooltip("Higher value gives closer grapple points more priority.")]
    public float aimAssistDistanceWeight = 2f;

    [Header("Joint Settings")]
    public float maxDistanceMultiplier = 0.45f;
    public float minDistanceMultiplier = 0.15f;
    public float spring = 9f;
    public float damper = 6f;
    public float massScale = 4.5f;

    [Header("Extra Pull Force")]
    public float pullForce = 15f;

    [Header("Canvas Grapple Wind Screen Effect")]
    public GameObject windEffectRoot;
    public bool forceWindParticlesLocalSpace = true;
    public bool disableWindObjectOnStop = true;
    public bool clearWindOnStop = true;
    public float windWarmupTime = 0.15f;

    private ParticleSystem[] windParticles;

    [Header("Grapple Speed Sound")]
    public bool useGrappleSpeedSound = true;

    [Tooltip("Put the looping speed/wind sound here.")]
    public AudioClip grappleSpeedLoopClip;

    [Tooltip("Optional. If empty, the script will use or create an AudioSource on this object.")]
    public AudioSource grappleSpeedAudioSource;

    [Range(0f, 1f)]
    public float grappleSpeedVolume = 0.8f;

    [Tooltip("If true, the sound starts from the beginning every time grapple starts.")]
    public bool restartSpeedSoundOnGrapple = true;

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

        CacheWindParticles();
        SetupGrappleSpeedSound();
    }

    void Start()
    {
        StopGrappleWindEffect();
        StopGrappleSpeedSound();
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

        Vector3 foundGrapplePoint;

        if (!TryFindGrapplePoint(out foundGrapplePoint))
        {
            return;
        }

        grapplePoint = foundGrapplePoint;

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
        PlayGrappleSpeedSound();
    }

    bool TryFindGrapplePoint(out Vector3 foundPoint)
    {
        foundPoint = Vector3.zero;

        RaycastHit directHit;

        if (Physics.Raycast(camera.position, camera.forward, out directHit, maxDistance, whatIsGrappleable))
        {
            foundPoint = directHit.point;
            return true;
        }

        if (!useAimAssist)
        {
            return false;
        }

        Collider[] possibleTargets = Physics.OverlapSphere(
            camera.position,
            maxDistance,
            whatIsGrappleable
        );

        Collider bestTarget = null;
        float bestScore = Mathf.Infinity;

        foreach (Collider target in possibleTargets)
        {
            if (target == null || !target.enabled) continue;

            Vector3 targetCenter = target.bounds.center;
            Vector3 directionToTarget = targetCenter - camera.position;

            float distanceToTarget = directionToTarget.magnitude;

            if (distanceToTarget <= 0.01f) continue;
            if (distanceToTarget > maxDistance) continue;

            float angleToTarget = Vector3.Angle(camera.forward, directionToTarget.normalized);

            if (angleToTarget > aimAssistMaxAngle) continue;

            float distanceScore = distanceToTarget / maxDistance;
            float finalScore = angleToTarget + distanceScore * aimAssistDistanceWeight;

            if (finalScore < bestScore)
            {
                bestScore = finalScore;
                bestTarget = target;
            }
        }

        if (bestTarget == null)
        {
            return false;
        }

        Ray rayToBestTarget = new Ray(
            camera.position,
            (bestTarget.bounds.center - camera.position).normalized
        );

        RaycastHit surfaceHit;

        if (bestTarget.Raycast(rayToBestTarget, out surfaceHit, maxDistance))
        {
            foundPoint = surfaceHit.point;
        }
        else
        {
            foundPoint = bestTarget.bounds.center;
        }

        return true;
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
        StopGrappleSpeedSound();
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

    void CacheWindParticles()
    {
        if (windEffectRoot == null)
        {
            windParticles = null;
            return;
        }

        windParticles = windEffectRoot.GetComponentsInChildren<ParticleSystem>(true);
    }

    void PlayGrappleWindEffect()
    {
        if (windEffectRoot == null) return;

        windEffectRoot.SetActive(true);

        CacheWindParticles();

        if (windParticles == null || windParticles.Length == 0) return;

        foreach (ParticleSystem particle in windParticles)
        {
            if (particle == null) continue;

            particle.gameObject.SetActive(true);

            ParticleSystem.MainModule main = particle.main;

            if (forceWindParticlesLocalSpace)
            {
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
            }

            main.loop = true;

            ParticleSystem.EmissionModule emission = particle.emission;
            emission.enabled = true;

            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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

        CacheWindParticles();

        if (windParticles != null)
        {
            foreach (ParticleSystem particle in windParticles)
            {
                if (particle == null) continue;

                ParticleSystem.EmissionModule emission = particle.emission;
                emission.enabled = false;

                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                if (clearWindOnStop)
                {
                    particle.Clear(true);
                }
            }
        }

        if (disableWindObjectOnStop)
        {
            windEffectRoot.SetActive(false);
        }
    }

    void SetupGrappleSpeedSound()
    {
        if (!useGrappleSpeedSound) return;

        if (grappleSpeedAudioSource == null)
        {
            grappleSpeedAudioSource = GetComponent<AudioSource>();
        }

        if (grappleSpeedAudioSource == null && grappleSpeedLoopClip != null)
        {
            grappleSpeedAudioSource = gameObject.AddComponent<AudioSource>();
        }

        if (grappleSpeedAudioSource == null) return;

        if (grappleSpeedLoopClip != null)
        {
            grappleSpeedAudioSource.clip = grappleSpeedLoopClip;
        }

        grappleSpeedAudioSource.loop = true;
        grappleSpeedAudioSource.playOnAwake = false;
        grappleSpeedAudioSource.volume = grappleSpeedVolume;
    }

    void PlayGrappleSpeedSound()
    {
        if (!useGrappleSpeedSound) return;

        if (grappleSpeedAudioSource == null)
        {
            SetupGrappleSpeedSound();
        }

        if (grappleSpeedAudioSource == null) return;
        if (grappleSpeedAudioSource.clip == null) return;

        grappleSpeedAudioSource.loop = true;
        grappleSpeedAudioSource.volume = grappleSpeedVolume;

        if (restartSpeedSoundOnGrapple)
        {
            grappleSpeedAudioSource.Stop();
            grappleSpeedAudioSource.Play();
        }
        else
        {
            if (!grappleSpeedAudioSource.isPlaying)
            {
                grappleSpeedAudioSource.Play();
            }
        }
    }

    void StopGrappleSpeedSound()
    {
        if (grappleSpeedAudioSource == null) return;

        if (grappleSpeedAudioSource.isPlaying)
        {
            grappleSpeedAudioSource.Stop();
        }
    }
}