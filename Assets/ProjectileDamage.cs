using UnityEngine;

public class ProjectileDamage : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 15;

    [Header("Projectile Life")]
    public float lifeTime = 5f;
    public float ignoreCollisionTime = 0.15f;

    [Header("Impact Effect")]
    public GameObject hitEffectPrefab;
    public bool destroyOnHit = true;
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
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other, other.ClosestPoint(transform.position));
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

        // Prevent projectile from instantly hitting the dragon mouth/body when spawned.
        if (Time.time < spawnTime + ignoreCollisionTime) return;

        // Ignore other projectiles/arrows.
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
            playerHealth.TakeDamage(damage);
            Debug.Log(gameObject.name + " damaged player: " + damage);
        }
    }

    private void DoAreaDamage(Vector3 center)
    {
        Collider[] hits = Physics.OverlapSphere(center, areaRadius, damageLayers, QueryTriggerInteraction.Ignore);

        foreach (Collider hit in hits)
        {
            PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();

            if (playerHealth == null)
            {
                playerHealth = hit.GetComponentInParent<PlayerHealth>();
            }

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
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