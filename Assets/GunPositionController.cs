using UnityEngine;

public class GunPositionController : MonoBehaviour
{
    [Header("References")]
    public PlayerMovement playerMovement;
    public GrapplingGun grapplingGun;

    [Header("Gun Target Positions")]
    public Transform handPosition;
    public Transform airPosition;

    [Header("Settings")]
    public float moveSpeed = 15f;
    public float rotationSpeed = 15f;

    private void LateUpdate()
    {
        if (playerMovement == null || handPosition == null || airPosition == null)
            return;

        bool isGrappling = grapplingGun != null && grapplingGun.IsGrappling();
        bool isInAir = !playerMovement.grounded;

        Transform target;

        if (isGrappling || isInAir)
        {
            target = airPosition;
        }
        else
        {
            target = handPosition;
        }

        // Use LOCAL position because gun and target objects are under same WeaponHolder
        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            target.localPosition,
            Time.deltaTime * moveSpeed
        );

        // Copy rotation also
        // But while grappling, let RotateGun script aim toward grapple point
        if (!isGrappling)
        {
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                target.localRotation,
                Time.deltaTime * rotationSpeed
            );
        }
    }
}