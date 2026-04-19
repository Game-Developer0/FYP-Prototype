using UnityEngine;

public class ArrowShoot : MonoBehaviour
{
    public GameObject ArrowPrefab;
    public Transform ArrowSpawnPosition;
    public GameObject HandArrow;
    public Camera playerCam;

    public float range = 1000f;
    public float arrowForce = 40f;

    private RaycastHit hit;

    void Shoot()
    {
        HandArrow.SetActive(false);

        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 targetPoint;

        if (Physics.Raycast(ray, out hit, range))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.origin + ray.direction * range;
        }

        Vector3 shootDirection = (targetPoint - ArrowSpawnPosition.position).normalized;
        Quaternion shootRotation = Quaternion.LookRotation(shootDirection);

        GameObject arrowInstance = Instantiate(
            ArrowPrefab,
            ArrowSpawnPosition.position,
            shootRotation
        );

        Rigidbody rb = arrowInstance.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = shootDirection * arrowForce;
        }
    }
}