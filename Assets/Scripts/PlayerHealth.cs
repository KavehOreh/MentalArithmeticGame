using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

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
    
    [Header("Shop UI Elements")]
    [SerializeField] private GameObject shopPanel; // Панель магазина
    [SerializeField] private TextMeshProUGUI shopMessageText; // Текст с предложением
    [SerializeField] private Button yesButton; // Кнопка "Да"
    [SerializeField] private Button noButton; // Кнопка "Нет"
    
    [Header("Shop Settings")]
    [SerializeField] private int victoryPointsCost = 1; // Стоимость одной жизни в победах
    
    [Header("Effects")]
    [SerializeField] private GameObject damageEffectPrefab;
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip healSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip buySound; // Звук покупки
    [SerializeField] private AudioClip errorSound; // Звук ошибки (не хватает побед)
    
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
    private DoorController doorController; // Ссылка на DoorController для доступа к victoryPoints
    
    // State
    private bool isInvulnerable = false;
    private bool isDead = false;
    private GameObject currentHPObject; // Текущий объект с тегом HP
    private bool isShopOpen = false; // Открыт ли магазин
    private bool canInteractWithHP = true; // Можно ли взаимодействовать с HP объектами
    
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
        doorController = GetComponent<DoorController>(); // Получаем ссылку на DoorController
        
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
            
        // Скрываем панель магазина при старте
        if (shopPanel != null)
            shopPanel.SetActive(false);
            
        // Настраиваем кнопки магазина
        SetupShopButtons();
    }
    
    void Update()
    {
        // Дополнительная логика если нужна
    }
    
    /// <summary>
    /// Обработка столкновения с объектом
    /// </summary>
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Проверяем, есть ли тег HP и можно ли взаимодействовать
        if (collision.gameObject.CompareTag("HP") && canInteractWithHP && !isShopOpen && !isDead)
        {
            Debug.Log($"Collided with HP object: {collision.gameObject.name}");
            currentHPObject = collision.gameObject;
            OpenShop();
        }
    }
    
    /// <summary>
    /// Обработка триггерного столкновения (если используете IsTrigger)
    /// </summary>
    void OnTriggerEnter2D(Collider2D other)
    {
        // Проверяем, есть ли тег HP и можно ли взаимодействовать
        if (other.CompareTag("HP") && canInteractWithHP && !isShopOpen && !isDead)
        {
            Debug.Log($"Trigger entered with HP object: {other.gameObject.name}");
            currentHPObject = other.gameObject;
            OpenShop();
        }
    }
    
    void SetupShopButtons()
    {
        if (yesButton != null)
        {
            yesButton.onClick.AddListener(OnYesButtonClicked);
        }
        
        if (noButton != null)
        {
            noButton.onClick.AddListener(OnNoButtonClicked);
        }
    }
    
    void OpenShop()
    {
        if (shopPanel == null || currentHPObject == null) return;
        
        Debug.Log("Opening health shop");
        
        isShopOpen = true;
        canInteractWithHP = false; // Запрещаем новые взаимодействия
        
        // Показываем панель магазина
        shopPanel.SetActive(true);
        
        // Обновляем текст с предложением
        if (shopMessageText != null)
        {
            int currentVictoryPoints = doorController != null ? doorController.victoryPoints : 0;
            shopMessageText.text = $"Хотите купить 1 жизнь за {victoryPointsCost} очко победы?\n\nВаши очки: {currentVictoryPoints}";
        }
        
        // Блокируем управление игроком
        Time.timeScale = 0f;
        DisablePlayerInput();
    }
    
    void OnYesButtonClicked()
    {
        Debug.Log("Yes button clicked - attempting to buy health");
        
        if (doorController == null)
        {
            Debug.LogError("DoorController not found!");
            CloseShop();
            return;
        }
        
        // Проверяем, хватает ли очков побед
        if (doorController.victoryPoints >= victoryPointsCost)
        {
            // Проверяем, не полное ли здоровье
            if (currentHealth < maxHealth)
            {
                // Совершаем покупку
                doorController.victoryPoints -= victoryPointsCost;
                doorController.UpdateVictoryPointsUI();
                
                // Добавляем одну жизнь
                Heal(1);
                
                // Звук покупки
                if (buySound != null && audioSource != null)
                    audioSource.PlayOneShot(buySound);
                
                // Уничтожаем объект HP (одноразовый магазин)
                if (currentHPObject != null)
                {
                    Destroy(currentHPObject);
                    currentHPObject = null;
                }
                
                Debug.Log($"Health purchased! Remaining victory points: {doorController.victoryPoints}");
                
                // Показываем сообщение об успешной покупке
                StartCoroutine(ShowPurchaseMessage("Покупка успешна! +1 жизнь", Color.green));
                
                // Закрываем магазин после успешной покупки
                CloseShop();
            }
            else
            {
                // Здоровье полное
                Debug.Log("Health is already full!");
                if (errorSound != null && audioSource != null)
                    audioSource.PlayOneShot(errorSound);
                StartCoroutine(ShowPurchaseMessage("У вас уже полное здоровье!", Color.yellow));
            }
        }
        else
        {
            // Не хватает очков
            Debug.Log($"Not enough victory points! Need {victoryPointsCost}, have {doorController.victoryPoints}");
            if (errorSound != null && audioSource != null)
                audioSource.PlayOneShot(errorSound);
            StartCoroutine(ShowPurchaseMessage($"Не хватает очков! Нужно {victoryPointsCost}", Color.red));
        }
    }
    
    IEnumerator ShowPurchaseMessage(string message, Color color)
    {
        // Показываем сообщение на панели магазина
        if (shopMessageText != null)
        {
            Color originalColor = shopMessageText.color;
            shopMessageText.text = message;
            shopMessageText.color = color;
            
            yield return new WaitForSecondsRealtime(1.5f);
            
            // Возвращаем исходный текст, если объект HP еще существует
            if (currentHPObject != null)
            {
                int currentVictoryPoints = doorController != null ? doorController.victoryPoints : 0;
                shopMessageText.text = $"Хотите купить 1 жизнь за {victoryPointsCost} очко победы?\n\nВаши очки: {currentVictoryPoints}";
                shopMessageText.color = originalColor;
            }
        }
    }
    
    void OnNoButtonClicked()
    {
        Debug.Log("No button clicked - closing shop");
        CloseShop();
    }
    
    void CloseShop()
    {
        isShopOpen = false;
        
        // Возвращаем возможность взаимодействия только если объект HP еще существует
        canInteractWithHP = currentHPObject != null;
        
        if (shopPanel != null)
            shopPanel.SetActive(false);
        
        // Возвращаем управление игроку
        Time.timeScale = 1f;
        EnablePlayerInput();
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
        // Проверки на возможность получения урона
        if (isInvulnerable || isDead || currentHealth <= 0)
        {
            Debug.Log($"Damage blocked: isInvulnerable={isInvulnerable}, isDead={isDead}, currentHealth={currentHealth}");
            return;
        }
        
        // Наносим урон
        int previousHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - damageAmount);
        
        Debug.Log($"Health changed: {previousHealth} -> {currentHealth}");
        
        // Визуальные и звуковые эффекты
        PlayDamageEffects();
        
        // Запускаем неуязвимость только если здоровье > 0
        if (currentHealth > 0)
        {
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
        Debug.Log($"Invulnerability started for {invulnerabilityDuration} seconds");
        
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
        canInteractWithHP = true; // Сбрасываем флаг взаимодействия
        
        if (spriteRenderer != null)
            spriteRenderer.enabled = true;
            
        UpdateHealthUI();
    }
    
    void DisablePlayerInput()
    {
        if (playerInput != null)
            playerInput.SetInputEnabled(false);
    }
    
    void EnablePlayerInput()
    {
        if (playerInput != null)
            playerInput.SetInputEnabled(true);
    }
}