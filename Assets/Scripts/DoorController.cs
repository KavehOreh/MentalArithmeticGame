using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class DoorController : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject mathDialogPanel;
    public TextMeshProUGUI mathProblemText;
    public TMP_InputField answerInputField;
    public TextMeshProUGUI resultMessageText;
    public TextMeshProUGUI timerText;
    public Button submitButton;
    public Button closeButton;
    
    [Header("Player Stats")]
    public PlayerHealth playerHealth;
    public int victoryPoints = 0;
    public TextMeshProUGUI victoryPointsText;
    
    [Header("Interaction Settings")]
    public float interactionRadius = 2f;
    public KeyCode interactionKey = KeyCode.E;
    
    [Header("Combat Settings")]
    public float timePerProblem = 10f;
    public float combatRadius = 5f; // Радиус для поиска врагов в бою
    
    // Для дверей
    private GameObject currentDoor;
    
    // Для врагов
    private List<GameObject> enemiesInCombat = new List<GameObject>();
    private int enemiesToDefeat = 0; // Сколько врагов нужно победить
    private int enemiesDefeated = 0; // Сколько уже победили
    
    private int correctAnswer;
    private bool isDialogOpen = false;
    private float currentTime;
    private bool isTimerRunning = false;
    private bool isInCombat = false;
    private bool isProcessingTimeout = false;
    
    void Start()
    {
        Debug.Log("=== DoorController Start ===");
        
        if (mathDialogPanel != null)
            mathDialogPanel.SetActive(false);
            
        if (submitButton != null)
            submitButton.onClick.AddListener(CheckAnswer);
            
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseDialog);
            
        if (timerText != null)
            timerText.gameObject.SetActive(false);
            
        UpdateVictoryPointsUI();
    }
    
    void Update()
    {
        // Поиск интерактивных объектов (двери и враги)
        if (!isDialogOpen)
        {
            FindInteractableObjects();
        }
        
        // Нажатие E для взаимодействия
        if (!isDialogOpen && Input.GetKeyDown(interactionKey))
        {
            TryInteract();
        }
        
        // Таймер для режима боя
        if (isTimerRunning && !isProcessingTimeout)
        {
            currentTime -= Time.unscaledDeltaTime;
            
            if (timerText != null)
            {
                timerText.text = $"Время: {Mathf.Max(0, currentTime):F1}";
                timerText.color = currentTime < 3f ? Color.red : Color.white;
            }
            
            if (currentTime <= 0)
            {
                StartCoroutine(HandleTimeout());
            }
        }
    }
    
    void FindInteractableObjects()
    {
        // Очищаем предыдущие данные
        currentDoor = null;
        enemiesInCombat.Clear();
        
        // Ищем все объекты в радиусе
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, interactionRadius);
        
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Door"))
            {
                currentDoor = hitCollider.gameObject;
                Debug.Log($"Door found: {currentDoor.name}");
            }
        }
    }
    
    void FindEnemiesForCombat(Vector2 center)
    {
        // Ищем врагов в радиусе боя
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(center, combatRadius);
        enemiesInCombat.Clear();
        
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Enemy"))
            {
                enemiesInCombat.Add(hitCollider.gameObject);
                Debug.Log($"Enemy found for combat: {hitCollider.gameObject.name}");
            }
        }
    }
    
    void TryInteract()
    {
        // Сначала проверяем двери
        if (currentDoor != null)
        {
            Debug.Log("Interacting with door");
            OpenDoorDialog();
            return;
        }
        
        // Затем ищем врагов в радиусе боя
        FindEnemiesForCombat(transform.position);
        
        if (enemiesInCombat.Count > 0)
        {
            Debug.Log($"Found {enemiesInCombat.Count} enemies in combat radius. Starting combat...");
            StartCombat();
            return;
        }
        
        Debug.Log("No interactable objects nearby");
    }
    
    void OpenDoorDialog()
    {
        isInCombat = false;
        isDialogOpen = true;
        isTimerRunning = false;
        
        // Скрываем таймер для двери
        if (timerText != null)
            timerText.gameObject.SetActive(false);
        
        // Генерируем пример
        GenerateMathProblem();
        
        // Показываем диалог
        if (mathDialogPanel != null)
            mathDialogPanel.SetActive(true);
        
        if (resultMessageText != null)
            resultMessageText.text = "Решите пример, чтобы открыть дверь";
        
        // Блокируем управление
        Time.timeScale = 0f;
        DisablePlayerInput();
    }
    
    void StartCombat()
    {
        // Сохраняем количество врагов, которых нужно победить
        enemiesToDefeat = enemiesInCombat.Count;
        enemiesDefeated = 0;
        
        Debug.Log($"Starting combat. Need to defeat {enemiesToDefeat} enemies");
        
        isInCombat = true;
        isDialogOpen = true;
        isProcessingTimeout = false;
        
        // Показываем UI
        if (mathDialogPanel != null)
            mathDialogPanel.SetActive(true);
        
        if (timerText != null)
            timerText.gameObject.SetActive(true);
        
        // Генерируем первый пример
        GenerateMathProblem();
        
        // Запускаем таймер
        ResetTimer();
        
        UpdateCombatMessage();
        
        // Блокируем управление
        Time.timeScale = 0f;
        DisablePlayerInput();
    }
    
    void UpdateCombatMessage()
    {
        if (resultMessageText != null)
        {
            int remaining = enemiesToDefeat - enemiesDefeated;
            resultMessageText.text = $"Осталось врагов: {remaining}";
        }
    }
    
    void GenerateMathProblem()
    {
        System.Random random = new System.Random();
        int problemType = random.Next(0, 4);
        
        int num1, num2;
        string problemText = "";
        
        switch (problemType)
        {
            case 0:
                num1 = random.Next(1, 21);
                num2 = random.Next(1, 21);
                correctAnswer = num1 + num2;
                problemText = $"{num1} + {num2} = ?";
                break;
            case 1:
                num1 = random.Next(10, 31);
                num2 = random.Next(1, num1);
                correctAnswer = num1 - num2;
                problemText = $"{num1} - {num2} = ?";
                break;
            case 2:
                num1 = random.Next(1, 11);
                num2 = random.Next(1, 11);
                correctAnswer = num1 * num2;
                problemText = $"{num1} × {num2} = ?";
                break;
            case 3:
                num2 = random.Next(2, 11);
                correctAnswer = random.Next(1, 11);
                num1 = num2 * correctAnswer;
                problemText = $"{num1} ÷ {num2} = ?";
                break;
        }
        
        if (mathProblemText != null)
            mathProblemText.text = problemText;
            
        Debug.Log($"Generated problem {enemiesDefeated + 1}/{enemiesToDefeat}: {problemText} = {correctAnswer}");
    }
    
    void ResetTimer()
    {
        currentTime = timePerProblem;
        isTimerRunning = true;
        Debug.Log($"Timer reset to {currentTime}");
    }
    
    void CheckAnswer()
    {
        if (answerInputField == null) return;
        
        string userAnswer = answerInputField.text.Trim();
        
        if (string.IsNullOrEmpty(userAnswer))
        {
            ShowMessage("Введите ответ!", Color.yellow);
            return;
        }
        
        if (int.TryParse(userAnswer, out int answer))
        {
            Debug.Log($"User answer: {answer}, Correct: {correctAnswer}");
            
            if (answer == correctAnswer)
            {
                HandleCorrectAnswer();
            }
            else
            {
                HandleWrongAnswer();
            }
            
            answerInputField.text = "";
            answerInputField.ActivateInputField();
        }
        else
        {
            ShowMessage("Введите число!", Color.yellow);
        }
    }
    
    void HandleCorrectAnswer()
    {
        Debug.Log("Correct answer!");
        ShowMessage("Правильно!", Color.green);
        
        if (!isInCombat) // Режим двери
        {
            if (currentDoor != null)
            {
                Debug.Log($"Destroying door: {currentDoor.name}");
                Destroy(currentDoor);
                currentDoor = null;
                ShowMessage("Дверь открыта!", Color.green);
                Invoke("CloseDialog", 1.5f);
            }
        }
        else // Режим боя
        {
            // Увеличиваем счетчик побежденных врагов
            enemiesDefeated++;
            
            // Убиваем одного врага (первого в списке)
            if (enemiesInCombat.Count > 0)
            {
                GameObject enemy = enemiesInCombat[0];
                if (enemy != null)
                {
                    Debug.Log($"Killing enemy: {enemy.name}");
                    Destroy(enemy);
                }
                enemiesInCombat.RemoveAt(0);
            }
            
            // Даем очко победы
            victoryPoints++;
            UpdateVictoryPointsUI();
            
            Debug.Log($"Defeated {enemiesDefeated}/{enemiesToDefeat} enemies");
            
            // Проверяем, всех ли врагов победили
            if (enemiesDefeated >= enemiesToDefeat)
            {
                // Все враги побеждены
                Debug.Log("VICTORY! All enemies defeated!");
                ShowMessage("ПОБЕДА! Все враги повержены!", Color.green);
                isTimerRunning = false;
                Invoke("CloseDialog", 2f);
            }
            else
            {
                // Есть еще враги
                Debug.Log($"Moving to next enemy. {enemiesToDefeat - enemiesDefeated} remaining");
                
                // Сбрасываем неуязвимость игрока
                ResetPlayerInvulnerability();
                
                // Генерируем новый пример
                GenerateMathProblem();
                
                // Сбрасываем таймер
                ResetTimer();
                
                // Обновляем сообщение
                UpdateCombatMessage();
            }
        }
    }
    
    void HandleWrongAnswer()
    {
        Debug.Log("Wrong answer!");
        ShowMessage("Неправильно! -1 жизнь", Color.red);
        
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(1);
        }
        
        // Для режима боя - даем новый шанс с тем же врагом
        if (isInCombat && playerHealth != null && playerHealth.CurrentHealth > 0)
        {
            ResetTimer();
        }
    }
    
    IEnumerator HandleTimeout()
    {
        if (isProcessingTimeout) yield break;
        
        isProcessingTimeout = true;
        isTimerRunning = false;
        
        Debug.Log("Timeout!");
        ShowMessage("Время вышло! -1 жизнь", Color.red);
        
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(1);
        }
        
        yield return new WaitForSecondsRealtime(1f);
        
        // Если игрок еще жив и это режим боя, продолжаем с тем же врагом
        if (isInCombat && playerHealth != null && playerHealth.CurrentHealth > 0)
        {
            ResetPlayerInvulnerability();
            ResetTimer();
        }
        
        isProcessingTimeout = false;
    }
    
    void ResetPlayerInvulnerability()
    {
        if (playerHealth != null)
        {
            var field = typeof(PlayerHealth).GetField("isInvulnerable", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                field.SetValue(playerHealth, false);
            }
            
            playerHealth.StopAllCoroutines();
            
            SpriteRenderer sprite = playerHealth.GetComponent<SpriteRenderer>();
            if (sprite != null)
                sprite.enabled = true;
        }
    }
    
    void ShowMessage(string message, Color color)
    {
        if (resultMessageText != null)
        {
            resultMessageText.text = message;
            resultMessageText.color = color;
        }
    }
    
    void UpdateVictoryPointsUI()
    {
        if (victoryPointsText != null)
            victoryPointsText.text = $"Победы: {victoryPoints}";
    }
    
    void DisablePlayerInput()
    {
        PlayerInputDemo playerInput = GetComponent<PlayerInputDemo>();
        if (playerInput != null)
            playerInput.SetInputEnabled(false);
    }
    
    void EnablePlayerInput()
    {
        PlayerInputDemo playerInput = GetComponent<PlayerInputDemo>();
        if (playerInput != null)
            playerInput.SetInputEnabled(true);
    }
    
    void CloseDialog()
    {
        Debug.Log("Closing dialog");
        isDialogOpen = false;
        isInCombat = false;
        isTimerRunning = false;
        isProcessingTimeout = false;
        
        // Очищаем списки и счетчики
        enemiesInCombat.Clear();
        enemiesToDefeat = 0;
        enemiesDefeated = 0;
        
        if (mathDialogPanel != null)
            mathDialogPanel.SetActive(false);
        
        if (timerText != null)
            timerText.gameObject.SetActive(false);
        
        Time.timeScale = 1f;
        EnablePlayerInput();
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, combatRadius);
    }
}