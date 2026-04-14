using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ShopInteraction : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private TextMeshProUGUI dialogMessageText;
    [SerializeField] private TextMeshProUGUI dialogPriceText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    
    [Header("Player References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private DoorController doorController;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerInputDemo playerInput;
    
    [Header("HP Shop Settings")]
    [SerializeField] private int hpPrice = 1;
    
    [Header("Audio")]
    [SerializeField] private AudioClip buySound;
    [SerializeField] private AudioClip errorSound;
    
    // Private variables
    private GameObject currentItemObject;
    private string currentItemTag;
    private int currentItemPrice;
    private bool isDialogOpen = false;
    private bool isHPObject = false;
    private AudioSource audioSource;
    private Color originalMessageColor;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        if (dialogPanel != null)
            dialogPanel.SetActive(false);
        
        if (dialogMessageText != null)
        {
            originalMessageColor = dialogMessageText.color;
        }
        
        SetupButtons();
        
        if (playerInput == null)
            playerInput = GetComponent<PlayerInputDemo>();
        
        // Debug checks
        if (playerInventory == null)
        {
            Debug.LogError("PlayerInventory reference is MISSING in ItemInteraction! Please assign it in the Inspector.");
        }
        
        if (doorController == null)
        {
            Debug.LogError("DoorController reference is MISSING in ItemInteraction! Please assign it in the Inspector.");
        }
        
        if (playerHealth == null)
        {
            Debug.LogError("PlayerHealth reference is MISSING in ItemInteraction! Please assign it in the Inspector.");
        }
    }
    
    void SetupButtons()
    {
        if (yesButton != null)
            yesButton.onClick.AddListener(OnYesButtonClicked);
        
        if (noButton != null)
            noButton.onClick.AddListener(OnNoButtonClicked);
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        CheckItemInteraction(collision.gameObject);
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        CheckItemInteraction(other.gameObject);
    }
    
    void CheckItemInteraction(GameObject obj)
    {
        if (isDialogOpen) return;
        
        if (obj.CompareTag("ChancePotion") || 
            obj.CompareTag("HelpPotion") || 
            obj.CompareTag("Armor") ||
            obj.CompareTag("HP"))
        {
            currentItemObject = obj;
            currentItemTag = obj.tag;
            isHPObject = (currentItemTag == "HP");
            
            // Для HP объектов проверяем, не полное ли здоровье
            if (isHPObject && playerHealth != null && playerHealth.CurrentHealth >= playerHealth.MaxHealth)
            {
                // Здоровье полное - показываем сообщение и не открываем диалог
                Debug.Log("Cannot buy HP - health is already full!");
                StartCoroutine(ShowFloatingMessage("У вас уже полное здоровье!", Color.yellow, 1.5f));
                return;
            }
            
            OpenDialog();
        }
    }
    
    void OpenDialog()
    {
        if (dialogPanel == null || currentItemObject == null || isDialogOpen) return;
        
        // Устанавливаем цену в зависимости от тега
        if (isHPObject)
        {
            currentItemPrice = hpPrice;
        }
        else
        {
            switch (currentItemTag)
            {
                case "ChancePotion":
                    currentItemPrice = 5;
                    break;
                case "HelpPotion":
                    currentItemPrice = 3;
                    break;
                case "Armor":
                    currentItemPrice = 8;
                    break;
                default:
                    return;
            }
        }
        
        // Обновляем текст диалога
        if (dialogMessageText != null)
        {
            string itemName = GetItemName(currentItemTag);
            string itemEffect = GetItemEffect(currentItemTag);
            
            if (isHPObject)
            {
                dialogMessageText.text = $"Купить 1 жизнь?\n\n<size=80%>Восстанавливает 1 единицу здоровья</size>";
            }
            else
            {
                dialogMessageText.text = $"Купить {itemName}?\n\n<size=80%>{itemEffect}</size>";
            }
            dialogMessageText.color = originalMessageColor;
        }
        
        if (dialogPriceText != null)
        {
            int currentPoints = doorController != null ? doorController.victoryPoints : 0;
            dialogPriceText.text = $"Цена: {currentItemPrice} очков\nВаши очки: {currentPoints}";
        }
        
        dialogPanel.SetActive(true);
        isDialogOpen = true;
        Time.timeScale = 0f;
        DisablePlayerInput();
    }
    
    void OnYesButtonClicked()
    {
        Debug.Log($"Yes button clicked! Item: {currentItemTag}, Price: {currentItemPrice}");
        
        if (doorController == null)
        {
            Debug.LogError("DoorController is null!");
            CloseDialog();
            return;
        }
        
        if (currentItemObject == null)
        {
            Debug.LogError("currentItemObject is null!");
            CloseDialog();
            return;
        }
        
        // Дополнительная проверка для HP объектов перед покупкой
        if (isHPObject && playerHealth != null && playerHealth.CurrentHealth >= playerHealth.MaxHealth)
        {
            Debug.Log("Cannot buy HP - health is already full!");
            StartCoroutine(ShowTemporaryMessage("У вас уже полное здоровье!", Color.yellow));
            CloseDialog();
            return;
        }
        
        if (doorController.victoryPoints >= currentItemPrice)
        {
            // Списываем очки
            doorController.victoryPoints -= currentItemPrice;
            doorController.UpdateVictoryPointsUI();
            
            if (isHPObject)
            {
                if (playerHealth != null)
                {
                    playerHealth.Heal(1);
                    Debug.Log("Health restored by HP object!");
                    StartCoroutine(ShowTemporaryMessage("+1 жизнь!", Color.green));
                }
                else
                {
                    Debug.LogError("PlayerHealth is null! Cannot heal!");
                }
            }
            else
            {
                AddItemToInventory(currentItemTag);
                StartCoroutine(ShowTemporaryMessage($"Куплено! Предмет добавлен в инвентарь", Color.green));
            }
            
            if (buySound != null && audioSource != null)
                audioSource.PlayOneShot(buySound);
            
            Destroy(currentItemObject);
            currentItemObject = null;
            CloseDialog();
        }
        else
        {
            Debug.Log($"Not enough points! Have: {doorController.victoryPoints}, Need: {currentItemPrice}");
            if (errorSound != null && audioSource != null)
                audioSource.PlayOneShot(errorSound);
            StartCoroutine(ShowTemporaryMessage($"Не хватает очков! Нужно {currentItemPrice}", Color.red));
        }
    }
    
    void AddItemToInventory(string tag)
    {
        Debug.Log($"AddItemToInventory called for tag: {tag}");
        
        if (playerInventory == null)
        {
            Debug.LogError("PlayerInventory is NULL! Cannot add item!");
            return;
        }
        
        switch (tag)
        {
            case "HelpPotion":
                playerInventory.AddHelpPotion(1);
                Debug.Log("HelpPotion added to inventory!");
                break;
                
            case "ChancePotion":
                playerInventory.AddChancePotion(1);
                Debug.Log("ChancePotion added to inventory!");
                break;
                
            case "Armor":
                playerInventory.AddArmor(1);
                Debug.Log("Armor added to inventory!");
                break;
                
            default:
                Debug.LogWarning($"Unknown tag: {tag}");
                break;
        }
    }
    
    void OnNoButtonClicked()
    {
        CloseDialog();
    }
    
    void CloseDialog()
    {
        if (dialogPanel != null)
            dialogPanel.SetActive(false);
        
        isDialogOpen = false;
        isHPObject = false;
        
        if (dialogMessageText != null)
        {
            dialogMessageText.color = originalMessageColor;
        }
        
        Time.timeScale = 1f;
        EnablePlayerInput();
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
    
    string GetItemName(string tag)
    {
        switch (tag)
        {
            case "ChancePotion": return "Зелье Шанса";
            case "HelpPotion": return "Зелье Помощи";
            case "Armor": return "Броню";
            case "HP": return "Жизнь";
            default: return "Предмет";
        }
    }
    
    string GetItemEffect(string tag)
    {
        switch (tag)
        {
            case "ChancePotion": return "Эффект: Дает 50% шанс избежать урона на 10 секунд";
            case "HelpPotion": return "Эффект: Помогает в решении задач";
            case "Armor": return "Эффект: Увеличивает максимальное здоровье на 1";
            case "HP": return "Эффект: Восстанавливает 1 жизнь";
            default: return "";
        }
    }
    
    IEnumerator ShowTemporaryMessage(string message, Color color)
    {
        if (dialogMessageText == null) yield break;
        
        string currentMessage = dialogMessageText.text;
        string currentPrice = dialogPriceText != null ? dialogPriceText.text : "";
        Color currentColor = dialogMessageText.color;
        
        dialogMessageText.text = message;
        dialogMessageText.color = color;
        
        if (dialogPriceText != null)
            dialogPriceText.text = "";
        
        yield return new WaitForSecondsRealtime(1.5f);
        
        if (isDialogOpen && currentItemObject != null)
        {
            dialogMessageText.text = currentMessage;
            dialogMessageText.color = currentColor;
            
            if (dialogPriceText != null && doorController != null)
            {
                int currentPoints = doorController.victoryPoints;
                dialogPriceText.text = $"Цена: {currentItemPrice} очков\nВаши очки: {currentPoints}";
            }
        }
        else
        {
            dialogMessageText.color = originalMessageColor;
        }
    }
    
    IEnumerator ShowFloatingMessage(string message, Color color, float duration)
    {
        // Показываем временное сообщение без открытия диалога
        if (dialogMessageText != null)
        {
            string originalText = dialogMessageText.text;
            Color originalColor = dialogMessageText.color;
            
            dialogMessageText.text = message;
            dialogMessageText.color = color;
            
            // Временно показываем панель если она скрыта
            bool wasPanelActive = dialogPanel.activeSelf;
            if (!wasPanelActive)
                dialogPanel.SetActive(true);
            
            yield return new WaitForSecondsRealtime(duration);
            
            dialogMessageText.text = originalText;
            dialogMessageText.color = originalColor;
            
            if (!wasPanelActive)
                dialogPanel.SetActive(false);
        }
    }
    
    void OnDestroy()
    {
        if (yesButton != null)
            yesButton.onClick.RemoveListener(OnYesButtonClicked);
        
        if (noButton != null)
            noButton.onClick.RemoveListener(OnNoButtonClicked);
        
        if (isDialogOpen)
            Time.timeScale = 1f;
    }
}