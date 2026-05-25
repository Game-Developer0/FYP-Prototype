using UnityEngine;
using System.Collections;

public class DragonEnemy : MonoBehaviour
{
    [Header("Target")]
    public Transform playerTarget;

    [Header("Components")]
    public Rigidbody rb;
    public Animator animator;

    [Header("Zone")]
    public Transform zoneCenter;
    public float detectionRange = 45f;
    public float losePlayerRange = 75f;

    [Header("Start Setting")]
    public bool startInSky = false;

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

    [Tooltip("If true, add Animation Event on attack animation and call ShootFireballFromAnimation or ShootAcidFromAnimation.")]
    public bool useAnimationEventsForProjectiles = false;

    [Header("Health")]
    public int arrowHits = 0;
    public int hitsToDie = 8;
    public int fallAfterHits = 4;
    public float destroyAfterDeath = 7f;

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
    private bool animGliding = false;

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
    void Start()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (zoneCenter != null)
            zoneCenterPosition = zoneCenter.position;
        else
            zoneCenterPosition = transform.position;

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

        if (playerTarget == null)
        {
            Debug.LogWarning("Player Target missing. Drag Player into DragonEnemy script.");
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

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(Vector3.up * takeOffForce, ForceMode.VelocityChange);
        }

        SetTriggerIfExists(takeOffTriggerName);
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

        wantsFlyMove = false;
        wantsHover = false;

        SetTriggerIfExists(landTriggerName);
    }

    void HandleLandingMovement()
    {
        Vector3 landingPoint = new Vector3(transform.position.x, GetZoneCenter().y + 1f, transform.position.z);

        FlyToward(landingPoint, landingSpeed);

        if (Vector3.Distance(transform.position, landingPoint) <= 1.5f)
        {
            isLanding = false;
            isFlying = false;

            if (rb != null)
            {
                rb.useGravity = true;
                rb.linearVelocity = Vector3.zero;
            }

            ChooseNewGroundPatrolPoint();
            SetNextCombatModeChangeTime();
        }

        UpdateAnimatorBools();
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
            isFalling = false;
            isFlying = false;
            isRecoveringFromFall = true;
            fallRecoverEndTime = Time.time + recoverAfterFallTime;

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.useGravity = true;
            }

            animWalking = false;
            animRunning = false;
            animGliding = false;

            SetBoolIfExists(isFlyingBoolName, false);
            SetBoolIfExists(isFallingBoolName, false);
            SetBoolIfExists(isDeadBoolName, false);

            ChooseNewGroundPatrolPoint();

            if (animator != null && !string.IsNullOrEmpty(groundIdleStateName))
            {
                animator.CrossFadeInFixedTime(groundIdleStateName, 0.1f);
            }
        }
    }
    bool IsDragonCloseToGround(float rayDistance)
    {
        Vector3 rayStart = transform.position + Vector3.up * 1f;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayDistance, groundLayer, QueryTriggerInteraction.Ignore))
        {
            return true;
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
        ShootProjectile(fireballPrefab);
    }
    public void StartSpreadAcidBreathFromAnimation()
    {
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
    }

    IEnumerator SpreadAcidBreathRoutine()
    {
        Transform acidOrigin = spreadAcidPoint;

        if (acidOrigin == null)
            acidOrigin = firePoint;

        if (acidOrigin == null)
            acidOrigin = transform;

        AimMouthPointAtPlayer(acidOrigin, true);

        if (spreadAcidParticle != null)
        {
            spreadAcidParticle.Play();
        }

        float endTime = Time.time + spreadAcidBreathDuration;
        nextSpreadAcidDamageTime = 0f;

        while (Time.time < endTime)
        {
            if (playerTarget != null)
            {
                FaceTarget(playerTarget.position);
                AimMouthPointAtPlayer(acidOrigin, false);
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
            return;

        float angleToPlayer = Vector3.Angle(acidOrigin.forward, directionToPlayer.normalized);

        if (angleToPlayer > spreadAcidAngle * 0.5f)
            return;

        // Optional: stop acid damage if wall/ground is between dragon and player
        if (spreadAcidBlockLayers.value != 0)
        {
            if (Physics.Raycast(acidOrigin.position, directionToPlayer.normalized, out RaycastHit hit, distanceToPlayer, spreadAcidBlockLayers, QueryTriggerInteraction.Ignore))
            {
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

        playerHealth.TakeDamage(spreadAcidDamage);
        Debug.Log(gameObject.name + " spread acid hit player: " + spreadAcidDamage);
    }
    public void ShootAcidFromAnimation()
    {
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

        Vector3 targetPoint = playerTarget.position + mouthTargetOffset;
        Vector3 direction = targetPoint - firePoint.position;

        if (direction == Vector3.zero) return;

        direction.Normalize();

        Quaternion spawnRotation = Quaternion.LookRotation(direction);

        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, spawnRotation);

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
            ArrowHit(other.gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;

        if (collision.collider.CompareTag("Arrow"))
        {
            ArrowHit(collision.gameObject);
        }
    }

    void ArrowHit(GameObject arrow)
    {
        if (isDead) return;

        arrowHits++;

        Destroy(arrow);

        hasDetectedPlayer = true;

        if (arrowHits >= hitsToDie)
        {
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
        if (isSkyDeath)
        {
            SetBoolIfExists(isFlyingBoolName, true);
            SetBoolIfExists(isFallingBoolName, true);
            SetBoolIfExists(isDeadBoolName, false);
            return;
        }

        SetBoolIfExists(isFlyingBoolName, isFlying);
        SetBoolIfExists(isWalkingBoolName, animWalking);
        SetBoolIfExists(isRunningBoolName, animRunning);
        SetBoolIfExists(isGlidingBoolName, animGliding);
        SetBoolIfExists(isFallingBoolName, isFalling);
        SetBoolIfExists(isDeadBoolName, isDead);
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