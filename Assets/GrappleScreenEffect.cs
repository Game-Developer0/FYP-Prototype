using UnityEngine;

public class GrappleScreenEffect : MonoBehaviour
{
    [Header("Grappling")]
    public GrapplingGun grapplingGun;

    [Header("Screen Effect")]
    public GameObject speedScreenEffect;

    [Header("Optional Fade")]
    public bool useFade = false;
    public CanvasGroup effectCanvasGroup;
    public float fadeSpeed = 8f;

    private bool effectActive;

    private void Start()
    {
        if (speedScreenEffect != null)
        {
            speedScreenEffect.SetActive(false);
        }

        if (effectCanvasGroup != null)
        {
            effectCanvasGroup.alpha = 0f;
        }
    }

    private void Update()
    {
        if (grapplingGun == null || speedScreenEffect == null) return;

        bool isGrappling = grapplingGun.IsGrappling();

        if (useFade && effectCanvasGroup != null)
        {
            HandleFadeEffect(isGrappling);
        }
        else
        {
            HandleSimpleEffect(isGrappling);
        }
    }

    private void HandleSimpleEffect(bool isGrappling)
    {
        if (isGrappling && !effectActive)
        {
            speedScreenEffect.SetActive(true);
            effectActive = true;
        }

        if (!isGrappling && effectActive)
        {
            speedScreenEffect.SetActive(false);
            effectActive = false;
        }
    }

    private void HandleFadeEffect(bool isGrappling)
    {
        if (isGrappling && !speedScreenEffect.activeSelf)
        {
            speedScreenEffect.SetActive(true);
        }

        float targetAlpha = isGrappling ? 1f : 0f;

        effectCanvasGroup.alpha = Mathf.Lerp(
            effectCanvasGroup.alpha,
            targetAlpha,
            Time.deltaTime * fadeSpeed
        );

        if (!isGrappling && effectCanvasGroup.alpha <= 0.02f)
        {
            effectCanvasGroup.alpha = 0f;
            speedScreenEffect.SetActive(false);
        }
    }
}