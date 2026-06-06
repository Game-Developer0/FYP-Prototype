using UnityEngine;

public class ProjectileDamage : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 15;

    [Header("Damage Effect")]
    public PlayerHealth.DamageEffectType damageEffectType = PlayerHealth.DamageEffectType.None;
    public float screenEffectDuration = 0.7f;

    [Header("Slow Effect")]
    public bool applySlowOnHit = false;
    public float slowMultiplier = 0.6f;
    public float slowDuration = 1.5f;

    [Header("Projectile Life")]
    public float lifeTime = 3f;
    public float ignoreCollisionTime = 0.15f;

    [Header("Impact Effect")]
    public GameObject hitEffectPrefab;
    public float destroyHitEffectAfter = 3f;

    [Header("Destroy Settings")]
    public bool destroyOnHit = true;
    public float destroyProjectileAfterHitDelay = 0f;

    [Header("Optional Area Damage")]
    public bool useAreaDamage = false;
    public float areaRadius = 3f;
    public LayerMask damageLayers;

    [Header("Extra Hit Detection")]
    public bool useExtraHitDetection = true;
    public float extraDetectionRadius = 0.6f;
    public LayerMask extraHitLayers = ~0;

    private bool hasHit = false;
    private float spawnTime;
    private Vector3 lastPosition;
    private Rigidbody rb;
    private Collider ownCollider;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ownCollider = GetComponent<Collider>();
    }

    private void Start()
    {
        spawnTime = Time.time;
        lastPosition = transform.position;

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (hasHit)
            return;

        if (!useExtraHitDetection)
        {
            lastPosition = transform.position;
            return;
        }

        if (Time.time < spawnTime + ignoreCollisionTime)
        {
            lastPosition = transform.position;
            return;
        }

        CheckHitBetweenLastAndCurrentPosition();
        CheckHitAtCurrentPosition();

        lastPosition = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        Vector3 hitPoint = GetSafeHitPoint(other);
        HandleHit(other, hitPoint);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Vector3 hitPoint = transform.position;

        if (collision.contactCount > 0)
            hitPoint = collision.contacts[0].point;

        HandleHit(collision.collider, hitPoint);
    }

    private void CheckHitBetweenLastAndCurrentPosition()
    {
        Vector3 currentPosition = transform.position;
        Vector3 direction = currentPosition - lastPosition;
        float distance = direction.magnitude;

        if (distance <= 0.001f)
            return;

        RaycastHit[] hits = Physics.SphereCastAll(
            lastPosition,
            extraDetectionRadius,
            direction.normalized,
            distance,
            extraHitLayers,
            QueryTriggerInteraction.Collide
        );

        if (hits == null || hits.Length == 0)
            return;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            if (IsOwnCollider(hit.collider))
                continue;

            HandleHit(hit.collider, hit.point);
            return;
        }
    }

    private void CheckHitAtCurrentPosition()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            extraDetectionRadius,
            extraHitLayers,
            QueryTriggerInteraction.Collide
        );

        if (hits == null || hits.Length == 0)
            return;

        foreach (Collider hit in hits)
        {
            if (hit == null)
                continue;

            if (IsOwnCollider(hit))
                continue;

            HandleHit(hit, GetSafeHitPoint(hit));
            return;
        }
    }

    private bool IsOwnCollider(Collider other)
    {
        if (other == null)
            return true;

        if (other == ownCollider)
            return true;

        if (other.transform.root == transform.root)
            return true;

        return false;
    }

    private Vector3 GetSafeHitPoint(Collider other)
    {
        if (other == null)
            return transform.position;

        if (CanUseClosestPoint(other))
            return other.ClosestPoint(transform.position);

        return transform.position;
    }

    private bool CanUseClosestPoint(Collider collider)
    {
        if (collider == null) return false;

        if (collider is BoxCollider) return true;
        if (collider is SphereCollider) return true;
        if (collider is CapsuleCollider) return true;

        MeshCollider meshCollider = collider as MeshCollider;

        if (meshCollider != null && meshCollider.convex)
            return true;

        return false;
    }

    private void HandleHit(Collider other, Vector3 hitPoint)
    {
        if (hasHit)
            return;

        if (Time.time < spawnTime + ignoreCollisionTime)
            return;

        if (other == null)
            return;

        if (other.CompareTag("Arrow"))
            return;

        hasHit = true;

        Debug.Log("Projectile hit: " + other.name);

        if (useAreaDamage)
        {
            DoAreaDamage(hitPoint);
        }
        else
        {
            DamageSingleTarget(other);
        }

        SpawnHitEffect(hitPoint);

        if (destroyOnHit)
        {
            DestroyProjectileNow();
        }
    }

    private void DamageSingleTarget(Collider other)
    {
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

        if (playerHealth == null)
            playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamageWithEffect(
                damage,
                damageEffectType,
                screenEffectDuration,
                applySlowOnHit,
                slowMultiplier,
                slowDuration
            );

            Debug.Log(gameObject.name + " damaged player: " + damage);
        }
    }

    private void DoAreaDamage(Vector3 center)
    {
        Collider[] hits = Physics.OverlapSphere(
            center,
            areaRadius,
            damageLayers,
            QueryTriggerInteraction.Collide
        );

        foreach (Collider hit in hits)
        {
            PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();

            if (playerHealth == null)
                playerHealth = hit.GetComponentInParent<PlayerHealth>();

            if (playerHealth != null)
            {
                playerHealth.TakeDamageWithEffect(
                    damage,
                    damageEffectType,
                    screenEffectDuration,
                    applySlowOnHit,
                    slowMultiplier,
                    slowDuration
                );

                Debug.Log(gameObject.name + " area damaged player: " + damage);
                return;
            }
        }
    }

    private void SpawnHitEffect(Vector3 hitPoint)
    {
        if (hitEffectPrefab == null)
            return;

        GameObject effect = Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);
        Destroy(effect, destroyHitEffectAfter);
    }

    private void DestroyProjectileNow()
    {
        Debug.Log("Destroying projectile immediately: " + gameObject.name);

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
        }

        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>();

        foreach (ParticleSystem particle in particles)
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Clear(true);
        }

        // Hide it immediately from the scene.
        gameObject.SetActive(false);

        // Destroy the actual projectile root.
        Destroy(gameObject);
    }
}