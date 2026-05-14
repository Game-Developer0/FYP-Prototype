using UnityEngine;

public class RotateGun : MonoBehaviour
{
    public GrapplingGun grappling;

    private Quaternion desiredRotation;
    private Quaternion startLocalRotation;
    private Vector3 startLocalPosition;

    private float rotationSpeed = 5f;

    void Start()
    {
        // Save the gun's original position and rotation from the scene
        startLocalRotation = transform.localRotation;
        startLocalPosition = transform.localPosition;
    }

    void Update()
    {
        // Keep gun fixed in the hand
        transform.localPosition = startLocalPosition;

        if (!grappling.IsGrappling())
        {
            // Go back to original local rotation, not parent world rotation
            transform.localRotation = Quaternion.Lerp(
                transform.localRotation,
                startLocalRotation,
                Time.deltaTime * rotationSpeed
            );
        }
        else
        {
            desiredRotation = Quaternion.LookRotation(grappling.GetGrapplePoint() - transform.position);

            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                desiredRotation,
                Time.deltaTime * rotationSpeed
            );
        }
    }
}