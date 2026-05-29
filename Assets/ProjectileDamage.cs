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
    public bool destroyOnHit = false;
    public float destroyHitEffectAfter = 3f;

    [Header("Optional Area Damage")]
    public bool useAreaDamage = false;
    public float areaRadius = 3f;
    public LayerMask damageLayers;

    private bool hasHit = false;
    private float spawnTime;

    private void Start()
    {
        spawnTime = Time.time;

        // Projectile always disappears after 3 seconds.
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        Vector3 hitPoint = transform.position;

        if (other != null)
        {
            if (CanUseClosestPoint(other))
            {
                hitPoint = other.ClosestPoint(transform.position);
            }
            else
            {
                hitPoint = transform.position;
            }
        }

        HandleHit(other, hitPoint);
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

    private void OnCollisionEnter(Collision collision)
    {
        Vector3 hitPoint = transform.position;

        if (collision.contactCount > 0)
        {
            hitPoint = collision.contacts[0].point;
        }

        HandleHit(collision.collider, hitPoint);
    }

    private void HandleHit(Collider other, Vector3 hitPoint)
    {
        if (hasHit) return;

        if (Time.time < spawnTime + ignoreCollisionTime) return;

        if (other == null) return;

        if (other.CompareTag("Arrow")) return;

        hasHit = true;

        if (useAreaDamage)
        {
            DoAreaDamage(hitPoint);
        }
        else
        {
            DamageSingleTarget(other);
        }

        SpawnHitEffect(hitPoint);

        // We do NOT destroy instantly now.
        // The ball will disappear after Life Time.
        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
    }

    private void DamageSingleTarget(Collider other)
    {
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

        if (playerHealth == null)
        {
            playerHealth = other.GetComponentInParent<PlayerHealth>();
        }

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
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider hit in hits)
        {
            PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();

            if (playerHealth == null)
            {
                playerHealth = hit.GetComponentInParent<PlayerHealth>();
            }

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
        if (hitEffectPrefab == null) return;

        GameObject effect = Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);
        Destroy(effect, destroyHitEffectAfter);
    }
}