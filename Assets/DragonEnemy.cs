using UnityEngine;
using System.Collections;

public class DragonEnemy : MonoBehaviour
{

    [Header("Target")]
    public Transform playerTarget;

    [Header("Player Auto Detection")]
    public bool autoFindPlayerByTag = true;
    public string playerTag = "Player";
    public bool showPlayerFindWarnings = true;

    private bool playerMissingWarningShown = false;
    private bool playerTagWarningShown = false;

    [Header("Components")]
    public Rigidbody rb;
    public Animator animator;

    [Header("Zone")]
    public Transform zoneCenter;
    public float detectionRange = 45f;
    public float losePlayerRange = 75f;

    [Header("Start Setting")]
    public bool startInSky = false;
    public Transform ownerRoot;

    [Header("Combat Mode Switching")]
    public bool canUseGroundCombat = true;
    public bool canUseSkyCombat = true;

    public float groundCombatMinTime = 6f;
    public float groundCombatMaxTime = 12f;

    public float skyCombatMinTime = 7f;
    public float skyCombatMaxTime = 14f;

    [Range(0f, 1f)]
    public float chanceToTakeOffAfterGroundTime = 0.6f;

    [Range(0f, 1f)]
    public float chanceToLandAfterSkyTime = 0.45f;

    [Header("Fall Recovery")]
    public float recoverAfterFallTime = 2.5f;

    [Range(0f, 1f)]
    public float chanceToFlyAgainAfterFall = 0.5f;

    [Header("Falling Ground Check")]
    public LayerMask groundLayer;
    public float fallingGroundRayDistance = 5f;
    public float minimumFallAnimationTime = 1.2f;

    private float fallStartedTime = 0f;
    private float skyDeathStartedTime = 0f;
    public float fallGroundCheckHeight = 1.5f;


    private float nextCombatModeChangeTime = 0f;
    private bool isRecoveringFromFall = false;
    private float fallRecoverEndTime = 0f;

    [Header("Ground Movement")]
    public bool canWalkOnGroundBeforeDetection = true;
    public float groundPatrolRadius = 12f;
    public float groundWalkSpeed = 2f;
    public float groundRunSpeed = 5f;
    public float passiveWaitMin = 3f;
    public float passiveWaitMax = 6f;

    [Header("Flying Movement")]
    public float skyPatrolRadius = 30f;
    public float minFlyHeight = 15f;
    public float maxFlyHeight = 28f;
    public float chaseHeightAbovePlayer = 12f;

    public float takeOffForce = 12f;
    public float takeOffDuration = 2.2f;

    public float flyPatrolSpeed = 8f;
    public float flyChaseSpeed = 14f;
    public float landingSpeed = 8f;
    public float flightAcceleration = 6f;
    public float maxFlightSpeed = 22f;
    public float hoverBrakeForce = 5f;
    public float turnSpeed = 4f;

    [Header("Landing Ground Fix")]
    public float landingRayStartHeight = 80f;
    public float landingRayDistance = 160f;
    public float landingGroundOffset = 1f;
    public float landingAnimationStartHeight = 4f;
    public float landingFinishDistance = 0.8f;

    private Vector3 currentLandingPoint;
    private bool landingAnimationStarted = false;

    [Header("Landing Animation Finish Control")]
    public string flyStationaryToLandingStateName = "FlyStationaryToLanding";
    public string glideToLandingStateName = "GlideToLanding";

    public float minimumLandingAnimationTime = 1.2f;
    public float landingAnimationFinishNormalizedTime = 0.9f;

    private float landingAnimationStartedTime = 0f;
    private bool dragonReachedLandingPoint = false;

    [Header("Attack")]
    public float groundBiteRange = 5f;
    public float groundSpreadAcidRange = 12f;
    public float groundSpitAcidRange = 22f;
    public float skyAttackRange = 24f;
    public float attackCooldown = 3f;
    public int biteDamage = 20;

    [Range(0f, 1f)]
    public float closeRangeBiteChance = 0.5f;

    [Range(0f, 1f)]
    public float mediumRangeSpreadChance = 0.6f;

    private float nextAttackTime = 0f;

    [Header("Projectile Attack")]
    public GameObject fireballPrefab;
    public GameObject acidBallPrefab;
    public Transform firePoint;
    public float projectileSpeed = 28f;

    [Header("Projectile Shoot Safety")]
    public float projectileShootLockTime = 0.8f;

    private bool projectileShotDuringThisAttack = false;
    private float nextAllowedProjectileShootTime = 0f;

    [Tooltip("If true, add Animation Event on attack animation and call ShootFireballFromAnimation or ShootAcidFromAnimation.")]
    public bool useAnimationEventsForProjectiles = false;

    [Header("Health")]
    public int maxHealth = 300;
    public int currentHealth;
    public int defaultArrowDamage = 20;

    [Tooltip("This is still used for falling from sky after some hits.")]
    public int arrowHits = 0;

    [Tooltip("Dragon will fall from sky after this many arrow hits, but death uses health now.")]
    public int fallAfterHits = 4;

    public float destroyAfterDeath = 7f;

    [Header("Dragon Health Bar")]
    public DragonHealthBar healthBar;
    public GameObject healthBarRoot;
    public bool hideHealthBarUntilHit = true;

    private bool hasAlreadyFallenFromSky = false;

    [Header("Hit Reaction")]
    public float groundHitStunDuration = 0.8f;
    public string getHit1StateName = "GetHit1";

    private bool isGroundHitStunned = false;
    private float groundHitStunEndTime = 0f;

    [Header("Animator Bool Names")]
    public string isFlyingBoolName = "IsFlying";
    public string isWalkingBoolName = "IsWalking";
    public string isRunningBoolName = "IsRunning";
    public string isGlidingBoolName = "IsGliding";
    public string isFallingBoolName = "IsFalling";
    public string isDeadBoolName = "IsDead";

    [Header("Animator Trigger Names")]
    public string takeOffTriggerName = "TakeOff";
    public string landTriggerName = "Land";
    public string biteAttackTriggerName = "BiteAttack";
    public string spitAcidTriggerName = "SpitAcid";
    public string spreadAcidBreathTriggerName = "SpreadAcidBreath";
    public string getHit1TriggerName = "GetHit1";
    public string flyGetHitTriggerName = "FlyGetHit";
    public string fallFromHitTriggerName = "FallFromHit";
    public string deathHitGroundTriggerName = "DeathHitGround";

    [Header("Animation State Names")]
    public string groundIdleStateName = "IdleBreathe";
    public string groundDeathStateName = "Death";
    public string skyDeathStartStateName = "FlyGetHitToFalling";
    public string skyFallingStateName = "Falling";
    public string deathHitGroundStateName = "DeathHitTheGround";

    [Header("Fall Recovery Landing Animation")]
    public string fallRecoveryLandingStateName = "FlyStationaryToLanding";

    [Header("Sky Death")]
    public float skyDeathGroundCheckHeight = 1.5f;

    private bool isSkyDeath = false;
    private bool skyDeathHitGround = false;

    private Vector3 zoneCenterPosition;
    private Vector3 currentSkyPatrolPoint;
    private Vector3 currentGroundPatrolPoint;

    private bool hasDetectedPlayer = false;
    private bool isFlying = false;
    private bool isTakingOff = false;
    private bool isLanding = false;
    private bool isFalling = false;
    private bool isDead = false;

    private bool wantsFlyMove = false;
    private bool wantsGroundMove = false;
    private bool wantsHover = false;

    private bool animWalking = false;
    private bool animRunning = false;
    public bool animGliding = false;

    private Vector3 flyMoveTarget;
    private float currentFlySpeed;

    private Vector3 groundMoveTarget;
    private float currentGroundSpeed;

    private bool isGroundWalkingInZone = false;
    private float nextPassiveChangeTime = 0f;
    private float takeOffEndTime = 0f;

    private float detectionCheckRate = 0.2f;
    private float nextDetectionCheckTime = 0f;

    private PlayerHealth playerHealth;

    [Header("Mouth Aim")]
    public bool aimMouthAtPlayer = true;
    public float mouthAimSpeed = 25f;
    public Vector3 mouthTargetOffset = new Vector3(0f, 1.2f, 0f);

    [Header("Spread Acid Breath")]
    public ParticleSystem spreadAcidParticle;
    public Transform spreadAcidPoint;

    public float spreadAcidBreathDuration = 2f;
    public int spreadAcidDamage = 8;
    public float spreadAcidDamageRange = 16f;
    public float spreadAcidAngle = 55f;
    public float spreadAcidDamageInterval = 0.4f;

    public LayerMask spreadAcidBlockLayers;

    private Coroutine spreadAcidRoutine;
    private float nextSpreadAcidDamageTime = 0f;

    [Header("Breath Player Effect")]
    public PlayerHealth.DamageEffectType breathEffectType = PlayerHealth.DamageEffectType.Acid;
    public float breathScreenEffectDuration = 0.7f;

    public bool breathSlowsPlayer = true;
    public float breathSlowMultiplier = 0.55f;
    public float breathSlowDuration = 1f;

    [Header("Spread Acid Player Hit Effect")]
    public GameObject spreadAcidHitEffectPrefab;
    public Transform playerAcidHitPoint;

    [Header("Player Acid Hit Point Auto Detection")]
    public bool autoFindPlayerAcidHitPoint = true;
    public string playerAcidHitPointChildName = "PlayerAcidHitPoint";
    public string playerAcidHitPointTag = "PlayerAcidHitPoint";
    public bool searchAcidHitPointByTagIfChildNotFound = true;

    public float spreadAcidHitEffectDestroyTime = 1.5f;
    public Vector3 spreadAcidHitEffectScale = Vector3.one;

    [Header("Spread Acid Damage Timing")]
    public bool autoCalculateSpreadAcidHitDelay = true;
    public float spreadAcidParticleVisualSpeed = 20f;
    public float manualSpreadAcidFirstDamageDelay = 0.6f;
    public float minSpreadAcidFirstDamageDelay = 0.15f;
    public float maxSpreadAcidFirstDamageDelay = 1.2f;

    [Header("Dragon Audio")]
    public AudioSource acidBreathAudioSource;
    public AudioClip spreadAcidBreathClip;

    public AudioSource fireballAudioSource;
    public AudioClip fireballShootClip;

    public AudioSource wingAudioSource;

    [Header("Dragon Wing Flying Sounds")]
    [Tooltip("Add all dragon wing flap / flying sounds here.")]
    public AudioClip[] dragonWingFlyingClips;

    [Tooltip("Normal flying wing volume.")]
    [Range(0f, 1f)]
    public float dragonWingFlyVolume = 0.75f;

    [Tooltip("Louder takeoff wing volume.")]
    [Range(0f, 1f)]
    public float dragonWingTakeOffVolume = 0.9f;

    [Tooltip("Normal flying pitch variation.")]
    public Vector2 dragonWingFlyPitchRange = new Vector2(0.9f, 1.08f);

    [Tooltip("Takeoff pitch variation.")]
    public Vector2 dragonWingTakeOffPitchRange = new Vector2(0.85f, 1.02f);

    [Tooltip("Protection against duplicate wing animation events.")]
    public float dragonMinWingSoundInterval = 0.15f;

    [Tooltip("0 = 2D sound, 1 = full 3D sound.")]
    [Range(0f, 1f)]
    public float dragonWingSpatialBlend = 1f;

    private float nextAllowedDragonWingSoundTime = 0f;
    private AudioClip lastDragonWingClip;

    public AudioSource footstepAudioSource;

    [Header("Dragon Footstep Sounds")]
    [Tooltip("Add all dragon grass footstep sounds here.")]
    public AudioClip[] dragonGrassFootstepClips;

    [Tooltip("Soft walking volume.")]
    [Range(0f, 1f)]
    public float dragonWalkFootstepVolume = 0.45f;

    [Tooltip("Loud running volume.")]
    [Range(0f, 1f)]
    public float dragonRunFootstepVolume = 0.85f;

    public Vector2 dragonWalkPitchRange = new Vector2(0.9f, 1.05f);
    public Vector2 dragonRunPitchRange = new Vector2(1.0f, 1.15f);

    [Tooltip("Protection against duplicate animation events.")]
    public float dragonMinFootstepInterval = 0.12f;

    [Tooltip("0 = 2D sound, 1 = full 3D sound.")]
    [Range(0f, 1f)]
    public float dragonFootstepSpatialBlend = 1f;

    private float nextAllowedDragonFootstepTime = 0f;
    private AudioClip lastDragonFootstepClip;

    public AudioSource hitAudioSource;
    public AudioClip dragonHitClip;

    [Header("Wing Audio Settings")]
    public float wingAudioStopDelay = 0.35f;

    [Header("Dragon Audio Area")]
    public bool onlyPlayAudioInsideDragonArea = true;

    [Tooltip("If this is 0, script will use detectionRange as the audio area.")]
    public float customAudioAreaRange = 0f;

    private float wingAudioShouldStopTime = 0f;
    private float footstepAudioShouldStopTime = 0f;
    void Start()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (animator == null)
            animator = GetComponent<Animator>();

        currentHealth = maxHealth;
        SetupDragonHealthBar();

        if (zoneCenter != null)
            zoneCenterPosition = zoneCenter.position;
        else
            zoneCenterPosition = transform.position;

        FindPlayerTargetIfMissing();

        if (playerTarget != null)
            playerHealth = playerTarget.GetComponent<PlayerHealth>();

        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.useGravity = !startInSky;
            rb.isKinematic = false;
        }

        SetupDragonAudio();

        isFlying = startInSky;

        if (isFlying)
            ChooseNewSkyPatrolPoint();
        else
            ChooseNewGroundPatrolPoint();

        SetNextCombatModeChangeTime();
        nextPassiveChangeTime = Time.time + Random.Range(passiveWaitMin, passiveWaitMax);

        UpdateAnimatorBools();
    }
    void Update()
    {
        if (isSkyDeath)
            return;

        if (isDead)
            return;

        if (!FindPlayerTargetIfMissing())
        {
            return;
        }

        HandleDetection();

        if (isTakingOff || isLanding || isFalling || isRecoveringFromFall)
        {
            if (isRecoveringFromFall)
                HandleFallRecovery();

            UpdateAnimatorBools();
            return;
        }

        if (isGroundHitStunned)
        {
            HandleGroundHitStun();
            UpdateAnimatorBools();
            return;
        }

        ResetMovementRequests();

        if (hasDetectedPlayer)
            CombatBehaviour();
        else
            PassiveBehaviour();

        UpdateAnimatorBools();
    }


    void FixedUpdate()
    {
        if (rb == null) return;

        if (isSkyDeath)
        {
            HandleSkyDeathFalling();
            return;
        }

        if (isDead) return;

        if (isGroundHitStunned)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        if (isTakingOff)
        {
            HandleTakeOffMovement();
            return;
        }

        if (isLanding)
        {
            HandleLandingMovement();
            return;
        }

        if (isFalling)
        {
            HandleFallingMovement();
            return;
        }

        if (wantsFlyMove)
        {
            FlyToward(flyMoveTarget, currentFlySpeed);
        }
        else if (wantsHover)
        {
            HoverInPlace();
        }

        if (wantsGroundMove)
        {
            MoveOnGround(groundMoveTarget, currentGroundSpeed);
        }
    }
    bool FindPlayerTargetIfMissing()
    {
        if (playerTarget != null)
        {
            if (playerHealth == null)
            {
                playerHealth = playerTarget.GetComponent<PlayerHealth>();
            }

            FindPlayerAcidHitPointIfMissing();

            return true;
        }

        if (!autoFindPlayerByTag)
        {
            if (showPlayerFindWarnings && !playerMissingWarningShown)
            {
                Debug.LogWarning("Player Target is missing and autoFindPlayerByTag is disabled.", this);
                playerMissingWarningShown = true;
            }

            return false;
        }

        if (string.IsNullOrEmpty(playerTag))
        {
            if (showPlayerFindWarnings && !playerTagWarningShown)
            {
                Debug.LogWarning("Player tag name is empty in DragonEnemy script.", this);
                playerTagWarningShown = true;
            }

            return false;
        }

        try
        {
            GameObject foundPlayer = GameObject.FindGameObjectWithTag(playerTag);

            if (foundPlayer != null)
            {
                playerTarget = foundPlayer.transform;
                playerHealth = foundPlayer.GetComponent<PlayerHealth>();

                FindPlayerAcidHitPointIfMissing();

                playerMissingWarningShown = false;
                playerTagWarningShown = false;

                return true;
            }

            if (showPlayerFindWarnings && !playerMissingWarningShown)
            {
                Debug.LogWarning(
                    "No object with tag '" + playerTag + "' was found. Make sure your Player object has Tag = " + playerTag + ".",
                    this
                );

                playerMissingWarningShown = true;
            }
        }
        catch
        {
            if (showPlayerFindWarnings && !playerTagWarningShown)
            {
                Debug.LogWarning(
                    "Tag '" + playerTag + "' does not exist. Go to Inspector > Tag > Add Tag and create a tag named '" + playerTag + "'.",
                    this
                );

                playerTagWarningShown = true;
            }
        }

        return false;
    }
    void FindPlayerAcidHitPointIfMissing()
    {
        if (!autoFindPlayerAcidHitPoint)
            return;

        if (playerAcidHitPoint != null)
            return;

        if (playerTarget == null)
            return;

        if (!string.IsNullOrEmpty(playerAcidHitPointChildName))
        {
            Transform foundChild = FindChildRecursive(playerTarget, playerAcidHitPointChildName);

            if (foundChild != null)
            {
                playerAcidHitPoint = foundChild;
                return;
            }
        }

        if (!searchAcidHitPointByTagIfChildNotFound)
            return;

        if (string.IsNullOrEmpty(playerAcidHitPointTag))
            return;

        try
        {
            GameObject foundByTag = GameObject.FindGameObjectWithTag(playerAcidHitPointTag);

            if (foundByTag != null)
            {
                playerAcidHitPoint = foundByTag.transform;
            }
        }
        catch
        {
            Debug.LogWarning(
                "Tag '" + playerAcidHitPointTag + "' does not exist. " +
                "Either create this tag or use the child name method with an object named '" + playerAcidHitPointChildName + "'.",
                this
            );
        }
    }

    Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }

            Transform found = FindChildRecursive(child, childName);

            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
    void HandleDetection()
    {
        if (Time.time < nextDetectionCheckTime) return;

        nextDetectionCheckTime = Time.time + detectionCheckRate;

        float distanceFromZoneToPlayer = Vector3.Distance(GetZoneCenter(), playerTarget.position);

        if (!hasDetectedPlayer && distanceFromZoneToPlayer <= detectionRange)
        {
            hasDetectedPlayer = true;
            SetNextCombatModeChangeTime();
        }

        if (hasDetectedPlayer && distanceFromZoneToPlayer > losePlayerRange)
        {
            hasDetectedPlayer = false;

            if (isFlying)
                ChooseNewSkyPatrolPoint();
            else
                ChooseNewGroundPatrolPoint();

            SetNextCombatModeChangeTime();
        }
    }

    void PassiveBehaviour()
    {
        if (isFlying)
            SkyPatrol();
        else
            GroundPassive();
    }

    void CombatBehaviour()
    {
        HandleCombatModeSwitching();

        if (isFlying)
            SkyCombat();
        else
            GroundCombat();
    }

    void HandleCombatModeSwitching()
    {
        if (Time.time < nextCombatModeChangeTime) return;

        if (!canUseGroundCombat && canUseSkyCombat && !isFlying)
        {
            StartTakeOff();
            return;
        }

        if (!canUseSkyCombat && canUseGroundCombat && isFlying)
        {
            StartLanding();
            return;
        }

        if (isFlying)
        {
            if (canUseGroundCombat && Random.value <= chanceToLandAfterSkyTime)
            {
                StartLanding();
                return;
            }
        }
        else
        {
            if (canUseSkyCombat && Random.value <= chanceToTakeOffAfterGroundTime)
            {
                StartTakeOff();
                return;
            }
        }

        SetNextCombatModeChangeTime();
    }

    void SetNextCombatModeChangeTime()
    {
        if (isFlying)
            nextCombatModeChangeTime = Time.time + Random.Range(skyCombatMinTime, skyCombatMaxTime);
        else
            nextCombatModeChangeTime = Time.time + Random.Range(groundCombatMinTime, groundCombatMaxTime);
    }

    void GroundPassive()
    {
        animRunning = false;
        animGliding = false;

        if (!canWalkOnGroundBeforeDetection)
        {
            animWalking = false;
            return;
        }

        if (isGroundWalkingInZone)
        {
            wantsGroundMove = true;
            groundMoveTarget = currentGroundPatrolPoint;
            currentGroundSpeed = groundWalkSpeed;

            animWalking = true;

            float distance = Vector3.Distance(
                new Vector3(transform.position.x, 0f, transform.position.z),
                new Vector3(currentGroundPatrolPoint.x, 0f, currentGroundPatrolPoint.z)
            );

            if (distance <= 1f)
            {
                isGroundWalkingInZone = false;
                animWalking = false;
                nextPassiveChangeTime = Time.time + Random.Range(passiveWaitMin, passiveWaitMax);
            }

            return;
        }

        animWalking = false;

        if (Time.time >= nextPassiveChangeTime)
        {
            ChooseNewGroundPatrolPoint();
            isGroundWalkingInZone = true;
        }
    }

    void GroundCombat()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        if (distanceToPlayer <= groundBiteRange)
        {
            animWalking = false;
            animRunning = false;

            FaceTarget(playerTarget.position);
            TryRandomCloseGroundAttack();
        }
        else if (distanceToPlayer <= groundSpreadAcidRange)
        {
            animWalking = false;
            animRunning = false;

            FaceTarget(playerTarget.position);
            TryRandomMediumGroundAttack();
        }
        else if (distanceToPlayer <= groundSpitAcidRange)
        {
            animWalking = false;
            animRunning = false;

            FaceTarget(playerTarget.position);
            TryGroundSpitAcidAttack();
        }
        else
        {
            wantsGroundMove = true;
            groundMoveTarget = playerTarget.position;
            currentGroundSpeed = groundRunSpeed;

            animWalking = false;
            animRunning = true;
        }
    }

    void TryRandomCloseGroundAttack()
    {
        if (Time.time < nextAttackTime) return;

        float randomValue = Random.value;

        if (randomValue <= closeRangeBiteChance)
        {
            SetTriggerIfExists(biteAttackTriggerName);
            DamagePlayerByBite();
        }
        else if (randomValue <= 0.75f)
        {
            SetTriggerIfExists(spreadAcidBreathTriggerName);

            if (!useAnimationEventsForProjectiles)
            {
                StartSpreadAcidBreathFromAnimation();
            }
        }
        else
        {
            BeginNewProjectileAttack();

            SetTriggerIfExists(spitAcidTriggerName);

            if (!useAnimationEventsForProjectiles)
            {
                ShootFireballFromAnimation();
            }
        }

        nextAttackTime = Time.time + attackCooldown;
    }

    void TryRandomMediumGroundAttack()
    {
        if (Time.time < nextAttackTime) return;

        if (Random.value <= mediumRangeSpreadChance)
        {
            SetTriggerIfExists(spreadAcidBreathTriggerName);

            if (!useAnimationEventsForProjectiles)
            {
                StartSpreadAcidBreathFromAnimation();
            }
        }
        else
        {
            BeginNewProjectileAttack();

            SetTriggerIfExists(spitAcidTriggerName);

            if (!useAnimationEventsForProjectiles)
            {
                ShootFireballFromAnimation();
            }
        }

        nextAttackTime = Time.time + attackCooldown;
    }

    void TryGroundSpitAcidAttack()
    {
        if (Time.time < nextAttackTime) return;

        BeginNewProjectileAttack();

        SetTriggerIfExists(spitAcidTriggerName);

        if (!useAnimationEventsForProjectiles)
            ShootFireballFromAnimation();

        nextAttackTime = Time.time + attackCooldown;
    }

    void SkyPatrol()
    {
        float distanceToPatrolPoint = Vector3.Distance(transform.position, currentSkyPatrolPoint);

        if (distanceToPatrolPoint <= 3f)
        {
            ChooseNewSkyPatrolPoint();
        }

        wantsFlyMove = true;
        flyMoveTarget = currentSkyPatrolPoint;
        currentFlySpeed = flyPatrolSpeed;

        animWalking = false;
        animRunning = false;

        animGliding = currentSkyPatrolPoint.y < transform.position.y - 2f;
    }

    void SkyCombat()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        if (distanceToPlayer <= skyAttackRange)
        {
            wantsFlyMove = false;
            wantsHover = true;

            animWalking = false;
            animRunning = false;
            animGliding = false;

            FaceTarget(playerTarget.position);
            TrySkyAttack();
        }
        else
        {
            Vector3 chasePosition = playerTarget.position + Vector3.up * chaseHeightAbovePlayer;

            wantsFlyMove = true;
            flyMoveTarget = chasePosition;
            currentFlySpeed = flyChaseSpeed;

            animWalking = false;
            animRunning = false;
            animGliding = false;
        }
    }


    void StartTakeOff()
    {
        if (isDead) return;
        if (isFlying) return;
        if (isTakingOff) return;
        if (!canUseSkyCombat && hasDetectedPlayer) return;

        isTakingOff = true;
        takeOffEndTime = Time.time + takeOffDuration;

        wantsGroundMove = false;
        animWalking = false;
        animRunning = false;
        animGliding = false;

        // Stop footstep audio immediately when dragon starts flying
        if (footstepAudioSource != null && footstepAudioSource.isPlaying)
        {
            footstepAudioSource.Stop();
        }

        // Start wing audio immediately, not after takeoff finishes
        StartWingAudioNow();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(Vector3.up * takeOffForce, ForceMode.VelocityChange);
        }

        SetTriggerIfExists(takeOffTriggerName);

        UpdateAnimatorBools();
    }
    void StartWingAudioNow()
    {
        // Old version started a looping wing sound.
        // New version plays one immediate wing sound.
        PlayDragonWingSound();
    }

    void HandleTakeOffMovement()
    {
        Vector3 targetHeight = transform.position;
        targetHeight.y = GetZoneCenter().y + minFlyHeight;

        FlyToward(targetHeight, flyPatrolSpeed);

        if (Time.time >= takeOffEndTime || transform.position.y >= GetZoneCenter().y + minFlyHeight - 1f)
        {
            isTakingOff = false;
            isFlying = true;

            if (rb != null)
            {
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;
            }

            ChooseNewSkyPatrolPoint();
            SetNextCombatModeChangeTime();
        }

        UpdateAnimatorBools();
    }

    void StartLanding()
    {
        if (isDead) return;
        if (!isFlying) return;
        if (isLanding) return;
        if (!canUseGroundCombat && hasDetectedPlayer) return;

        isLanding = true;
        landingAnimationStarted = false;
        dragonReachedLandingPoint = false;
        landingAnimationStartedTime = 0f;

        currentLandingPoint = GetLandingPointBelowDragon();

        wantsFlyMove = false;
        wantsHover = false;
        wantsGroundMove = false;

        animWalking = false;
        animRunning = false;
        animGliding = false;

        StopWingAudioNow();

        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Do not trigger Land here.
        // Landing animation starts only when dragon is close to ground.
        UpdateAnimatorBools();
    }

    void HandleLandingMovement()
    {
        currentLandingPoint = GetLandingPointBelowDragon();

        float heightFromGround = transform.position.y - currentLandingPoint.y;

        StopWingAudioNow();

        if (!dragonReachedLandingPoint)
        {
            FlyTowardWithoutChangingRotation(currentLandingPoint, landingSpeed);

            if (playerTarget != null)
                FaceTargetFlat(playerTarget.position);
            else
                FaceTargetFlat(currentLandingPoint + transform.forward * 5f);

            if (!landingAnimationStarted && heightFromGround <= landingAnimationStartHeight)
            {
                landingAnimationStarted = true;
                landingAnimationStartedTime = Time.time;

                ResetAllDragonTriggers();
                SetTriggerIfExists(landTriggerName);
            }

            float distanceToLandingPoint = Vector3.Distance(transform.position, currentLandingPoint);

            if (distanceToLandingPoint <= landingFinishDistance)
            {
                dragonReachedLandingPoint = true;

                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.useGravity = false;
                }

                transform.position = currentLandingPoint;
            }

            UpdateAnimatorBools();
            return;
        }

        // Dragon is now on/near ground, but we keep it in landing mode
        // until the landing animation has finished.
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
        }

        bool landingAnimationFinished = HasLandingAnimationFinished();

        if (!landingAnimationFinished)
        {
            UpdateAnimatorBools();
            return;
        }

        isLanding = false;
        isFlying = false;
        landingAnimationStarted = false;
        dragonReachedLandingPoint = false;

        wantsFlyMove = false;
        wantsHover = false;
        wantsGroundMove = false;

        animWalking = false;
        animRunning = false;
        animGliding = false;

        if (rb != null)
        {
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        StopWingAudioNow();

        ChooseNewGroundPatrolPoint();
        SetNextCombatModeChangeTime();

        UpdateAnimatorBools();
    }
    bool HasLandingAnimationFinished()
    {
        if (!landingAnimationStarted)
            return true;

        if (Time.time < landingAnimationStartedTime + minimumLandingAnimationTime)
            return false;

        if (animator == null)
            return true;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        bool isInLandingState =
            stateInfo.IsName(flyStationaryToLandingStateName) ||
            stateInfo.IsName(glideToLandingStateName);

        if (!isInLandingState)
        {
            // If animator already left landing after minimum time, allow script to finish.
            return true;
        }

        return stateInfo.normalizedTime >= landingAnimationFinishNormalizedTime;
    }
    void FaceTargetFlat(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;

        // Remove up/down rotation.
        // Dragon will only rotate left/right.
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);

        Quaternion smoothRotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime
        );

        if (rb != null)
            rb.MoveRotation(smoothRotation);
        else
            transform.rotation = smoothRotation;
    }
    void FlyTowardWithoutChangingRotation(Vector3 targetPosition, float moveSpeed)
    {
        if (rb == null) return;

        Vector3 direction = targetPosition - transform.position;

        if (direction.magnitude < 0.2f)
            return;

        Vector3 desiredVelocity = direction.normalized * moveSpeed;
        Vector3 steeringForce = (desiredVelocity - rb.linearVelocity) * flightAcceleration;

        rb.AddForce(steeringForce, ForceMode.Acceleration);

        if (rb.linearVelocity.magnitude > maxFlightSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxFlightSpeed;
        }
    }
    Vector3 GetLandingPointBelowDragon()
    {
        Vector3 fallbackPoint = new Vector3(
            transform.position.x,
            GetZoneCenter().y + landingGroundOffset,
            transform.position.z
        );

        Vector3 rayStart = transform.position + Vector3.up * landingRayStartHeight;
        float totalRayDistance = landingRayStartHeight + landingRayDistance;

        RaycastHit[] hits = Physics.RaycastAll(
            rayStart,
            Vector3.down,
            totalRayDistance,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        if (hits == null || hits.Length == 0)
            return fallbackPoint;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            // Skip dragon's own colliders.
            if (hit.collider.transform.root == transform.root)
                continue;

            bool hasGroundTag = hit.collider.CompareTag("Ground");

            bool isInGroundLayer =
                groundLayer.value != 0 &&
                (groundLayer.value & (1 << hit.collider.gameObject.layer)) != 0;

            if (hasGroundTag || isInGroundLayer)
            {
                return hit.point + Vector3.up * landingGroundOffset;
            }
        }

        return fallbackPoint;
    }
    void StartFallingFromHit()
    {
        if (isDead) return;
        if (!isFlying) return;
        if (isFalling) return;

        hasAlreadyFallenFromSky = true;
        fallStartedTime = Time.time;

        isFalling = true;
        isLanding = false;
        isTakingOff = false;
        isRecoveringFromFall = false;
        isFlying = true;

        wantsFlyMove = false;
        wantsGroundMove = false;
        wantsHover = false;

        animWalking = false;
        animRunning = false;
        animGliding = false;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        SetBoolIfExists(isFlyingBoolName, true);
        SetBoolIfExists(isFallingBoolName, true);
        SetBoolIfExists(isDeadBoolName, false);

        ResetAllDragonTriggers();

        SetTriggerIfExists(fallFromHitTriggerName);

        if (animator != null && !string.IsNullOrEmpty(skyDeathStartStateName))
        {
            animator.CrossFadeInFixedTime(skyDeathStartStateName, 0.05f);
        }
    }

    void HandleFallingMovement()
    {
        SetBoolIfExists(isFlyingBoolName, true);
        SetBoolIfExists(isFallingBoolName, true);
        SetBoolIfExists(isDeadBoolName, false);

        // Give FlyGetHitToFalling and Falling time to play before checking ground.
        if (Time.time < fallStartedTime + minimumFallAnimationTime)
        {
            return;
        }

        if (IsDragonCloseToGround(fallingGroundRayDistance))
        {
            StartFallRecoveryLanding();
        }
    }

    void StartFallRecoveryLanding()
    {
        if (isDead) return;
        if (isSkyDeath) return;

        isFalling = false;
        isLanding = true;
        isFlying = true;
        isTakingOff = false;
        isRecoveringFromFall = false;

        landingAnimationStarted = true;
        landingAnimationStartedTime = Time.time;
        dragonReachedLandingPoint = false;

        currentLandingPoint = GetLandingPointBelowDragon();

        wantsFlyMove = false;
        wantsGroundMove = false;
        wantsHover = false;

        animWalking = false;
        animRunning = false;
        animGliding = false;

        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        SetBoolIfExists(isFlyingBoolName, true);
        SetBoolIfExists(isFallingBoolName, false);
        SetBoolIfExists(isDeadBoolName, false);

        ResetAllDragonTriggers();

        // This trigger is useful if your Animator transition uses Land.
        SetTriggerIfExists(landTriggerName);

        // This forces the visual state:
        // Falling -> FlyStationaryToLanding
        if (animator != null && !string.IsNullOrEmpty(fallRecoveryLandingStateName))
        {
            animator.CrossFadeInFixedTime(fallRecoveryLandingStateName, 0.08f);
        }
    }
    bool IsDragonCloseToGround(float rayDistance)
    {
        float checkHeight = Mathf.Max(0.5f, fallGroundCheckHeight);
        float checkDistance = rayDistance + checkHeight;

        Vector3 rayStart = transform.position + Vector3.up * checkHeight;

        RaycastHit[] hits = Physics.RaycastAll(
            rayStart,
            Vector3.down,
            checkDistance,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        if (hits == null || hits.Length == 0)
            return false;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            // Skip dragon's own colliders
            if (hit.collider.transform.root == transform.root)
                continue;

            // Check ground by tag
            if (hit.collider.CompareTag("Ground"))
            {
                return true;
            }
        }

        return false;
    }

    void HandleFallRecovery()
    {
        animWalking = false;
        animRunning = false;
        animGliding = false;

        FaceTarget(playerTarget.position);

        if (Time.time < fallRecoverEndTime) return;

        isRecoveringFromFall = false;

        if (hasDetectedPlayer && canUseSkyCombat && Random.value <= chanceToFlyAgainAfterFall)
        {
            StartTakeOff();
        }
        else
        {
            SetNextCombatModeChangeTime();
        }
    }

    void DamagePlayerByBite()
    {
        if (playerTarget == null) return;

        if (playerHealth == null)
            playerHealth = playerTarget.GetComponent<PlayerHealth>();

        if (playerHealth == null)
        {
            Debug.LogWarning("PlayerHealth script missing on Player.");
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        if (distanceToPlayer <= groundBiteRange)
        {
            playerHealth.TakeDamage(biteDamage);
            Debug.Log(gameObject.name + " bite damaged player: " + biteDamage);
        }
    }

    public void ShootFireballFromAnimation()
    {
        if (projectileShotDuringThisAttack)
            return;

        if (Time.time < nextAllowedProjectileShootTime)
            return;

        projectileShotDuringThisAttack = true;
        nextAllowedProjectileShootTime = Time.time + projectileShootLockTime;

        PlayFireballShootAudio();
        ShootProjectile(fireballPrefab);
    }
    void BeginNewProjectileAttack()
    {
        projectileShotDuringThisAttack = false;
    }
    void PlayFireballShootAudio()
    {
        if (fireballAudioSource == null) return;

        if (!IsPlayerInsideDragonAudioArea())
            return;

        fireballAudioSource.loop = false;
        fireballAudioSource.mute = false;

        if (fireballShootClip != null)
        {
            fireballAudioSource.PlayOneShot(fireballShootClip);
        }
        else if (fireballAudioSource.clip != null)
        {
            fireballAudioSource.Stop();
            fireballAudioSource.Play();
        }
    }
    public void StartSpreadAcidBreathFromAnimation()
    {
        PlaySpreadAcidBreathAudio();

        if (spreadAcidRoutine != null)
        {
            StopCoroutine(spreadAcidRoutine);
        }

        spreadAcidRoutine = StartCoroutine(SpreadAcidBreathRoutine());
    }
    public void StopSpreadAcidBreathFromAnimation()
    {
        if (spreadAcidRoutine != null)
        {
            StopCoroutine(spreadAcidRoutine);
            spreadAcidRoutine = null;
        }

        if (spreadAcidParticle != null)
        {
            spreadAcidParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (acidBreathAudioSource != null)
        {
            acidBreathAudioSource.Stop();
        }
    }
    void SetupDragonAudio()
    {
        bool playerCanHearDragon = IsPlayerInsideDragonAudioArea();

        if (acidBreathAudioSource != null)
        {
            acidBreathAudioSource.playOnAwake = false;
            acidBreathAudioSource.loop = false;
            acidBreathAudioSource.mute = !playerCanHearDragon;
        }

        if (fireballAudioSource != null)
        {
            fireballAudioSource.playOnAwake = false;
            fireballAudioSource.loop = false;
            fireballAudioSource.mute = !playerCanHearDragon;
        }

        if (wingAudioSource == null)
        {
            wingAudioSource = GetComponent<AudioSource>();
        }

        if (wingAudioSource == null)
        {
            wingAudioSource = gameObject.AddComponent<AudioSource>();
        }

        if (wingAudioSource != null)
        {
            wingAudioSource.playOnAwake = false;
            wingAudioSource.loop = false;
            wingAudioSource.spatialBlend = dragonWingSpatialBlend;
            wingAudioSource.mute = !playerCanHearDragon;
            wingAudioSource.Stop();
        }

        if (footstepAudioSource == null)
        {
            footstepAudioSource = GetComponent<AudioSource>();
        }

        if (footstepAudioSource == null)
        {
            footstepAudioSource = gameObject.AddComponent<AudioSource>();
        }

        if (footstepAudioSource != null)
        {
            footstepAudioSource.playOnAwake = false;
            footstepAudioSource.loop = false;
            footstepAudioSource.spatialBlend = dragonFootstepSpatialBlend;
            footstepAudioSource.mute = !playerCanHearDragon;
            footstepAudioSource.Stop();
        }

        if (hitAudioSource != null)
        {
            hitAudioSource.playOnAwake = false;
            hitAudioSource.loop = false;
            hitAudioSource.mute = !playerCanHearDragon;
        }
    }
    public void PlayDragonWingSound()
    {
        if (Time.time < nextAllowedDragonWingSoundTime) return;

        if (!CanPlayDragonWingSound()) return;

        if (wingAudioSource == null)
        {
            wingAudioSource = GetComponent<AudioSource>();
        }

        if (wingAudioSource == null)
        {
            wingAudioSource = gameObject.AddComponent<AudioSource>();
            wingAudioSource.playOnAwake = false;
            wingAudioSource.loop = false;
            wingAudioSource.spatialBlend = dragonWingSpatialBlend;
        }

        AudioClip selectedClip = GetRandomDragonWingClip();

        if (selectedClip == null) return;

        bool isTakeOffSound = isTakingOff;

        float volume = isTakeOffSound ? dragonWingTakeOffVolume : dragonWingFlyVolume;
        Vector2 pitchRange = isTakeOffSound ? dragonWingTakeOffPitchRange : dragonWingFlyPitchRange;

        wingAudioSource.mute = !IsPlayerInsideDragonAudioArea();
        wingAudioSource.loop = false;
        wingAudioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);

        wingAudioSource.PlayOneShot(selectedClip, volume);

        nextAllowedDragonWingSoundTime = Time.time + dragonMinWingSoundInterval;
    }

    bool CanPlayDragonWingSound()
    {
        if (isDead) return false;
        if (isSkyDeath) return false;
        if (isFalling) return false;
        if (isLanding) return false;
        if (isRecoveringFromFall) return false;

        if (!isFlying && !isTakingOff) return false;

        if (!IsPlayerInsideDragonAudioArea()) return false;

        return true;
    }

    AudioClip GetRandomDragonWingClip()
    {
        if (dragonWingFlyingClips == null || dragonWingFlyingClips.Length == 0)
        {
            return null;
        }

        if (dragonWingFlyingClips.Length == 1)
        {
            return dragonWingFlyingClips[0];
        }

        AudioClip selectedClip = null;

        for (int i = 0; i < 10; i++)
        {
            selectedClip = dragonWingFlyingClips[Random.Range(0, dragonWingFlyingClips.Length)];

            if (selectedClip != null && selectedClip != lastDragonWingClip)
            {
                break;
            }
        }

        if (selectedClip == null)
        {
            selectedClip = dragonWingFlyingClips[0];
        }

        lastDragonWingClip = selectedClip;
        return selectedClip;
    }
    void PlayDragonHitAudio()
    {
        if (hitAudioSource == null) return;

        if (!IsPlayerInsideDragonAudioArea())
            return;

        hitAudioSource.loop = false;
        hitAudioSource.mute = false;

        if (dragonHitClip != null)
        {
            hitAudioSource.PlayOneShot(dragonHitClip);
        }
        else if (hitAudioSource.clip != null)
        {
            hitAudioSource.Stop();
            hitAudioSource.Play();
        }
    }
    void PlaySpreadAcidBreathAudio()
    {
        if (acidBreathAudioSource == null) return;

        acidBreathAudioSource.loop = false;
        acidBreathAudioSource.mute = !IsPlayerInsideDragonAudioArea();

        if (spreadAcidBreathClip != null)
        {
            acidBreathAudioSource.PlayOneShot(spreadAcidBreathClip);
        }
        else if (acidBreathAudioSource.clip != null)
        {
            acidBreathAudioSource.Stop();
            acidBreathAudioSource.Play();
        }
    }
    bool IsPlayerInsideDragonAudioArea()
    {
        if (!onlyPlayAudioInsideDragonArea)
            return true;

        if (playerTarget == null)
            return false;

        float audioRange = customAudioAreaRange > 0f ? customAudioAreaRange : detectionRange;

        float distanceFromDragonToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        return distanceFromDragonToPlayer <= audioRange;
    }
    void UpdateWingAudio()
    {
        if (wingAudioSource == null) return;

        bool playerCanHearDragon = IsPlayerInsideDragonAudioArea();
        wingAudioSource.mute = !playerCanHearDragon;

        bool dragonCannotPlayWingAudio =
            isDead ||
            isSkyDeath ||
            isFalling ||
            isLanding ||
            isRecoveringFromFall ||
            (!isFlying && !isTakingOff);

        if (dragonCannotPlayWingAudio)
        {
            if (wingAudioSource.isPlaying)
            {
                wingAudioSource.Stop();
            }
        }
    }
    void UpdateFootstepAudio()
    {
        if (footstepAudioSource == null) return;

        bool playerCanHearDragon = IsPlayerInsideDragonAudioArea();
        footstepAudioSource.mute = !playerCanHearDragon;

        bool dragonCannotPlayFootsteps =
            isDead ||
            isSkyDeath ||
            isFlying ||
            isTakingOff ||
            isLanding ||
            isFalling ||
            isRecoveringFromFall ||
            isGroundHitStunned ||
            (!animWalking && !animRunning);

        if (dragonCannotPlayFootsteps)
        {
            if (footstepAudioSource.isPlaying)
            {
                footstepAudioSource.Stop();
            }
        }
    }
    public void PlayDragonFootstepSound()
    {
        if (Time.time < nextAllowedDragonFootstepTime) return;

        if (!CanPlayDragonFootstepSound()) return;

        if (footstepAudioSource == null)
        {
            footstepAudioSource = GetComponent<AudioSource>();
        }

        if (footstepAudioSource == null)
        {
            footstepAudioSource = gameObject.AddComponent<AudioSource>();
            footstepAudioSource.playOnAwake = false;
            footstepAudioSource.loop = false;
            footstepAudioSource.spatialBlend = dragonFootstepSpatialBlend;
        }

        AudioClip selectedClip = GetRandomDragonGrassFootstepClip();

        if (selectedClip == null) return;

        bool isRunningStep = animRunning;

        float volume = isRunningStep ? dragonRunFootstepVolume : dragonWalkFootstepVolume;
        Vector2 pitchRange = isRunningStep ? dragonRunPitchRange : dragonWalkPitchRange;

        footstepAudioSource.mute = !IsPlayerInsideDragonAudioArea();
        footstepAudioSource.loop = false;
        footstepAudioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);

        footstepAudioSource.PlayOneShot(selectedClip, volume);

        nextAllowedDragonFootstepTime = Time.time + dragonMinFootstepInterval;
    }

    bool CanPlayDragonFootstepSound()
    {
        if (isDead) return false;
        if (isSkyDeath) return false;
        if (isFlying) return false;
        if (isTakingOff) return false;
        if (isLanding) return false;
        if (isFalling) return false;
        if (isRecoveringFromFall) return false;
        if (isGroundHitStunned) return false;

        if (!animWalking && !animRunning) return false;

        if (!IsPlayerInsideDragonAudioArea()) return false;

        return true;
    }

    AudioClip GetRandomDragonGrassFootstepClip()
    {
        if (dragonGrassFootstepClips == null || dragonGrassFootstepClips.Length == 0)
        {
            return null;
        }

        if (dragonGrassFootstepClips.Length == 1)
        {
            return dragonGrassFootstepClips[0];
        }

        AudioClip selectedClip = null;

        for (int i = 0; i < 10; i++)
        {
            selectedClip = dragonGrassFootstepClips[Random.Range(0, dragonGrassFootstepClips.Length)];

            if (selectedClip != null && selectedClip != lastDragonFootstepClip)
            {
                break;
            }
        }

        if (selectedClip == null)
        {
            selectedClip = dragonGrassFootstepClips[0];
        }

        lastDragonFootstepClip = selectedClip;
        return selectedClip;
    }
    void UpdateDragonAudioAreaMute()
    {
        bool playerCanHearDragon = IsPlayerInsideDragonAudioArea();

        if (wingAudioSource != null)
            wingAudioSource.mute = !playerCanHearDragon;

        if (acidBreathAudioSource != null)
            acidBreathAudioSource.mute = !playerCanHearDragon;

        if (fireballAudioSource != null)
            fireballAudioSource.mute = !playerCanHearDragon;

        if (footstepAudioSource != null)
            footstepAudioSource.mute = !playerCanHearDragon;

        if (hitAudioSource != null)
            hitAudioSource.mute = !playerCanHearDragon;
    }
    void StopAllDragonAudio()
    {
        if (wingAudioSource != null)
        {
            wingAudioSource.mute = false;
            wingAudioSource.Stop();
        }

        if (acidBreathAudioSource != null)
        {
            acidBreathAudioSource.mute = false;
            acidBreathAudioSource.Stop();
        }

        if (fireballAudioSource != null)
        {
            fireballAudioSource.mute = false;
            fireballAudioSource.Stop();
        }

        if (footstepAudioSource != null)
        {
            footstepAudioSource.mute = false;
            footstepAudioSource.Stop();
        }
    }
    float GetSpreadAcidFirstDamageDelay(Transform acidOrigin)
    {
        if (!autoCalculateSpreadAcidHitDelay)
            return manualSpreadAcidFirstDamageDelay;

        if (acidOrigin == null)
            return manualSpreadAcidFirstDamageDelay;

        if (playerTarget == null)
            return manualSpreadAcidFirstDamageDelay;

        if (spreadAcidParticleVisualSpeed <= 0.1f)
            return manualSpreadAcidFirstDamageDelay;

        Vector3 targetPoint = playerTarget.position + Vector3.up * 1.2f;
        float distanceToPlayer = Vector3.Distance(acidOrigin.position, targetPoint);

        float calculatedDelay = distanceToPlayer / spreadAcidParticleVisualSpeed;

        calculatedDelay = Mathf.Clamp(
            calculatedDelay,
            minSpreadAcidFirstDamageDelay,
            maxSpreadAcidFirstDamageDelay
        );

        return calculatedDelay;
    }

    IEnumerator SpreadAcidBreathRoutine()
    {
        Transform acidOrigin = spreadAcidPoint;

        if (acidOrigin == null)
            acidOrigin = firePoint;

        if (acidOrigin == null)
            acidOrigin = transform;

        AimMouthPointAtPlayer(acidOrigin, true);
        UpdateSpreadAcidParticleVisual(acidOrigin, true);

        if (spreadAcidParticle != null)
        {
            spreadAcidParticle.Clear(true);
            spreadAcidParticle.Play(true);
        }

        float endTime = Time.time + spreadAcidBreathDuration;

        float firstDamageDelay = GetSpreadAcidFirstDamageDelay(acidOrigin);

        // This is the important fix.
        // Damage will not start instantly anymore.
        nextSpreadAcidDamageTime = Time.time + firstDamageDelay;

        Debug.Log("Spread Acid first damage delay: " + firstDamageDelay);

        while (Time.time < endTime)
        {
            if (playerTarget != null)
            {
                FaceTarget(playerTarget.position);
                AimMouthPointAtPlayer(acidOrigin, false);
                UpdateSpreadAcidParticleVisual(acidOrigin, false);
            }

            if (Time.time >= nextSpreadAcidDamageTime)
            {
                DamagePlayerWithSpreadAcid();
                nextSpreadAcidDamageTime = Time.time + spreadAcidDamageInterval;
            }

            yield return null;
        }

        if (spreadAcidParticle != null)
        {
            spreadAcidParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        spreadAcidRoutine = null;
    }
    void AimMouthPointAtPlayer(Transform mouthPoint, bool instant)
    {
        if (!aimMouthAtPlayer) return;
        if (mouthPoint == null) return;
        if (playerTarget == null) return;

        Vector3 targetPoint = playerTarget.position + mouthTargetOffset;
        Vector3 direction = targetPoint - mouthPoint.position;

        if (direction == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);

        if (instant)
        {
            mouthPoint.rotation = targetRotation;
        }
        else
        {
            mouthPoint.rotation = Quaternion.Slerp(
                mouthPoint.rotation,
                targetRotation,
                Time.deltaTime * mouthAimSpeed
            );
        }
    }
    void DamagePlayerWithSpreadAcid()
    {
        if (playerTarget == null) return;

        Transform acidOrigin = spreadAcidPoint;

        if (acidOrigin == null)
            acidOrigin = firePoint;

        if (acidOrigin == null)
            acidOrigin = transform;

        Vector3 targetPoint = playerTarget.position + Vector3.up * 1.2f;
        Vector3 directionToPlayer = targetPoint - acidOrigin.position;

        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer > spreadAcidDamageRange)
        {
            Debug.Log("Spread Acid missed: Player is too far.");
            return;
        }

        Vector3 directionToPlayerNormalized = directionToPlayer.normalized;

        Vector3 breathForward = transform.forward;

        if (!isFlying)
        {
            breathForward.y = 0f;

            Vector3 flatDirectionToPlayer = directionToPlayerNormalized;
            flatDirectionToPlayer.y = 0f;

            if (breathForward.sqrMagnitude > 0.01f && flatDirectionToPlayer.sqrMagnitude > 0.01f)
            {
                breathForward.Normalize();
                flatDirectionToPlayer.Normalize();

                float flatAngle = Vector3.Angle(breathForward, flatDirectionToPlayer);

                if (flatAngle > spreadAcidAngle * 0.5f)
                {
                    Debug.Log("Spread Acid missed: Player is outside acid angle. Angle = " + flatAngle);
                    return;
                }
            }
        }
        else
        {
            float angleToPlayer = Vector3.Angle(breathForward, directionToPlayerNormalized);

            if (angleToPlayer > spreadAcidAngle * 0.5f)
            {
                Debug.Log("Spread Acid missed: Player is outside acid angle. Angle = " + angleToPlayer);
                return;
            }
        }

        if (spreadAcidBlockLayers.value != 0)
        {
            if (Physics.Raycast(
                acidOrigin.position,
                directionToPlayerNormalized,
                out RaycastHit hit,
                distanceToPlayer,
                spreadAcidBlockLayers,
                QueryTriggerInteraction.Ignore))
            {
                Debug.Log("Spread Acid blocked by: " + hit.collider.name);
                return;
            }
        }

        if (playerHealth == null)
            playerHealth = playerTarget.GetComponent<PlayerHealth>();

        if (playerHealth == null)
        {
            Debug.LogWarning("PlayerHealth script missing on Player.");
            return;
        }

        playerHealth.TakeDamageWithEffect(
            spreadAcidDamage,
            breathEffectType,
            breathScreenEffectDuration,
            breathSlowsPlayer,
            breathSlowMultiplier,
            breathSlowDuration
        );

        SpawnSpreadAcidHitEffect();

        Debug.Log(gameObject.name + " spread acid damaged player: " + spreadAcidDamage);
    }
    void UpdateSpreadAcidParticleVisual(Transform acidOrigin, bool instant)
    {
        if (spreadAcidParticle == null) return;
        if (acidOrigin == null) return;
        if (playerTarget == null) return;

        Transform particleTransform = spreadAcidParticle.transform;

        particleTransform.position = acidOrigin.position;

        Vector3 targetPoint = playerTarget.position + Vector3.up * 1.2f;
        Vector3 direction = targetPoint - particleTransform.position;

        if (direction == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);

        if (instant)
        {
            particleTransform.rotation = targetRotation;
        }
        else
        {
            particleTransform.rotation = Quaternion.Slerp(
                particleTransform.rotation,
                targetRotation,
                Time.deltaTime * mouthAimSpeed
            );
        }
    }
    void SpawnSpreadAcidHitEffect()
    {
        if (spreadAcidHitEffectPrefab == null) return;
        if (playerTarget == null) return;

        FindPlayerAcidHitPointIfMissing();

        Vector3 spawnPosition;

        if (playerAcidHitPoint != null)
            spawnPosition = playerAcidHitPoint.position;
        else
            spawnPosition = playerTarget.position + Vector3.up * 1.2f;

        Quaternion spawnRotation = Quaternion.LookRotation(transform.forward);

        GameObject hitEffect = Instantiate(
            spreadAcidHitEffectPrefab,
            spawnPosition,
            spawnRotation
        );

        hitEffect.transform.localScale = spreadAcidHitEffectScale;

        if (playerAcidHitPoint != null)
            hitEffect.transform.SetParent(playerAcidHitPoint);
        else
            hitEffect.transform.SetParent(playerTarget);

        Destroy(hitEffect, spreadAcidHitEffectDestroyTime);
    }
    public void ShootAcidFromAnimation()
    {
        if (projectileShotDuringThisAttack)
            return;

        if (Time.time < nextAllowedProjectileShootTime)
            return;

        projectileShotDuringThisAttack = true;
        nextAllowedProjectileShootTime = Time.time + projectileShootLockTime;

        ShootProjectile(acidBallPrefab);
    }
    void TrySkyAttack()
    {
        if (Time.time < nextAttackTime) return;

        bool useSpreadBreath = Random.value > 0.5f;

        if (useSpreadBreath)
        {
            SetTriggerIfExists(spreadAcidBreathTriggerName);

            if (!useAnimationEventsForProjectiles)
            {
                StartSpreadAcidBreathFromAnimation();
            }
        }
        else
        {
            BeginNewProjectileAttack();

            SetTriggerIfExists(spitAcidTriggerName);

            if (!useAnimationEventsForProjectiles)
            {
                ShootFireballFromAnimation();
            }
        }

        nextAttackTime = Time.time + attackCooldown;
    }

    void ShootProjectile(GameObject projectilePrefab)
    {
        if (projectilePrefab == null) return;
        if (firePoint == null) return;
        if (playerTarget == null) return;

        Vector3 targetPoint = playerTarget.position + Vector3.up * 1.2f;
        Vector3 direction = targetPoint - firePoint.position;

        if (direction == Vector3.zero) return;

        direction.Normalize();

        Quaternion spawnRotation = Quaternion.LookRotation(direction);

        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, spawnRotation);

        ProjectileDamage projectileDamage = projectile.GetComponent<ProjectileDamage>();

        if (projectileDamage == null)
        {
            projectileDamage = projectile.GetComponentInChildren<ProjectileDamage>();
        }



        Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();

        if (projectileRb != null)
        {
            projectileRb.linearVelocity = direction * projectileSpeed;
        }
    }

    void FlyToward(Vector3 targetPosition, float moveSpeed)
    {
        if (rb == null) return;

        FaceTarget(targetPosition);

        Vector3 direction = targetPosition - transform.position;

        if (direction.magnitude < 0.2f)
            return;

        Vector3 desiredVelocity = direction.normalized * moveSpeed;
        Vector3 steeringForce = (desiredVelocity - rb.linearVelocity) * flightAcceleration;

        rb.AddForce(steeringForce, ForceMode.Acceleration);

        if (rb.linearVelocity.magnitude > maxFlightSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxFlightSpeed;
        }
    }

    void HoverInPlace()
    {
        if (rb == null) return;

        rb.AddForce(-rb.linearVelocity * hoverBrakeForce, ForceMode.Acceleration);
    }

    void MoveOnGround(Vector3 targetPosition, float moveSpeed)
    {
        if (rb == null) return;

        Vector3 flatTarget = new Vector3(targetPosition.x, transform.position.y, targetPosition.z);

        FaceTarget(flatTarget);

        Vector3 newPosition = Vector3.MoveTowards(
            rb.position,
            flatTarget,
            moveSpeed * Time.fixedDeltaTime
        );

        rb.MovePosition(newPosition);
    }

    void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;

        if (!isFlying)
            direction.y = 0f;
        else
            direction.y *= 0.4f;

        if (direction == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        Quaternion smoothRotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime
        );

        if (rb != null)
            rb.MoveRotation(smoothRotation);
        else
            transform.rotation = smoothRotation;
    }

    void ChooseNewSkyPatrolPoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle * skyPatrolRadius;
        float randomHeight = Random.Range(minFlyHeight, maxFlyHeight);

        Vector3 center = GetZoneCenter();

        currentSkyPatrolPoint = new Vector3(
            center.x + randomCircle.x,
            center.y + randomHeight,
            center.z + randomCircle.y
        );
    }

    void ChooseNewGroundPatrolPoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle * groundPatrolRadius;
        Vector3 center = GetZoneCenter();

        currentGroundPatrolPoint = new Vector3(
            center.x + randomCircle.x,
            center.y,
            center.z + randomCircle.y
        );
    }

    void ResetMovementRequests()
    {
        wantsFlyMove = false;
        wantsGroundMove = false;
        wantsHover = false;

        animWalking = false;
        animRunning = false;
        animGliding = false;
    }

    Vector3 GetZoneCenter()
    {
        if (zoneCenter != null)
            return zoneCenter.position;

        return zoneCenterPosition;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        if (other.CompareTag("Arrow"))
        {
            Vector3 hitPoint = other.ClosestPoint(transform.position);

            if (hitPoint == transform.position)
            {
                hitPoint = other.transform.position;
            }

            Vector3 hitDirection = other.transform.forward;

            if (other.attachedRigidbody != null && other.attachedRigidbody.linearVelocity.sqrMagnitude > 0.1f)
            {
                hitDirection = other.attachedRigidbody.linearVelocity.normalized;
            }

            Vector3 hitNormal = -hitDirection;

            ArrowHit(other.gameObject, hitPoint, hitNormal);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;

        if (collision.collider.CompareTag("Arrow"))
        {
            Vector3 hitPoint = transform.position;
            Vector3 hitNormal = -collision.collider.transform.forward;

            if (collision.contactCount > 0)
            {
                hitPoint = collision.contacts[0].point;
                hitNormal = collision.contacts[0].normal;
            }

            ArrowHit(collision.gameObject, hitPoint, hitNormal);
        }
    }

    void ArrowHit(GameObject arrow, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (isDead) return;

        PlayDragonHitAudio();

        int damageAmount = GetArrowDamage(arrow);

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        arrowHits++;

        ShowDragonHealthBar();
        UpdateDragonHealthBar();

        Debug.Log(gameObject.name + " took arrow damage: " + damageAmount);
        Debug.Log("Dragon HP: " + currentHealth + " / " + maxHealth);

        Destroy(arrow);

        hasDetectedPlayer = true;

        if (currentHealth <= 0)
        {
            HideDragonHealthBar();

            if (isFlying || isFalling || isTakingOff || isLanding)
                StartSkyDeath();
            else
                StartGroundDeath();

            return;
        }

        if (isFlying || isFalling)
        {
            if (fallAfterHits > 0 && arrowHits >= fallAfterHits && !hasAlreadyFallenFromSky)
            {
                StartFallingFromHit();
            }
            else
            {
                SetTriggerIfExists(flyGetHitTriggerName);
            }

            return;
        }

        StartGroundHitReaction();
    }
    void StartGroundHitReaction()
    {
        isGroundHitStunned = true;
        groundHitStunEndTime = Time.time + groundHitStunDuration;

        wantsGroundMove = false;
        wantsFlyMove = false;
        wantsHover = false;

        animWalking = false;
        animRunning = false;
        animGliding = false;

        if (rb != null)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }

        SetBoolIfExists(isWalkingBoolName, false);
        SetBoolIfExists(isRunningBoolName, false);
        SetBoolIfExists(isGlidingBoolName, false);

        ResetAllDragonTriggers();
        SetTriggerIfExists(getHit1TriggerName);

        if (animator != null && !string.IsNullOrEmpty(getHit1StateName))
        {
            animator.CrossFadeInFixedTime(getHit1StateName, 0.05f);
        }
    }

    void HandleGroundHitStun()
    {
        animWalking = false;
        animRunning = false;
        animGliding = false;

        SetBoolIfExists(isWalkingBoolName, false);
        SetBoolIfExists(isRunningBoolName, false);
        SetBoolIfExists(isGlidingBoolName, false);

        FaceTarget(playerTarget.position);

        if (Time.time >= groundHitStunEndTime)
        {
            isGroundHitStunned = false;
        }
    }

    void StartGroundDeath()
    {
        if (isDead) return;

        StopAllDragonAudio();

        isDead = true;
        isSkyDeath = false;
        skyDeathHitGround = false;

        hasDetectedPlayer = false;
        isTakingOff = false;
        isLanding = false;
        isFalling = false;
        isRecoveringFromFall = false;
        isGroundHitStunned = false;
        isFlying = false;

        wantsFlyMove = false;
        wantsGroundMove = false;
        wantsHover = false;

        animWalking = false;
        animRunning = false;
        animGliding = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = true;
            rb.isKinematic = true;
        }

        SetBoolIfExists(isWalkingBoolName, false);
        SetBoolIfExists(isRunningBoolName, false);
        SetBoolIfExists(isGlidingBoolName, false);
        SetBoolIfExists(isFallingBoolName, false);
        SetBoolIfExists(isFlyingBoolName, false);

        ResetAllDragonTriggers();

        SetBoolIfExists(isDeadBoolName, true);

        if (animator != null && !string.IsNullOrEmpty(groundDeathStateName))
        {
            animator.CrossFadeInFixedTime(groundDeathStateName, 0.05f);
        }

        Destroy(gameObject, destroyAfterDeath);
    }

    void StartSkyDeath()
    {
        if (isSkyDeath) return;

        StopAllDragonAudio();

        isDead = true;
        isSkyDeath = true;
        skyDeathHitGround = false;
        skyDeathStartedTime = Time.time;

        hasDetectedPlayer = false;
        isTakingOff = false;
        isLanding = false;
        isFalling = true;
        isRecoveringFromFall = false;
        isGroundHitStunned = false;
        isFlying = true;

        wantsFlyMove = false;
        wantsGroundMove = false;
        wantsHover = false;

        animWalking = false;
        animRunning = false;
        animGliding = false;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        SetBoolIfExists(isWalkingBoolName, false);
        SetBoolIfExists(isRunningBoolName, false);
        SetBoolIfExists(isGlidingBoolName, false);
        SetBoolIfExists(isFlyingBoolName, true);
        SetBoolIfExists(isFallingBoolName, true);

        // Keep IsDead false during sky death so ground Death does not interrupt.
        SetBoolIfExists(isDeadBoolName, false);

        ResetAllDragonTriggers();

        SetTriggerIfExists(fallFromHitTriggerName);

        if (animator != null && !string.IsNullOrEmpty(skyDeathStartStateName))
        {
            animator.CrossFadeInFixedTime(skyDeathStartStateName, 0.05f);
        }
    }

    void HandleSkyDeathFalling()
    {
        if (skyDeathHitGround) return;

        SetBoolIfExists(isFlyingBoolName, true);
        SetBoolIfExists(isFallingBoolName, true);

        // Keep false so Any State -> Death does not interrupt sky death.
        SetBoolIfExists(isDeadBoolName, false);

        // Give FlyGetHitToFalling and Falling time to play before checking ground.
        if (Time.time < skyDeathStartedTime + minimumFallAnimationTime)
        {
            return;
        }

        if (IsDragonCloseToGround(fallingGroundRayDistance))
        {
            skyDeathHitGround = true;

            isFlying = false;
            isFalling = false;

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.useGravity = true;
                rb.isKinematic = true;
            }

            SetBoolIfExists(isFlyingBoolName, false);
            SetBoolIfExists(isFallingBoolName, false);
            SetBoolIfExists(isDeadBoolName, false);

            ResetAllDragonTriggers();
            SetTriggerIfExists(deathHitGroundTriggerName);

            if (animator != null && !string.IsNullOrEmpty(deathHitGroundStateName))
            {
                animator.CrossFadeInFixedTime(deathHitGroundStateName, 0.05f);
            }

            Destroy(gameObject, destroyAfterDeath);
        }
    }
    void StopWingAudioNow()
    {
        wingAudioShouldStopTime = 0f;

        if (wingAudioSource != null && wingAudioSource.isPlaying)
        {
            wingAudioSource.Stop();
        }
    }
    void ResetAllDragonTriggers()
    {
        if (animator == null) return;

        ResetTriggerIfExists(takeOffTriggerName);
        ResetTriggerIfExists(landTriggerName);
        ResetTriggerIfExists(biteAttackTriggerName);
        ResetTriggerIfExists(spitAcidTriggerName);
        ResetTriggerIfExists(spreadAcidBreathTriggerName);
        ResetTriggerIfExists(getHit1TriggerName);
        ResetTriggerIfExists(flyGetHitTriggerName);
        ResetTriggerIfExists(fallFromHitTriggerName);
        ResetTriggerIfExists(deathHitGroundTriggerName);
    }

    void ResetTriggerIfExists(string parameterName)
    {
        if (animator == null) return;
        if (string.IsNullOrEmpty(parameterName)) return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Trigger)
            {
                animator.ResetTrigger(parameterName);
                return;
            }
        }
    }

    void UpdateAnimatorBools()
    {
        UpdateDragonAudioAreaMute();

        if (isSkyDeath)
        {
            SetBoolIfExists(isFlyingBoolName, true);
            SetBoolIfExists(isFallingBoolName, true);
            SetBoolIfExists(isDeadBoolName, false);

            UpdateWingAudio();
            UpdateFootstepAudio();
            return;
        }

        SetBoolIfExists(isFlyingBoolName, isFlying);
        SetBoolIfExists(isWalkingBoolName, animWalking);
        SetBoolIfExists(isRunningBoolName, animRunning);
        SetBoolIfExists(isGlidingBoolName, animGliding);
        SetBoolIfExists(isFallingBoolName, isFalling);
        SetBoolIfExists(isDeadBoolName, isDead);

        UpdateWingAudio();
        UpdateFootstepAudio();
    }

    bool SetBoolIfExists(string parameterName, bool value)
    {
        if (animator == null) return false;
        if (string.IsNullOrEmpty(parameterName)) return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(parameterName, value);
                return true;
            }
        }

        return false;
    }

    bool SetTriggerIfExists(string parameterName)
    {
        if (animator == null) return false;
        if (string.IsNullOrEmpty(parameterName)) return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Trigger)
            {
                animator.SetTrigger(parameterName);
                return true;
            }
        }

        return false;
    }
    void SetupDragonHealthBar()
    {
        if (healthBar == null && healthBarRoot != null)
        {
            healthBar = healthBarRoot.GetComponentInChildren<DragonHealthBar>(true);
        }

        if (healthBar == null)
        {
            healthBar = GetComponentInChildren<DragonHealthBar>(true);
        }

        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
            healthBar.SetHealth(currentHealth);
        }

        if (healthBarRoot != null && hideHealthBarUntilHit)
        {
            healthBarRoot.SetActive(false);
        }
    }

    void ShowDragonHealthBar()
    {
        if (healthBarRoot != null)
        {
            healthBarRoot.SetActive(true);
        }
    }

    void HideDragonHealthBar()
    {
        if (healthBarRoot != null)
        {
            healthBarRoot.SetActive(false);
        }
    }

    void UpdateDragonHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }
    }

    int GetArrowDamage(GameObject arrow)
    {
        if (arrow == null)
            return defaultArrowDamage;

        ArrowDamage arrowDamage = arrow.GetComponent<ArrowDamage>();

        if (arrowDamage == null)
            arrowDamage = arrow.GetComponentInParent<ArrowDamage>();

        if (arrowDamage == null)
            arrowDamage = arrow.GetComponentInChildren<ArrowDamage>();

        if (arrowDamage != null)
            return arrowDamage.damage;

        return defaultArrowDamage;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center;

        if (zoneCenter != null)
            center = zoneCenter.position;
        else
            center = Application.isPlaying ? zoneCenterPosition : transform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, detectionRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(center, skyPatrolRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(center, groundPatrolRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, skyAttackRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(center, losePlayerRange);
    }
}