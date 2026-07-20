using UnityEngine;

public class QuiverPickup : MonoBehaviour
{
    [Header("Which arrow this quiver gives")]
    public ArrowShoot.ArrowType arrowType =
        ArrowShoot.ArrowType.Fire;

    [Header("Arrow Amount")]
    [Min(1)]
    public int arrowsToAdd = 10;

    [Header("Pickup Settings")]
    public bool destroyAfterPickup = true;

    private bool pickedUp = false;

    private void OnTriggerEnter(Collider other)
    {
        if (pickedUp)
        {
            return;
        }

        PlayerMovement playerMovement =
            other.GetComponentInParent<PlayerMovement>();

        if (playerMovement == null)
        {
            return;
        }

        // True means Unity can also find ArrowShoot
        // when the bow holder is inactive.
        ArrowShoot arrowShoot =
            playerMovement.GetComponentInChildren<ArrowShoot>(true);

        if (arrowShoot == null)
        {
            Debug.LogWarning(
                "Quiver could not find the ArrowShoot component."
            );

            return;
        }

        pickedUp = true;

        // Preserve your existing arrow-type-changing system.
        playerMovement.SetArrowType(arrowType);

        // Also directly update ArrowShoot to guarantee that
        // the correct projectile type is selected.
        arrowShoot.SetArrowType(arrowType);

        // Add 10 arrows. ArrowShoot limits the amount to 20.
        arrowShoot.AddArrows(arrowsToAdd);

        if (destroyAfterPickup)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}