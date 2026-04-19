using UnityEngine;

public class AimTargetFollowCrosshair : MonoBehaviour
{
    public Camera playerCam;
    public float maxDistance = 100f;
    public float fallbackDistance = 20f;
    public LayerMask aimLayers;

    private void LateUpdate()
    {
        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        Vector3 targetPos;

        if (Physics.Raycast(ray, out hit, maxDistance, aimLayers))
        {
            targetPos = hit.point;
        }
        else
        {
            targetPos = ray.origin + ray.direction * fallbackDistance;
        }

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 20f);
    }
}