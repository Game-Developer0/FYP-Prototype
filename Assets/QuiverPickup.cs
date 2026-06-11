using UnityEngine;

public class QuiverPickup : MonoBehaviour
{
    [Header("Which arrow this quiver gives")]
    public ArrowShoot.ArrowType arrowType = ArrowShoot.ArrowType.Fire;

    [Header("Pickup Settings")]
    public bool destroyAfterPickup = true;

    private bool pickedUp = false;

    private void OnTriggerEnter(Collider other)
    {
        if (pickedUp) return;

        PlayerMovement playerMovement = other.GetComponentInParent<PlayerMovement>();

        if (playerMovement == null) return;

        pickedUp = true;

        playerMovement.SetArrowType(arrowType);

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