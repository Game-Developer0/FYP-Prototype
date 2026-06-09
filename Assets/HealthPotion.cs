using UnityEngine;

public class HealthPotion : MonoBehaviour
{
    [Header("Potion Settings")]
    public int healAmount = 25;
    public float healScreenEffectDuration = 3f;

    [Header("Pickup Settings")]
    public string playerTag = "Player";
    public bool onlyUseWhenPlayerNeedsHealth = true;
    public bool destroyAfterPickup = true;

    [Header("Optional Pickup Effect")]
    public GameObject pickupEffectPrefab;
    public float destroyPickupEffectAfter = 2f;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private bool pickedUp = false;

    void OnTriggerEnter(Collider other)
    {
        if (pickedUp) return;

        if (showDebugLogs)
        {
            Debug.Log("Potion touched by: " + other.name);
        }

        if (!other.CompareTag(playerTag))
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("Touched object is not tagged as Player.");
            }

            return;
        }

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

        if (playerHealth == null)
        {
            playerHealth = other.GetComponentInParent<PlayerHealth>();
        }

        if (playerHealth == null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("No PlayerHealth found on player.");
            }

            return;
        }

        if (onlyUseWhenPlayerNeedsHealth && playerHealth.currentHealth >= playerHealth.maxHealth)
        {
            if (showDebugLogs)
            {
                Debug.Log("Player health is already full. Potion not used.");
            }

            return;
        }

        pickedUp = true;

        if (showDebugLogs)
        {
            Debug.Log("Potion used. Healing player by: " + healAmount);
        }

        playerHealth.HealWithEffect(healAmount, healScreenEffectDuration);

        if (pickupEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                pickupEffectPrefab,
                transform.position,
                Quaternion.identity
            );

            Destroy(effect, destroyPickupEffectAfter);
        }

        if (destroyAfterPickup)
        {
            Destroy(gameObject);
        }
    }
}