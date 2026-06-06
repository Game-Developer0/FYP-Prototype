using UnityEngine;
using UnityEngine.UI;

public class AnimalHealthBar : MonoBehaviour
{
    [Header("UI")]
    public Slider slider;
    public Gradient gradient;
    public Image fill;

    [Header("Face Camera")]
    public bool faceCamera = true;
    public Camera targetCamera;

    void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    void LateUpdate()
    {
        if (!faceCamera) return;

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null) return;
        }

        transform.LookAt(
            transform.position + targetCamera.transform.rotation * Vector3.forward,
            targetCamera.transform.rotation * Vector3.up
        );
    }

    public void SetMaxHealth(int health)
    {
        if (slider == null) return;

        slider.maxValue = health;
        slider.value = health;

        if (fill != null)
        {
            fill.color = gradient.Evaluate(1f);
        }
    }

    public void SetHealth(int health)
    {
        if (slider == null) return;

        slider.value = health;

        if (fill != null)
        {
            fill.color = gradient.Evaluate(slider.normalizedValue);
        }
    }
}