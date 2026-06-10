using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [Header("Target")]
    public Transform playerTarget;

    [Header("Components")]
    public NavMeshAgent agent;
    public Animator animator;

    [Header("Ecosystem")]
    public EcosystemAnimal ecosystemAnimal;

    [Header("Player Auto Detection")]
    public bool autoFindPlayerByTag = true;
    public string playerTag = "Player";

    [Header("Movement Speeds")]
    public float zoneWalkSpeed = 1.5f;
    public float chaseRunSpeed = 4f;

    [Header("Movement Animation Bools")]
    public string zoneWalkBoolName = "WalkForward";
    public string chaseRunBoolName = "Run Forward";

    [Header("Animal Area")]
    public float detectionRange = 15f;
    public float attackRange = 2f;
    public float losePlayerRange = 30f;
    public bool returnToStartPosition = true;

    private Vector3 startPosition;
    private bool hasDetectedPlayer = false;

    [Header("Passive Before Detection")]
    public bool canWalkInZone = true;
    public float patrolRadius = 8f;
    public float passiveStateMinTime = 4f;
    public float passiveStateMaxTime = 8f;

    public string[] passiveBoolNames = { "Sleep", "Sit", "Eat", "Idle" };

    private float nextPassiveChangeTime = 0f;
    private bool isWalkingInZone = false;

    [Header("Optional Detection Stun")]
    public bool useStunOnDetect = true;
    public string detectStunBoolName = "Stunned Loop";
    public float detectStunDuration = 1.5f;

    private bool isDetectStunning = false;
    private float detectStunEndTime = 0f;

    [Header("Player Damage")]
    public int attackDamage = 10;
    public float damageRange = 3f;
    private PlayerHealth playerHealth;

    [Header("Attack")]
    public float attackCooldown = 2f;
    private float nextAttackTime = 0f;

    public string[] attackTriggers = { "Attack1", "Attack2", "Attack3", "Attack4", "Attack5" };

    [Header("Hit Animation")]
    public string hitTriggerName = "Get Hit Front";

    [Header("Optional Knock Down")]
    public bool hasKnockDownAnimation = true;
    public string knockDownTriggerName = "Knock Down";
    public int knockDownAfterHits = 2;
    public float knockDownDuration = 2.5f;

    private bool knockDownAlreadyPlayed = false;
    private bool isKnockedDown = false;
    private float knockDownEndTime = 0f;

    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;
    public int defaultArrowDamage = 20;

    [Header("Mission Settings")]
    public bool countForBossWolfMission = false;
    public bool countForBearMission = false;

    private bool missionKillAlreadyCounted = false;

    [Header("Soft Despawn Return")]
    public bool allowSoftDespawnReturn = true;

    private bool isReturningHomeForDespawn = false;
    private Vector3 softDespawnHomePosition;
    private float softDespawnRepathTime = 0f;

    [Tooltip("Still used for knock down after some hits.")]
    public int arrowHits = 0;

    private bool isDead = false;
    public float destroyObjectTime = 4f;

    [Header("Animal Health Bar")]
    public AnimalHealthBar healthBar;
    public GameObject healthBarRoot;
    public bool hideHealthBarUntilHit = true;

    [Header("Blood Effects")]
    public GameObject[] hitBloodPrefabs;
    public GameObject[] deathBloodPrefabs;

    public float hitBloodDestroyTime = 3f;
    public float deathBloodDestroyTime = 5f;

    public Vector3 hitBloodScale = Vector3.one;
    public Vector3 deathBloodScale = Vector3.one;

    public int deathBloodAmount = 8;
    public float deathBloodSpawnRadius = 1.5f;

    public Transform[] deathBloodPoints;

    [Header("Blood Decal")]
    public GameObject groundBloodDecalPrefab;
    public float groundBloodDecalDestroyTime = 30f;
    public Vector3 groundBloodDecalScale = Vector3.one;
    public LayerMask bloodGroundLayer;
    public float bloodDecalRayDistance = 10f;

    void Start()
    {
        AssignReferencesIfMissing();

        startPosition = transform.position;

        currentHealth = maxHealth;
        arrowHits = 0;
        isDead = false;
        missionKillAlreadyCounted = false;
        hasDetectedPlayer = false;
        isDetectStunning = false;
        isKnockedDown = false;
        knockDownAlreadyPlayed = false;
        isWalkingInZone = false;

        SetupAnimalHealthBar();

        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        if (playerTarget != null)
            playerHealth = playerTarget.GetComponent<PlayerHealth>();

        if (agent != null)
        {
            agent.speed = zoneWalkSpeed;
            agent.stoppingDistance = attackRange;
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.isStopped = false;
        }

        ChoosePassiveState();
    }
    void AssignReferencesIfMissing()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (ecosystemAnimal == null)
            ecosystemAnimal = GetComponent<EcosystemAnimal>();

        if (playerTarget == null && autoFindPlayerByTag)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

            if (playerObject != null)
            {
                playerTarget = playerObject.transform;
            }
        }

        if (playerTarget != null && playerHealth == null)
        {
            playerHealth = playerTarget.GetComponent<PlayerHealth>();
        }
    }
    public void ResetAnimalForSpawn()
    {
        AssignReferencesIfMissing();

        startPosition = transform.position;

        currentHealth = maxHealth;
        arrowHits = 0;

        isDead = false;
        missionKillAlreadyCounted = false;

        MissionBossWolfTarget missionTarget = GetComponent<MissionBossWolfTarget>();

        if (missionTarget == null)
        {
            missionTarget = GetComponentInChildren<MissionBossWolfTarget>(true);
        }

        if (missionTarget != null)
        {
            missionTarget.ResetTargetForSpawn();
        }

        hasDetectedPlayer = false;
        isDetectStunning = false;
        isKnockedDown = false;
        knockDownAlreadyPlayed = false;
        isWalkingInZone = false;

        nextAttackTime = 0f;
        nextPassiveChangeTime = 0f;

        HideAnimalHealthBar();
        SetupAnimalHealthBar();

        ClearPassiveStates();
        StopMovementAnimations();

        SetBoolIfExists("Death", false);

        if (agent != null && agent.isOnNavMesh)
        {
            agent.enabled = true;
            agent.speed = zoneWalkSpeed;
            agent.stoppingDistance = attackRange;
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.isStopped = false;
            agent.ResetPath();
        }

        ChoosePassiveState();
    }
    void SetupAnimalHealthBar()
    {
        if (healthBar == null && healthBarRoot != null)
        {
            healthBar = healthBarRoot.GetComponentInChildren<AnimalHealthBar>(true);
        }

        if (healthBar == null)
        {
            healthBar = GetComponentInChildren<AnimalHealthBar>(true);
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

    void ShowAnimalHealthBar()
    {
        if (healthBarRoot != null)
        {
            healthBarRoot.SetActive(true);
        }
    }

    void HideAnimalHealthBar()
    {
        if (healthBarRoot != null)
        {
            healthBarRoot.SetActive(false);
        }
    }

    void UpdateAnimalHealthBar()
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

    void Update()
    {
        if (isDead) return;

        if (playerTarget == null)
        {
            Debug.LogWarning("Player Target missing. Drag Player into Enemy script.");
            return;
        }

        if (agent == null || !agent.isOnNavMesh)
        {
            Debug.LogWarning("Animal is not on NavMesh.");
            return;
        }

        if (isReturningHomeForDespawn)
        {
            HandleReturnHomeForDespawning();
            return;
        }

        if (isKnockedDown)
        {
            HandleKnockDown();
            return;
        }

        // Detection range now moves with the animal.
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        if (!hasDetectedPlayer && distanceToPlayer <= detectionRange)
        {
            hasDetectedPlayer = true;
            StartDetectionStun();
        }

        if (hasDetectedPlayer)
        {
            if (isDetectStunning)
            {
                HandleDetectionStun();
                return;
            }

            // Lose player range is unchanged.
            // It still checks distance between animal and player.
            if (distanceToPlayer > losePlayerRange)
            {
                hasDetectedPlayer = false;
                ClearPassiveStates();
                StopMovementAnimations();

                if (returnToStartPosition)
                {
                    ReturnHome();
                }
                else
                {
                    ChoosePassiveState();
                }

                return;
            }

            if (distanceToPlayer <= attackRange)
            {
                Attack();
            }
            else
            {
                Chase();
            }
        }
        else
        {
            // Patrol radius is unchanged.
            // It still uses startPosition as the animal home area.
            float distanceFromHome = Vector3.Distance(transform.position, startPosition);

            if (returnToStartPosition && distanceFromHome > patrolRadius + 2f)
            {
                ReturnHome();
            }
            else
            {
                PassiveBehaviour();
            }
        }
    }
    public void BeginReturnHomeForDespawning(Vector3 homePosition)
    {
        if (!allowSoftDespawnReturn)
            return;

        if (isDead)
            return;

        softDespawnHomePosition = homePosition;
        isReturningHomeForDespawn = true;
        softDespawnRepathTime = 0f;

        hasDetectedPlayer = false;
        isDetectStunning = false;
        isKnockedDown = false;
        isWalkingInZone = false;

        ClearPassiveStates();
        StopMovementAnimations();

        SetBoolIfExists(detectStunBoolName, false);

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = zoneWalkSpeed;
            agent.stoppingDistance = 0.5f;
            agent.ResetPath();
            agent.SetDestination(softDespawnHomePosition);
        }

        SetBoolIfExists(zoneWalkBoolName, true);
        SetBoolIfExists(chaseRunBoolName, false);
    }

    public void CancelReturnHomeForDespawning()
    {
        if (!isReturningHomeForDespawn)
            return;

        isReturningHomeForDespawn = false;

        ClearPassiveStates();
        StopMovementAnimations();

        ChoosePassiveState();
    }

    void HandleReturnHomeForDespawning()
    {
        if (isDead)
            return;

        ClearPassiveStates();

        if (agent != null && agent.isOnNavMesh)
        {
            agent.speed = zoneWalkSpeed;
            agent.isStopped = false;
            agent.stoppingDistance = 0.5f;

            if (Time.time >= softDespawnRepathTime)
            {
                agent.SetDestination(softDespawnHomePosition);
                softDespawnRepathTime = Time.time + 1f;
            }
        }

        SetBoolIfExists(zoneWalkBoolName, true);
        SetBoolIfExists(chaseRunBoolName, false);
    }
    void StartDetectionStun()
    {
        ClearPassiveStates();
        StopMovementAnimations();

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (useStunOnDetect && SetBoolIfExists(detectStunBoolName, true))
        {
            isDetectStunning = true;
            detectStunEndTime = Time.time + detectStunDuration;
        }
        else
        {
            isDetectStunning = false;
        }
    }

    void HandleDetectionStun()
    {
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        StopMovementAnimations();

        if (playerTarget != null)
        {
            Vector3 direction = playerTarget.position - transform.position;
            direction.y = 0f;

            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        if (Time.time >= detectStunEndTime)
        {
            SetBoolIfExists(detectStunBoolName, false);
            isDetectStunning = false;
        }
    }

    void PassiveBehaviour()
    {
        if (isDead) return;

        if (isWalkingInZone)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f)
            {
                isWalkingInZone = false;
                ChoosePassiveState();
            }

            return;
        }

        if (Time.time >= nextPassiveChangeTime)
        {
            if (canWalkInZone && Random.value < 0.35f)
            {
                WalkInZone();
            }
            else
            {
                ChoosePassiveState();
            }
        }
    }

    void ChoosePassiveState()
    {
        if (isDead) return;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        isWalkingInZone = false;

        ClearPassiveStates();
        StopMovementAnimations();

        if (passiveBoolNames != null && passiveBoolNames.Length > 0)
        {
            int randomIndex = Random.Range(0, passiveBoolNames.Length);
            string passiveState = passiveBoolNames[randomIndex];

            SetBoolIfExists(passiveState, true);

            Debug.Log(gameObject.name + " passive state: " + passiveState);
        }

        nextPassiveChangeTime = Time.time + Random.Range(passiveStateMinTime, passiveStateMaxTime);
    }

    void WalkInZone()
    {
        ClearPassiveStates();
        StopMovementAnimations();

        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += startPosition;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
        {
            agent.speed = zoneWalkSpeed;
            agent.isStopped = false;
            agent.stoppingDistance = 0.5f;
            agent.SetDestination(hit.position);

            SetBoolIfExists(zoneWalkBoolName, true);
            SetBoolIfExists(chaseRunBoolName, false);

            isWalkingInZone = true;

            Debug.Log(gameObject.name + " walking in zone");
        }
        else
        {
            ChoosePassiveState();
        }

        nextPassiveChangeTime = Time.time + Random.Range(passiveStateMinTime, passiveStateMaxTime);
    }

    void Chase()
    {
        if (isDead) return;

        ClearPassiveStates();

        agent.speed = chaseRunSpeed;
        agent.isStopped = false;
        agent.stoppingDistance = attackRange;
        agent.SetDestination(playerTarget.position);

        SetBoolIfExists(zoneWalkBoolName, false);
        SetBoolIfExists(chaseRunBoolName, true);
    }

    void Attack()
    {
        if (isDead) return;

        ClearPassiveStates();
        StopMovementAnimations();

        agent.isStopped = true;
        agent.ResetPath();

        Vector3 direction = playerTarget.position - transform.position;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        if (Time.time >= nextAttackTime)
        {
            PlayRandomAttack();
            DamagePlayer();

            nextAttackTime = Time.time + attackCooldown;
        }
    }

    void DamagePlayer()
    {
        if (playerTarget == null) return;

        if (playerHealth == null)
            playerHealth = playerTarget.GetComponent<PlayerHealth>();

        if (playerHealth == null)
        {
            Debug.LogWarning("PlayerHealth script missing on Player object.");
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        if (distanceToPlayer <= damageRange)
        {
            playerHealth.TakeDamage(attackDamage);
            Debug.Log(gameObject.name + " damaged player: " + attackDamage);
        }
    }

    void ReturnHome()
    {
        ClearPassiveStates();
        StopMovementAnimations();

        float distanceToHome = Vector3.Distance(transform.position, startPosition);

        if (distanceToHome > 1f)
        {
            agent.speed = zoneWalkSpeed;
            agent.isStopped = false;
            agent.stoppingDistance = 0.5f;
            agent.SetDestination(startPosition);

            SetBoolIfExists(zoneWalkBoolName, true);
            SetBoolIfExists(chaseRunBoolName, false);

            isWalkingInZone = false;
        }
        else
        {
            ChoosePassiveState();
        }
    }

    void PlayRandomAttack()
    {
        if (animator == null) return;
        if (attackTriggers == null || attackTriggers.Length == 0) return;

        int randomIndex = Random.Range(0, attackTriggers.Length);
        string randomAttack = attackTriggers[randomIndex];

        SetTriggerIfExists(randomAttack);
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

        int damageAmount = GetArrowDamage(arrow);

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        arrowHits++;

        ShowAnimalHealthBar();
        UpdateAnimalHealthBar();

        Debug.Log(gameObject.name + " took arrow damage: " + damageAmount);
        Debug.Log(gameObject.name + " HP: " + currentHealth + " / " + maxHealth);

        SpawnHitBlood(hitPoint, hitNormal);

        Destroy(arrow);

        hasDetectedPlayer = true;
        isDetectStunning = false;

        ClearPassiveStates();
        StopMovementAnimations();

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if (hasKnockDownAnimation && !knockDownAlreadyPlayed && arrowHits >= knockDownAfterHits)
        {
            StartKnockDown();
            return;
        }

        SetTriggerIfExists(hitTriggerName);
    }

    void SpawnHitBlood(Vector3 hitPoint, Vector3 hitNormal)
    {
        GameObject bloodPrefab = GetRandomPrefab(hitBloodPrefabs);

        if (bloodPrefab != null)
        {
            if (hitNormal == Vector3.zero)
            {
                hitNormal = -transform.forward;
            }

            Quaternion bloodRotation = Quaternion.LookRotation(hitNormal);

            GameObject blood = Instantiate(bloodPrefab, hitPoint, bloodRotation);
            blood.transform.localScale = hitBloodScale;

            Destroy(blood, hitBloodDestroyTime);
        }

        SpawnGroundBloodDecal(hitPoint);
    }

    void SpawnDeathBlood()
    {
        for (int i = 0; i < deathBloodAmount; i++)
        {
            GameObject bloodPrefab = GetRandomPrefab(deathBloodPrefabs);

            if (bloodPrefab == null)
            {
                bloodPrefab = GetRandomPrefab(hitBloodPrefabs);
            }

            if (bloodPrefab == null)
            {
                return;
            }

            Vector3 spawnPosition;
            Quaternion spawnRotation;

            if (deathBloodPoints != null && deathBloodPoints.Length > 0)
            {
                Transform randomPoint = deathBloodPoints[Random.Range(0, deathBloodPoints.Length)];

                if (randomPoint == null)
                {
                    continue;
                }

                spawnPosition = randomPoint.position;
                spawnRotation = randomPoint.rotation;
            }
            else
            {
                Vector3 randomOffset = Random.insideUnitSphere * deathBloodSpawnRadius;
                randomOffset.y = Mathf.Abs(randomOffset.y);

                spawnPosition = transform.position + randomOffset;
                spawnRotation = Random.rotation;
            }

            GameObject blood = Instantiate(bloodPrefab, spawnPosition, spawnRotation);
            blood.transform.localScale = deathBloodScale;

            Destroy(blood, deathBloodDestroyTime);

            SpawnGroundBloodDecal(spawnPosition);
        }
    }

    void SpawnGroundBloodDecal(Vector3 startPoint)
    {
        if (groundBloodDecalPrefab == null) return;

        Vector3 rayStart = startPoint + Vector3.up * 1f;

        bool hitGround;

        RaycastHit hit;

        if (bloodGroundLayer.value == 0)
        {
            hitGround = Physics.Raycast(rayStart, Vector3.down, out hit, bloodDecalRayDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }
        else
        {
            hitGround = Physics.Raycast(rayStart, Vector3.down, out hit, bloodDecalRayDistance, bloodGroundLayer, QueryTriggerInteraction.Ignore);
        }

        if (!hitGround) return;

        Quaternion decalRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
        decalRotation *= Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        Vector3 decalPosition = hit.point + hit.normal * 0.02f;

        GameObject decal = Instantiate(groundBloodDecalPrefab, decalPosition, decalRotation);
        decal.transform.localScale = groundBloodDecalScale;

        Destroy(decal, groundBloodDecalDestroyTime);
    }

    GameObject GetRandomPrefab(GameObject[] prefabs)
    {
        if (prefabs == null) return null;
        if (prefabs.Length == 0) return null;

        return prefabs[Random.Range(0, prefabs.Length)];
    }

    void StartKnockDown()
    {
        if (isDead) return;

        bool knockTriggerExists = SetTriggerIfExists(knockDownTriggerName);

        if (!knockTriggerExists)
        {
            Debug.LogWarning(gameObject.name + " does not have knock down trigger: " + knockDownTriggerName);
            SetTriggerIfExists(hitTriggerName);
            return;
        }

        knockDownAlreadyPlayed = true;
        isKnockedDown = true;
        knockDownEndTime = Time.time + knockDownDuration;

        isDetectStunning = false;

        ClearPassiveStates();
        StopMovementAnimations();

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        Debug.Log(gameObject.name + " knock down animation started.");
    }

    void HandleKnockDown()
    {
        if (isDead) return;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        StopMovementAnimations();

        if (Time.time >= knockDownEndTime)
        {
            isKnockedDown = false;
            hasDetectedPlayer = true;

            Debug.Log(gameObject.name + " recovered from knock down.");
        }
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;

        ReportKillToMission();
        NotifyBossWolfTargetKilled();

        if (ecosystemAnimal == null)
            ecosystemAnimal = GetComponent<EcosystemAnimal>();

        if (ecosystemAnimal != null)
        {
            ecosystemAnimal.ReportDeathToEcosystem();
        }

        HideAnimalHealthBar();

        ClearPassiveStates();
        StopMovementAnimations();

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        SpawnDeathBlood();

        SetBoolIfExists("Death", true);

        Destroy(gameObject, destroyObjectTime);
    }

    void ClearPassiveStates()
    {
        if (animator == null) return;

        SetBoolIfExists(detectStunBoolName, false);

        if (passiveBoolNames != null)
        {
            foreach (string stateName in passiveBoolNames)
            {
                SetBoolIfExists(stateName, false);
            }
        }
    }

    void StopMovementAnimations()
    {
        SetBoolIfExists(zoneWalkBoolName, false);
        SetBoolIfExists(chaseRunBoolName, false);
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
    void ReportKillToMission()
    {
        if (!countForBossWolfMission && !countForBearMission) return;
        if (missionKillAlreadyCounted) return;

        missionKillAlreadyCounted = true;

        MissionSequenceManager missionManager = FindObjectOfType<MissionSequenceManager>();

        if (missionManager == null)
        {
            Debug.LogWarning("Enemy: MissionSequenceManager not found in scene.");
            return;
        }

        if (countForBossWolfMission)
        {
            missionManager.RegisterBossWolfKill();
        }

        if (countForBearMission)
        {
            missionManager.RegisterBearKill();
        }
    }

    void NotifyBossWolfTargetKilled()
    {
        if (!countForBossWolfMission && !countForBearMission) return;

        MissionBossWolfTarget missionTarget = GetComponent<MissionBossWolfTarget>();

        if (missionTarget == null)
        {
            missionTarget = GetComponentInChildren<MissionBossWolfTarget>(true);
        }

        if (missionTarget != null)
        {
            missionTarget.NotifyKilled();
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Detection range moves with animal.
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Patrol radius stays at animal home/start area.
        Vector3 patrolCenter = Application.isPlaying ? startPosition : transform.position;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(patrolCenter, patrolRadius);

        // Attack range follows animal.
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Lose player range follows animal, same as your current logic.
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, losePlayerRange);
    }
}