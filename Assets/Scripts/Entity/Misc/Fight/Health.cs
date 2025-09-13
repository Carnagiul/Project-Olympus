// Health.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class Health : MonoBehaviour
{
    [Min(1f)][SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = -1f;

    public float Max => maxHealth;
    public float Current => currentHealth;
    public bool IsAlive { get; private set; } = true;

    [Header("Events")]
    public UnityEvent<float, float> OnHealthChanged; // (current, max)
    public UnityEvent<float> OnDamaged;              // (finalDamage)
    public UnityEvent<float> OnHealed;               // (healAmount)
    public UnityEvent OnDeath;

    void Awake()
    {
        OnHealthChanged ??= new UnityEvent<float, float>();
        OnDamaged ??= new UnityEvent<float>();
        OnHealed ??= new UnityEvent<float>();
        OnDeath ??= new UnityEvent();

        if (currentHealth < 0f) currentHealth = maxHealth;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        IsAlive = currentHealth > 0f;
        OnHealthChanged.Invoke(currentHealth, maxHealth);
    }

    public void SetMax(float newMax, bool refill = false)
    {
        maxHealth = Mathf.Max(1f, newMax);
        if (refill) currentHealth = maxHealth;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        OnHealthChanged.Invoke(currentHealth, maxHealth);
    }

    public void Heal(float amount)
    {
        if (!IsAlive || amount <= 0f) return;
        float before = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        float healed = currentHealth - before;
        if (healed > 0f)
        {
            OnHealed.Invoke(healed);
            OnHealthChanged.Invoke(currentHealth, maxHealth);
        }
    }

    public void ApplyFinalDamage(float finalDamage)
    {
        if (!IsAlive || finalDamage <= 0f) return;
        float before = currentHealth;
        currentHealth = Mathf.Max(0f, currentHealth - finalDamage);
        OnDamaged.Invoke(finalDamage);
        OnHealthChanged.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            IsAlive = false;
            OnDeath.Invoke();
        }
    }

    public void Kill()
    {
        if (!IsAlive) return;
        currentHealth = 0f;
        OnHealthChanged.Invoke(currentHealth, maxHealth);
        IsAlive = false;
        OnDeath.Invoke();
    }
}
