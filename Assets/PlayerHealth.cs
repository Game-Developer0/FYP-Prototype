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

    [Header("Health Bar")]
    public HealthBar healthBar;
    public bool autoFindHealthBarIfMissing = true;

    [Header("Optional Animation")]
    public Animator animator;
    public string deathBoolName = "Death";

    [Header("Canvas Screen Particles")]
    public GameObject fireScreenEffectRoot;
    public GameObject acidScreenEffectRoot;
    public GameObject iceScreenEffectRoot;
    public GameObject volcanoScreenEffectRoot;

    [Header("Canvas Particle Settings")]
    public bool forceParticlesLocalSpace = true;
    public bool disableElementObjectAfterStop = true;
    public bool clearParticlesOnStop = true;

    [Header("Canvas Blood Particle Effect")]
    public GameObject bloodScreenEffectRoot;
    public int bloodScreenHealthThreshold = 40;
    public bool disableBloodObjectWhenHidden = true;
    public bool keepBloodEffectOnDeath = true;

    private Coroutine fireEffectRoutine;
    private Coroutine acidEffectRoutine;
    private Coroutine iceEffectRoutine;
    private Coroutine volcanoEffectRoutine;

    private bool bloodEffectActive = false;

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

        if (healthBar == null && autoFindHealthBarIfMissing)
        {
            healthBar = FindObjectOfType<HealthBar>();
        }

        UpdateHealthBarMax();

        StopCanvasParticleEffect(fireScreenEffectRoot, true);
        StopCanvasParticleEffect(acidScreenEffectRoot, true);
        StopCanvasParticleEffect(iceScreenEffectRoot, true);
        StopCanvasParticleEffect(volcanoScreenEffectRoot, true);

        HideBloodScreenEffect();

        SetPlayerSpeedMultiplier(1f);
    }

    void UpdateHealthBarMax()
    {
        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
            healthBar.SetHealth(currentHealth);
        }
        else
        {
            Debug.LogWarning("HealthBar is not assigned on PlayerHealth.");
        }
    }

    void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthBar();
        UpdateBloodScreenEffect();

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

    public void Heal(int healAmount)
    {
        if (isDead) return;

        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthBar();
        UpdateBloodScreenEffect();

        Debug.Log("Player HP: " + currentHealth);
    }

    void PlayScreenEffect(DamageEffectType effectType, float duration)
    {
        if (duration <= 0f) return;

        if (effectType == DamageEffectType.Fire)
        {
            StartCanvasParticleRoutine(ref fireEffectRoutine, fireScreenEffectRoot, duration);
        }
        else if (effectType == DamageEffectType.Acid)
        {
            StartCanvasParticleRoutine(ref acidEffectRoutine, acidScreenEffectRoot, duration);
        }
        else if (effectType == DamageEffectType.Ice)
        {
            StartCanvasParticleRoutine(ref iceEffectRoutine, iceScreenEffectRoot, duration);
        }
        else if (effectType == DamageEffectType.Volcano)
        {
            StartCanvasParticleRoutine(ref volcanoEffectRoutine, volcanoScreenEffectRoot, duration);
        }
    }

    void StartCanvasParticleRoutine(ref Coroutine routine, GameObject effectRoot, float duration)
    {
        if (effectRoot == null) return;

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        StopCanvasParticleEffect(effectRoot, true);

        routine = StartCoroutine(CanvasParticleRoutine(effectRoot, duration));
    }

    IEnumerator CanvasParticleRoutine(GameObject effectRoot, float duration)
    {
        if (effectRoot == null) yield break;

        PlayCanvasParticleEffect(effectRoot);

        yield return new WaitForSeconds(duration);

        StopCanvasParticleEffect(effectRoot, true);
    }

    void PlayCanvasParticleEffect(GameObject effectRoot)
    {
        if (effectRoot == null) return;

        effectRoot.SetActive(true);

        ParticleSystem[] particles =
            effectRoot.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem particle in particles)
        {
            if (particle == null) continue;

            particle.gameObject.SetActive(true);

            ParticleSystem.MainModule main = particle.main;

            if (forceParticlesLocalSpace)
            {
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
            }

            main.loop = true;

            ParticleSystem.EmissionModule emission = particle.emission;
            emission.enabled = true;

            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Clear(true);
            particle.Play(true);
        }
    }

    void StopCanvasParticleEffect(GameObject effectRoot, bool disableObject)
    {
        if (effectRoot == null) return;

        ParticleSystem[] particles =
            effectRoot.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem particle in particles)
        {
            if (particle == null) continue;

            ParticleSystem.EmissionModule emission = particle.emission;
            emission.enabled = false;

            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            if (clearParticlesOnStop)
            {
                particle.Clear(true);
            }
        }

        if (disableObject && disableElementObjectAfterStop)
        {
            effectRoot.SetActive(false);
        }
    }

    void UpdateBloodScreenEffect()
    {
        if (bloodScreenEffectRoot == null) return;

        bool shouldShowBlood = currentHealth <= bloodScreenHealthThreshold;

        if (isDead && !keepBloodEffectOnDeath)
        {
            shouldShowBlood = false;
        }

        if (shouldShowBlood && !bloodEffectActive)
        {
            ShowBloodScreenEffect();
        }
        else if (!shouldShowBlood && bloodEffectActive)
        {
            HideBloodScreenEffect();
        }
    }

    void ShowBloodScreenEffect()
    {
        if (bloodScreenEffectRoot == null) return;

        bloodScreenEffectRoot.SetActive(true);

        ParticleSystem[] particles =
            bloodScreenEffectRoot.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem particle in particles)
        {
            if (particle == null) continue;

            particle.gameObject.SetActive(true);

            ParticleSystem.MainModule main = particle.main;

            if (forceParticlesLocalSpace)
            {
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
            }

            main.loop = true;

            ParticleSystem.EmissionModule emission = particle.emission;
            emission.enabled = true;

            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Clear(true);
            particle.Play(true);
        }

        bloodEffectActive = true;
    }

    void HideBloodScreenEffect()
    {
        if (bloodScreenEffectRoot == null) return;

        ParticleSystem[] particles =
            bloodScreenEffectRoot.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem particle in particles)
        {
            if (particle == null) continue;

            ParticleSystem.EmissionModule emission = particle.emission;
            emission.enabled = false;

            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            if (clearParticlesOnStop)
            {
                particle.Clear(true);
            }
        }

        if (disableBloodObjectWhenHidden)
        {
            bloodScreenEffectRoot.SetActive(false);
        }

        bloodEffectActive = false;
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

        if (!keepBloodEffectOnDeath)
        {
            HideBloodScreenEffect();
        }
        else
        {
            ShowBloodScreenEffect();
        }

        Debug.Log("Player died");

        if (animator != null)
        {
            animator.SetBool(deathBoolName, true);
        }
    }
}