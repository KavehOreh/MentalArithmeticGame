using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

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
        // Получаем компоненты
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        playerInput = GetComponent<PlayerInputDemo>();
        playerCollider = GetComponent<Collider2D>();
        playerRigidbody = GetComponent<Rigidbody2D>();
        
        // Если AudioSource нет, добавляем
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
    
    /// <summary>
    /// Инициализация здоровья при старте или рестарте
    /// </summary>
    public void InitializeHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        isInvulnerable = false;
        
        // Включаем компоненты
        if (playerCollider != null)
            playerCollider.enabled = true;
            
        if (playerRigidbody != null)
            playerRigidbody.simulated = true;
            
        if (playerInput != null)
            playerInput.SetInputEnabled(true);
        
        OnHealthChanged?.Invoke();
    }
    
    /// <summary>
    /// Получение урона
    /// </summary>
    public void TakeDamage(int damageAmount)
    {
        // Подробная отладка
        Debug.Log($"TakeDamage called: damageAmount={damageAmount}, isInvulnerable={isInvulnerable}, isDead={isDead}, currentHealth={currentHealth}");
    
        
        // Проверки на возможность получения урона
        if (isInvulnerable)
        {
            Debug.Log("Damage blocked - player is invulnerable");
            return;
        }
        
        if (isDead)
        {
            Debug.Log("Damage blocked - player is dead");
            return;
        }
        
        if (currentHealth <= 0)
        {
            Debug.Log("Damage blocked - player already has 0 health");
            return;
        }
        
        // Наносим урон
        int previousHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - damageAmount);
        
        Debug.Log($"Health changed: {previousHealth} -> {currentHealth}");
        
        // Визуальные и звуковые эффекты
        PlayDamageEffects();
        
        // Запускаем неуязвимость ТОЛЬКО если здоровье > 0
        if (currentHealth > 0)
        {
            Debug.Log("Starting invulnerability");
            StartCoroutine(InvulnerabilityRoutine());
        }
        
        // Обновляем UI
        UpdateHealthUI();
        
        // Вызываем события
        OnHealthChanged?.Invoke();
        OnPlayerDamaged?.Invoke();
        
        // Проверка на смерть
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// Лечение игрока
    /// </summary>
    public void Heal(int healAmount)
    {
        if (isDead) return;
        
        int previousHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
        
        if (currentHealth > previousHealth)
        {
            Debug.Log($"Healed: {previousHealth} -> {currentHealth}");
            
            // Эффект лечения
            if (healSound != null && audioSource != null)
                audioSource.PlayOneShot(healSound);
            
            // Визуальный эффект (зеленая вспышка)
            StartCoroutine(HealFlashRoutine());
            
            UpdateHealthUI();
            OnHealthChanged?.Invoke();
            OnPlayerHealed?.Invoke();
        }
    }
    
    /// <summary>
    /// Полное восстановление здоровья
    /// </summary>
    public void FullHeal()
    {
        if (isDead) return;
        
        currentHealth = maxHealth;
        UpdateHealthUI();
        OnHealthChanged?.Invoke();
        OnPlayerHealed?.Invoke();
        
        // Эффект полного исцеления
        if (healSound != null && audioSource != null)
            audioSource.PlayOneShot(healSound);
    }
    
    /// <summary>
    /// Добавление максимального здоровья
    /// </summary>
    public void IncreaseMaxHealth(int amount)
    {
        maxHealth += amount;
        currentHealth = maxHealth; // Полностью восстанавливаем здоровье
        UpdateHealthUI();
        OnHealthChanged?.Invoke();
    }
    
    /// <summary>
    /// Воспроизведение эффектов получения урона
    /// </summary>
    private void PlayDamageEffects()
    {
        // Звук урона
        if (damageSound != null && audioSource != null)
            audioSource.PlayOneShot(damageSound);
        
        // Префаб эффекта
        if (damageEffectPrefab != null)
            Instantiate(damageEffectPrefab, transform.position, Quaternion.identity);
        
        // Кратковременная остановка движения
        if (playerRigidbody != null)
            playerRigidbody.linearVelocity = Vector2.zero;
    }
    
    /// <summary>
    /// Корутина неуязвимости с миганием
    /// </summary>
    private IEnumerator InvulnerabilityRoutine()
    {
        isInvulnerable = true;
        Debug.Log($"Invulnerability started. Duration: {invulnerabilityDuration}");
        
        float elapsedTime = 0f;
        
        while (elapsedTime < invulnerabilityDuration)
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = !spriteRenderer.enabled;
            
            yield return new WaitForSeconds(flashInterval);
            elapsedTime += flashInterval;
        }
        
        // Восстанавливаем нормальное состояние
        if (spriteRenderer != null)
            spriteRenderer.enabled = true;
        
        isInvulnerable = false;
        Debug.Log("Invulnerability ended");
    }
    
    /// <summary>
    /// Корунтина эффекта лечения
    /// </summary>
    private IEnumerator HealFlashRoutine()
    {
        if (spriteRenderer == null) yield break;
        
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.green;
        yield return new WaitForSeconds(0.2f);
        spriteRenderer.color = originalColor;
    }
    
    /// <summary>
    /// Обновление всех UI элементов здоровья
    /// </summary>
    private void UpdateHealthUI()
    {
        // Текстовый вариант
        if (healthText != null)
            healthText.text = $"{currentHealth}/{maxHealth}";
        
        // Вариант со слайдером
        if (healthSlider != null)
            healthSlider.value = HealthPercentage;
        
        // Вариант с иконками сердец
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
    
    /// <summary>
    /// Смерть игрока
    /// </summary>
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        Debug.Log("Player died!");
        
        // Звук смерти
        if (deathSound != null && audioSource != null)
            audioSource.PlayOneShot(deathSound);
        
        // Отключаем управление
        if (playerInput != null)
            playerInput.SetInputEnabled(false);
        
        // Отключаем физику и коллизии
        if (playerCollider != null)
            playerCollider.enabled = false;
            
        if (playerRigidbody != null)
            playerRigidbody.simulated = false;
        
        // Показываем панель Game Over
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            if (gameOverText != null)
                gameOverText.text = "GAME OVER\nВы погибли!";
        }
        
        // Вызываем событие смерти
        OnPlayerDeath?.Invoke();
    }
    
    /// <summary>
    /// Рестарт игры
    /// </summary>
    public void RestartGame()
    {
        InitializeHealth();
        UpdateHealthUI();
        
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        
        // Возвращаем время
        Time.timeScale = 1f;
    }
    
    /// <summary>
    /// Проверка, жив ли игрок
    /// </summary>
    public bool IsAlive()
    {
        return currentHealth > 0 && !isDead;
    }
    
    /// <summary>
    /// Сброс состояния при рестарте уровня
    /// </summary>
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
    // Этот метод дублирует EnablePlayerControl из DoorController, можно удалить
    // private void EnablePlayerControl(bool enable)
    // {
    //     PlayerInputDemo playerInput = GetComponent<PlayerInputDemo>();
    //     if (playerInput != null)
    //     {
    //         playerInput.SetInputEnabled(enable);
    //     }
    // }


    // Добавьте этот метод временно для т