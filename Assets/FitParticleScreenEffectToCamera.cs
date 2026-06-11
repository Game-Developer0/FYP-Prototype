using UnityEngine;

public class FitParticleScreenEffectToCamera : MonoBehaviour
{
    public Camera targetCamera;
    public float distanceFromCamera = 1f;
    public float sizeMultiplier = 1.25f;

    void LateUpdate()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        transform.position = targetCamera.transform.position + targetCamera.transform.forward * distanceFromCamera;
        transform.rotation = targetCamera.transform.rotation;

        float height = 2f * distanceFromCamera * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float width = height * targetCamera.aspect;

        transform.localScale = new Vector3(width * sizeMultiplier, height * sizeMultiplier, 1f);
    }
}