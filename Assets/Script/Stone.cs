using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stone : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHp = 6;
    private int currentHp;
    public List<GameObject> damageVariant;
    public GridNode node;

    [Header("Damage Effect")]
    [SerializeField] private float pulseScale = 1.2f;
    [SerializeField] private float pulseDuration = 0.3f;
    [SerializeField] private Color damageColor = Color.red;
    
    private Vector3 originalScale;
    private Color originalColor;
    private Renderer stoneRenderer;
    private Coroutine damageEffectCoroutine;

    // =============================
    void Awake()
    {
        currentHp = maxHp;
        originalScale = transform.localScale;
        stoneRenderer = GetComponentInChildren<Renderer>();
        if (stoneRenderer != null)
            originalColor = stoneRenderer.material.color;
        
        UpdateVariant();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("cutter"))
        {
            Damage(1); // default cutter damage
        }
    }
    
    // =============================
    public void Damage(int dmg)
    {
        if (currentHp <= 0) return;

        currentHp -= dmg;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);

        UpdateVariant();
        TriggerDamageEffect();

        if (currentHp <= 0)
        {
            Break();
        }
    }

    // =============================
    void UpdateVariant()
    {
        if (damageVariant == null || damageVariant.Count == 0)
            return;

        int damageIndex = maxHp - currentHp;
        damageIndex = Mathf.Clamp(damageIndex, 0, damageVariant.Count - 1);

        for (int i = 0; i < damageVariant.Count; i++)
        {
            damageVariant[i].SetActive(i == damageIndex);
        }
    }

    // Breathing/pulse effect on damage
    void TriggerDamageEffect()
    {
        if (damageEffectCoroutine != null)
            StopCoroutine(damageEffectCoroutine);
        
        damageEffectCoroutine = StartCoroutine(DamageEffectRoutine());
    }

    IEnumerator DamageEffectRoutine()
    {
        float timer = 0;
        
        while (timer < pulseDuration)
        {
            timer += Time.deltaTime;
            float t = timer / pulseDuration;
            
            // Pulse in and out
            float scale = Mathf.Lerp(1f, pulseScale, Mathf.Sin(t * Mathf.PI));
            transform.localScale = originalScale * scale;
            
            // Color flash (optional)
            if (stoneRenderer != null)
            {
                Color lerpedColor = Color.Lerp(originalColor, damageColor, Mathf.Sin(t * Mathf.PI));
                stoneRenderer.material.color = lerpedColor;
            }
            
            yield return null;
        }
        
        // Reset to original
        transform.localScale = originalScale;
        if (stoneRenderer != null)
            stoneRenderer.material.color = originalColor;
    }

    // =============================
    void Break()
    {
        if (damageEffectCoroutine != null)
            StopCoroutine(damageEffectCoroutine);
            
        if (node != null)
        {
            node.walkable = true;
            node.NewUnlock = true;
            node.stone = null;
        }
        GridManager.Instance.GridUpdateAfter4Stonedistroy();
        Destroy(gameObject);
    }
}