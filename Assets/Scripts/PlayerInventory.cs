using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PlayerInventory : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject inventoryPanel;
    
    [Header("Item Slots")]
    [SerializeField] private TextMeshProUGUI helpPotionCountText;
    [SerializeField] private TextMeshProUGUI chancePotionCountText;
    [SerializeField] private TextMeshProUGUI armorCountText;
    
    [Header("Buttons")]
    [SerializeField] private Button useHelpPotionButton;
    [SerializeField] private Button useChancePotionButton;
    [SerializeField] private Button useArmorButton;
    [SerializeField] private Button closeButton;
    
    [Header("Effects")]
    [SerializeField] private AudioClip useItemSound;
    [SerializeField] private GameObject useEffectPrefab;
    [SerializeField] private TextMeshProUGUI feedbackText;
    
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    
    [Header("Display Inventory (UI Text elements)")]
    [SerializeField] private TextMeshProUGUI displayHelpPotion;
    [SerializeField] private TextMeshProUGUI displayChancePotion;
    [SerializeField] private TextMeshProUGUI displayArmor;
    
    // Item counts
    private int helpPotionCount = 0;
    private int chancePotionCount = 0;
    private int armorCount = 0;
    
    // Chance effect
    private bool isChanceActive = false;
    private float chanceEffectEndTime = 0;
    
    // Components
    private AudioSource audioSource;
    
    void Start()
    {
        // Initialize AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        // Hide inventory panel at start
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
        
        // Setup buttons
        SetupButtons();
        
        // Update all UI
        UpdateUI();
        UpdateDisplayInventory();
        
        // Debug log
        Debug.Log("PlayerInventory initialized successfully!");
        Debug.Log($"Display references - Help: {displayHelpPotion != null}, Chance: {displayChancePotion != null}, Armor: {displayArmor != null}");
    }
    
    void SetupButtons()
    {
        if (useHelpPotionButton != null)
        {
            useHelpPotionButton.onClick.RemoveAllListeners();
            useHelpPotionButton.onClick.AddListener(UseHelpPotion);
            Debug.Log("Help potion button listener added");
        }
        else
        {
            Debug.LogWarning("useHelpPotionButton is not assigned in Inspector!");
        }
        
        if (useChancePotionButton != null)
        {
            useChancePotionButton.onClick.RemoveAllListeners();
            useChancePotionButton.onClick.AddListener(UseChancePotion);
            Debug.Log("Chance potion button listener added");
        }
        else
        {
            Debug.LogWarning("useChancePotionButton is not assigned in Inspector!");
        }
        
        if (useArmorButton != null)
        {
            useArmorButton.onClick.RemoveAllListeners();
            useArmorButton.onClick.AddListener(UseArmor);
            Debug.Log("Armor button listener added");
        }
        else
        {
            Debug.LogWarning("useArmorButton is not assigned in Inspector!");
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseInventory);
            Debug.Log("Close button listener added");
        }
        else
        {
            Debug.LogWarning("closeButton is not assigned in Inspector!");
        }
    }
    
    void Update()
    {
        // Open/close inventory
        if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventory();
        }
        
        // Close inventory with Escape
        if (Input.GetKeyDown(KeyCode.Escape) && inventoryPanel != null && inventoryPanel.activeSelf)
        {
            CloseInventory();
        }
        
        // Check if chance effect has expired
        if (isChanceActive && Time.time >= chanceEffectEndTime)
        {
            isChanceActive = false;
            ShowFeedback("Эффект зелья шанса закончился", Color.yellow);
            Debug.Log("Chance effect expired");
        }
        
        // Update display inventory every frame (for real-time updates)
        UpdateDisplayInventory();
    }
    
    void ToggleInventory()
    {
        if (inventoryPanel != null)
        {
            bool isOpen = !inventoryPanel.activeSelf;
            inventoryPanel.SetActive(isOpen);
            Time.timeScale = isOpen ? 0f : 1f;
            Debug.Log($"Inventory toggled: {(isOpen ? "Opened" : "Closed")}");
        }
    }
    
    public void CloseInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
            Time.timeScale = 1f;
            Debug.Log("Inventory closed");
        }
    }
    
    // Add items to inventory
    public void AddHelpPotion(int amount) 
    { 
        helpPotionCount += amount; 
        UpdateUI(); 
        UpdateDisplayInventory();
        ShowFeedback($"Добавлено зелье помощи! Всего: {helpPotionCount}", Color.green);
        Debug.Log($"Help potions added: +{amount}, Total: {helpPotionCount}");
    }
    
    public void AddChancePotion(int amount) 
    { 
        chancePotionCount += amount; 
        UpdateUI(); 
        UpdateDisplayInventory();
        ShowFeedback($"Добавлено зелье шанса! Всего: {chancePotionCount}", Color.green);
        Debug.Log($"Chance potions added: +{amount}, Total: {chancePotionCount}");
    }
    
    public void AddArmor(int amount) 
    { 
        armorCount += amount; 
        UpdateUI(); 
        UpdateDisplayInventory();
        ShowFeedback($"Добавлена броня! Всего: {armorCount}", Color.green);
        Debug.Log($"Armor added: +{amount}, Total: {armorCount}");
    }
    
    // Use help potion - shows hint only
    void UseHelpPotion()
    {
        Debug.Log($"UseHelpPotion called. Current count: {helpPotionCount}");
        
        if (helpPotionCount > 0)
        {
            helpPotionCount--;
            ShowHelpHint();
            UpdateUI();
            UpdateDisplayInventory();
            PlayUseEffect();
            Debug.Log($"Help potion used. Remaining: {helpPotionCount}");
        }
        else
        {
            ShowFeedback("Нет зелий помощи!", Color.red);
            Debug.Log("Cannot use help potion - count is 0");
        }
    }
    
    // Use chance potion
    void UseChancePotion()
    {
        Debug.Log($"UseChancePotion called. Current count: {chancePotionCount}, IsActive: {isChanceActive}");
        
        if (chancePotionCount > 0 && !isChanceActive)
        {
            chancePotionCount--;
            isChanceActive = true;
            chanceEffectEndTime = Time.time + 10f;
            UpdateUI();
            UpdateDisplayInventory();
            PlayUseEffect();
            ShowFeedback("Зелье шанса активировано! 50% шанс избежать урона на 10 секунд", Color.cyan);
            Debug.Log($"Chance potion used. Remaining: {chancePotionCount}, Effect ends at: {chanceEffectEndTime}");
        }
        else if (chancePotionCount <= 0)
        {
            ShowFeedback("Нет зелий шанса!", Color.red);
            Debug.Log("Cannot use chance potion - count is 0");
        }
        else if (isChanceActive)
        {
            ShowFeedback("Эффект шанса уже активен!", Color.yellow);
            Debug.Log("Cannot use chance potion - effect already active");
        }
    }
    
    // Use armor
    void UseArmor()
    {
        Debug.Log($"UseArmor called. Current count: {armorCount}");
        
        if (armorCount > 0 && playerHealth != null)
        {
            armorCount--;
            playerHealth.AddExtraLife();
            UpdateUI();
            UpdateDisplayInventory();
            PlayUseEffect();
            ShowFeedback("Броня использована! +1 к максимальному здоровью", Color.green);
            Debug.Log($"Armor used. Remaining: {armorCount}");
        }
        else if (armorCount <= 0)
        {
            ShowFeedback("Нет брони!", Color.red);
            Debug.Log("Cannot use armor - count is 0");
        }
        else if (playerHealth == null)
        {
            Debug.LogError("PlayerHealth reference is missing! Cannot use armor.");
        }
    }
    
    // Check if damage can be avoided (called from PlayerHealth)
    public bool TryAvoidDamage()
    {
        if (isChanceActive)
        {
            bool avoid = Random.Range(0f, 1f) <= 0.5f;
            if (avoid)
            {
                ShowFeedback("Зелье шанса сработало! Урон избегнут!", Color.green);
                Debug.Log("Damage avoided by chance potion!");
                return true;
            }
            else
            {
                Debug.Log("Chance potion failed to avoid damage (50% chance)");
            }
        }
        return false;
    }
    
    // Show help hint
    void ShowHelpHint()
    {
        string[] hints = new string[]
        {
            "Подсказка: Осмотритесь вокруг внимательнее!",
            "Подсказка: Попробуйте взаимодействовать с объектами",
            "Подсказка: Иногда ответ лежит на поверхности",
            "Подсказка: Не бойтесь экспериментировать!",
            "Подсказка: Проверьте все углы комнаты",
            "Подсказка: Возможно, нужно нажать на что-то"
        };
        
        string randomHint = hints[Random.Range(0, hints.Length)];
        ShowFeedback(randomHint, Color.yellow);
        Debug.Log($"Help hint shown: {randomHint}");
    }
    
    // Play use effect (sound and visual)
    void PlayUseEffect()
    {
        if (useItemSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(useItemSound);
            Debug.Log("Use sound played");
        }
        
        if (useEffectPrefab != null && playerHealth != null)
        {
            Instantiate(useEffectPrefab, playerHealth.transform.position, Quaternion.identity);
            Debug.Log("Use effect instantiated");
        }
    }
    
    // Show feedback message
    void ShowFeedback(string message, Color color)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.color = color;
            StartCoroutine(ClearFeedback());
            Debug.Log($"Feedback shown: {message}");
        }
        else
        {
            Debug.Log($"Feedback (no UI): {message}");
        }
    }
    
    IEnumerator ClearFeedback()
    {
        yield return new WaitForSeconds(2f);
        if (feedbackText != null)
            feedbackText.text = "";
    }
    
    // Update inventory panel UI
    void UpdateUI()
    {
        if (helpPotionCountText != null)
        {
            helpPotionCountText.text = helpPotionCount.ToString();
            helpPotionCountText.color = helpPotionCount > 0 ? Color.white : Color.gray;
        }
        
        if (chancePotionCountText != null)
        {
            chancePotionCountText.text = chancePotionCount.ToString();
            chancePotionCountText.color = chancePotionCount > 0 ? Color.white : Color.gray;
        }
        
        if (armorCountText != null)
        {
            armorCountText.text = armorCount.ToString();
            armorCountText.color = armorCount > 0 ? Color.white : Color.gray;
        }
        
        // Update button interactability
        if (useHelpPotionButton != null)
            useHelpPotionButton.interactable = helpPotionCount > 0;
        
        if (useChancePotionButton != null)
            useChancePotionButton.interactable = chancePotionCount > 0 && !isChanceActive;
        
        if (useArmorButton != null)
            useArmorButton.interactable = armorCount > 0;
    }
    
    // Update display inventory (always visible UI)
    void UpdateDisplayInventory()
    {
        if (displayHelpPotion != null)
        {
            displayHelpPotion.text = $"x{helpPotionCount}";
            displayHelpPotion.color = helpPotionCount > 0 ? Color.white : Color.gray;
        }
        
        if (displayChancePotion != null)
        {
            displayChancePotion.text = $"x{chancePotionCount}";
            displayChancePotion.color = chancePotionCount > 0 ? Color.white : Color.gray;
        }
        
        if (displayArmor != null)
        {
            displayArmor.text = $"x{armorCount}";
            displayArmor.color = armorCount > 0 ? Color.white : Color.gray;
        }
    }
    
    // Public getters for debugging
    public int GetHelpPotionCount() => helpPotionCount;
    public int GetChancePotionCount() => chancePotionCount;
    public int GetArmorCount() => armorCount;
    public bool IsChanceActive() => isChanceActive;
}