using UnityEngine;

public class BossWolfHandDamage : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 25;
    public float damageCooldown = 0.8f;
    public bool damageOnlyOncePerAttack = true;

    [Header("Player")]
    public string playerTag = "Player";

    [Header("Hand Hitboxes")]
    public Collider[] handHitboxes;

    [Header("Debug")]
    public bool showDebugMessages = true;

    private bool damageActive = false;
    private bool damagedThisAttack = false;
    private float nextDamageTime = 0f;

    private void Start()
    {
        SetHandHitboxes(false);
    }

    public void EnableHandDamage()
    {
        damageActive = true;
        damagedThisAttack = false;

        SetHandHitboxes(true);

        if (showDebugMessages)
        {
            Debug.Log("Boss Wolf hand damage ENABLED");
        }
    }

    public void DisableHandDamage()
    {
        damageActive = false;

        SetHandHitboxes(false);

        if (showDebugMessages)
        {
            Debug.Log("Boss Wolf hand damage DISABLED");
        }
    }

    public void TryDamagePlayer(Collider other)
    {
        if (!damageActive)
            return;

        if (damageOnlyOncePerAttack && damagedThisAttack)
            return;

        if (Time.time < nextDamageTime)
            return;

        if (!other.CompareTag(playerTag))
            return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

        if (playerHealth == null)
            playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
            playerHealth = other.GetComponentInChildren<PlayerHealth>();

        if (playerHealth == null)
        {
            Debug.LogWarning("Boss Wolf hit player, but PlayerHealth was not found.");
            return;
        }

        playerHealth.TakeDamage(damage);

        damagedThisAttack = true;
        nextDamageTime = Time.time + damageCooldown;

        if (showDebugMessages)
        {
            Debug.Log("Boss Wolf hand damaged player: " + damage);
        }
    }

    private void SetHandHitboxes(bool enabled)
    {
        if (handHitboxes == null)
            return;

        foreach (Collider handHitbox in handHitboxes)
        {
            if (handHitbox == null)
                continue;

            handHitbox.isTrigger = true;
            handHitbox.enabled = enabled;
        }
    }
}