using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public enum DamageEffectType
    {
        None,
        Fire,
        Acid,
        Ice,
        Volcano
    }

    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Optional Animation")]
    public Animator animator;
    public string deathBoolName = "Death";

    [Header("Screen Effects")]
    public GameObject fireScreenEffectRoot;
    public GameObject acidScreenEffectRoot;
    public GameObject iceScreenEffectRoot;
    public GameObject volcanoScreenEffectRoot;

    [Tooltip("Keep this OFF if your screen effect uses HS_ScreenEffect.")]
    public bool disableScreenEffectObjectOnStop = false;

    public bool clearParticlesOnStop = true;

    private Coroutine fireEffectRoutine;
    private Coroutine acidEffectRoutine;
    private Coroutine iceEffectRoutine;
    private Coroutine volcanoEffectRoutine;

    [Header("Slow Effect")]
    public bool allowSlowEffect = true;
    private Coroutine slowRoutine;

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        StopScreenEffect(fireScreenEffectRoot);
        StopScreenEffect(acidScreenEffectRoot);
        StopScreenEffect(iceScreenEffectRoot);
        StopScreenEffect(volcanoScreenEffectRoot);

        SetPlayerSpeedMultiplier(1f);
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log("Player HP: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void TakeDamageWithEffect(
        int damageAmount,
        DamageEffectType effectType,
        float screenEffectDuration,
        bool applySlow,
        float slowMultiplier,
        float slowDuration
    )
    {
        if (isDead) return;

        TakeDamage(damageAmount);

        if (effectType != DamageEffectType.None)
        {
            PlayScreenEffect(effectType, screenEffectDuration);
        }

        if (applySlow && allowSlowEffect)
        {
            ApplySlow(slowMultiplier, slowDuration);
        }
    }

    void PlayScreenEffect(DamageEffectType effectType, float duration)
    {
        if (duration <= 0f) return;

        if (effectType == DamageEffectType.Fire)
        {
            StartScreenEffectRoutine(ref fireEffectRoutine, fireScreenEffectRoot, duration);
        }
        else if (effectType == DamageEffectType.Acid)
        {
            StartScreenEffectRoutine(ref acidEffectRoutine, acidScreenEffectRoot, duration);
        }
        else if (effectType == DamageEffectType.Ice)
        {
            StartScreenEffectRoutine(ref iceEffectRoutine, iceScreenEffectRoot, duration);
        }
        else if (effectType == DamageEffectType.Volcano)
        {
            StartScreenEffectRoutine(ref volcanoEffectRoutine, volcanoScreenEffectRoot, duration);
        }
    }

    void StartScreenEffectRoutine(ref Coroutine routine, GameObject effectRoot, float duration)
    {
        if (effectRoot == null) return;

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        StopScreenEffect(effectRoot);

        routine = StartCoroutine(ScreenEffectRoutine(effectRoot, duration));
    }

    IEnumerator ScreenEffectRoutine(GameObject effectRoot, float duration)
    {
        if (effectRoot == null) yield break;

        effectRoot.SetActive(true);

        Hovl.HS_ScreenEffect[] screenEffects = effectRoot.GetComponentsInChildren<Hovl.HS_ScreenEffect>(true);

        if (screenEffects != null && screenEffects.Length > 0)
        {
            foreach (Hovl.HS_ScreenEffect screenEffect in screenEffects)
            {
                if (screenEffect == null) continue;
                screenEffect.PlayEffect();
            }
        }
        else
        {
            ParticleSystem[] particles = effectRoot.GetComponentsInChildren<ParticleSystem>(true);

            foreach (ParticleSystem particle in particles)
            {
                if (particle == null) continue;

                particle.gameObject.SetActive(true);

                var emission = particle.emission;
                emission.enabled = true;

                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particle.Clear(true);
                particle.Play(true);
            }
        }

        yield return new WaitForSeconds(duration);

        StopScreenEffect(effectRoot);
    }

    void StopScreenEffect(GameObject effectRoot)
    {
        if (effectRoot == null) return;

        Hovl.HS_ScreenEffect[] screenEffects = effectRoot.GetComponentsInChildren<Hovl.HS_ScreenEffect>(true);

        if (screenEffects != null && screenEffects.Length > 0)
        {
            foreach (Hovl.HS_ScreenEffect screenEffect in screenEffects)
            {
                if (screenEffect == null) continue;
                screenEffect.StopEffect();
            }
        }
        else
        {
            ParticleSystem[] particles = effectRoot.GetComponentsInChildren<ParticleSystem>(true);

            foreach (ParticleSystem particle in particles)
            {
                if (particle == null) continue;

                var emission = particle.emission;
                emission.enabled = false;

                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                if (clearParticlesOnStop)
                {
                    particle.Clear(true);
                }
            }
        }

        if (disableScreenEffectObjectOnStop)
        {
            effectRoot.SetActive(false);
        }
    }

    void ApplySlow(float slowMultiplier, float slowDuration)
    {
        if (slowRoutine != null)
        {
            StopCoroutine(slowRoutine);
        }

        slowRoutine = StartCoroutine(SlowRoutine(slowMultiplier, slowDuration));
    }

    IEnumerator SlowRoutine(float slowMultiplier, float slowDuration)
    {
        slowMultiplier = Mathf.Clamp(slowMultiplier, 0.1f, 1f);

        SetPlayerSpeedMultiplier(slowMultiplier);

        yield return new WaitForSeconds(slowDuration);

        SetPlayerSpeedMultiplier(1f);

        slowRoutine = null;
    }

    void SetPlayerSpeedMultiplier(float multiplier)
    {
        gameObject.SendMessage(
            "SetExternalSpeedMultiplier",
            multiplier,
            SendMessageOptions.DontRequireReceiver
        );
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;

        SetPlayerSpeedMultiplier(1f);

        Debug.Log("Player died");

        if (animator != null)
        {
            animator.SetBool(deathBoolName, true);
        }
    }
}