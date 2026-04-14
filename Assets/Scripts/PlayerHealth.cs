using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private int currentHealth;
    [SerializeField] private float invulnerabilityDuration = 1.5f;
    [SerializeField] private float flashInterval = 0.1f;
    
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Image[] heartIcons;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverText;
    
    [Header("Effects")]
    [SerializeField] private GameObject damageEffectPrefab;
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip healSound;
    [SerializeField] private AudioClip deathSound;
    
    [Header("Events")]
    public System.Action OnHealthChanged;
    public System.Action OnPlayerDamaged;
    public System.Action OnPlayerHealed;
    public System.Action OnPlayerDeath;
    
    // Components
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private PlayerInputDemo playerInput;
    private Collider2D playerCollider;
    private Rigidbody2D playerRigidbody;
    private PlayerInventory playerInventory;
    
    // State
    private bool isInvulnerable = false;
    private bool isDead = false;
    
    // Properties
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public float HealthPercentage => (float)currentHealth / maxHealth;
    public bool IsDead => isDead;
    public bool IsInvulnerable => isInvulnerable;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        playerInput = GetComponent<PlayerInputDemo>();
        playerCollider = GetComponent<Collider2D>();
        playerRigidbody = GetComponent<Rigidbody2D>();
        playerInventory = GetComponent<PlayerInventory>();
        
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }
    
    void Start()
    {
        InitializeHealth();
        UpdateHealthUI();
        
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }
    
    public void InitializeHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        isInvulnerable = false;
        
        if (playerCollider != null)
            playerCollider.enabled = true;
        
        if (playerRigidbody != null)
            playerRigidbody.simulated = true;
        
        if (playerInput != null)
            playerInput.SetInputEnabled(true);
        
        OnHealthChanged?.Invoke();
    }
    
    public void TakeDamage(int damageAmount)
    {
        // Проверка на избегание урона от зелья шанса
        if (playerInventory != null && playerInventory.TryAvoidDamage())
        {
            Debug.Log("Damage avoided by chance potion!");
            return;
        }
        
        if (isInvulnerable || isDead || currentHealth <= 0)
        {
            Debug.Log($"Damage blocked: isInvulnerable={isInvulnerable}, isDead={isDead}, currentHealth={currentHealth}");
            return;
        }
        
        int previousHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - damageAmount);
        
        Debug.Log($"Health changed: {previousHealth} -> {currentHealth}");
        
        PlayDamageEffects();
        
        if (currentHealth > 0)
        {
            StartCoroutine(InvulnerabilityRoutine());
        }
        
        UpdateHealthUI();
        
        OnHealthChanged?.Invoke();
        OnPlayerDamaged?.Invoke();
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    // ОДИН метод Heal - удалите дубликат, если он есть!
    public void Heal(int healAmount)
    {
        if (isDead) return;
        
        int previousHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
        
        if (currentHealth > previousHealth)
        {
            Debug.Log($"Healed: {previousHealth} -> {currentHealth}");
            
            if (healSound != null && audioSource != null)
                audioSource.PlayOneShot(healSound);
            
            StartCoroutine(HealFlashRoutine());
            
            UpdateHealthUI();
            OnHealthChanged?.Invoke();
            OnPlayerHealed?.Invoke();
        }
    }
    
    public void FullHeal()
    {
        if (isDead) return;
        
        currentHealth = maxHealth;
        UpdateHealthUI();
        OnHealthChanged?.Invoke();
        OnPlayerHealed?.Invoke();
        
        if (healSound != null && audioSource != null)
            audioSource.PlayOneShot(healSound);
    }
    
    public void IncreaseMaxHealth(int amount)
    {
        maxHealth += amount;
        currentHealth = maxHealth;
        UpdateHealthUI();
        OnHealthChanged?.Invoke();
        Debug.Log($"Max health increased to {maxHealth}");
    }
    
    public void AddExtraLife()
    {
        if (!isDead)
        {
            IncreaseMaxHealth(1);
            Debug.Log("Extra life added from armor purchase!");
        }
    }
    
    private void PlayDamageEffects()
    {
        if (damageSound != null && audioSource != null)
            audioSource.PlayOneShot(damageSound);
        
        if (damageEffectPrefab != null)
            Instantiate(damageEffectPrefab, transform.position, Quaternion.identity);
        
        if (playerRigidbody != null)
            playerRigidbody.linearVelocity = Vector2.zero;
    }
    
    private IEnumerator InvulnerabilityRoutine()
    {
        isInvulnerable = true;
        Debug.Log($"Invulnerability started for {invulnerabilityDuration} seconds");
        
        float elapsedTime = 0f;
        
        while (elapsedTime < invulnerabilityDuration)
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = !spriteRenderer.enabled;
            
            yield return new WaitForSeconds(flashInterval);
            elapsedTime += flashInterval;
        }
        
        if (spriteRenderer != null)
            spriteRenderer.enabled = true;
        
        isInvulnerable = false;
        Debug.Log("Invulnerability ended");
    }
    
    private IEnumerator HealFlashRoutine()
    {
        if (spriteRenderer == null) yield break;
        
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.green;
        yield return new WaitForSeconds(0.2f);
        spriteRenderer.color = originalColor;
    }
    
    private void UpdateHealthUI()
    {
        if (healthText != null)
            healthText.text = $"{currentHealth}/{maxHealth}";
        
        if (healthSlider != null)
            healthSlider.value = HealthPercentage;
        
        if (heartIcons != null && heartIcons.Length > 0)
        {
            for (int i = 0; i < heartIcons.Length; i++)
            {
                if (heartIcons[i] != null)
                {
                    heartIcons[i].enabled = i < currentHealth;
                }
            }
        }
    }
    
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        Debug.Log("Player died!");
        
        if (deathSound != null && audioSource != null)
            audioSource.PlayOneShot(deathSound);
        
        if (playerInput != null)
            playerInput.SetInputEnabled(false);
        
        if (playerCollider != null)
            playerCollider.enabled = false;
        
        if (playerRigidbody != null)
            playerRigidbody.simulated = false;
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            if (gameOverText != null)
                gameOverText.text = "GAME OVER\nВы погибли!";
        }
        
        OnPlayerDeath?.Invoke();
    }
    
    public void RestartGame()
    {
        InitializeHealth();
        UpdateHealthUI();
        
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        
        Time.timeScale = 1f;
    }
    
    public bool IsAlive()
    {
        return currentHealth > 0 && !isDead;
    }
    
    public void ResetForNewLevel()
    {
        currentHealth = maxHealth;
        isDead = false;
        isInvulnerable = false;
        
        if (spriteRenderer != null)
            spriteRenderer.enabled = true;
        
        UpdateHealthUI();
    }
}