using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{

    [Header("Target")]
    public Transform playerTarget;

    [Header("Components")]
    public NavMeshAgent agent;
    public Animator animator;

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
    public int arrowHits = 0;
    public int hitsToDie = 2;
    private bool isDead = false;
    public float destroyObjectTime = 4f;

    void Start()
    {
        startPosition = transform.position;

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponent<Animator>();

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

        if (isKnockedDown)
        {
            HandleKnockDown();
            return;
        }

        float distanceFromStartArea = Vector3.Distance(startPosition, playerTarget.position);
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        if (!hasDetectedPlayer && distanceFromStartArea <= detectionRange)
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
        isDetectStunning = false;

        ClearPassiveStates();
        StopMovementAnimations();

        if (arrowHits >= hitsToDie)
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
        isDead = true;

        ClearPassiveStates();
        StopMovementAnimations();

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

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

    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? startPosition : transform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, detectionRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(center, patrolRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, losePlayerRange);
    }
}