using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterStatus : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    [Header("UI References")]
    public Slider healthSlider;
    public TMP_Text healthText;

    [Header("Animation")]
    public Animator animator;  // 👈 Add this

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateUI();

        if (animator != null)
        animator.SetTrigger("Idle");
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);
        UpdateUI();

        if (animator != null)
        {
            if (currentHealth <= 0)
            {
                isDead = true;
                animator.SetTrigger("Death");
            }
            else
            {
                animator.SetTrigger("Hit");
            }
        }
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        UpdateUI();
    }

    void UpdateUI()
    {
        if (healthSlider != null)
            healthSlider.value = (float)currentHealth / maxHealth;

        if (healthText != null)
            healthText.text = currentHealth + "/" + maxHealth;
    }

    public bool IsDead()
    {
        return currentHealth <= 0;
    }

    public void PlayAction(string action)
    {
        if (animator == null || isDead) return;
        animator.SetTrigger(action);
    }
}
