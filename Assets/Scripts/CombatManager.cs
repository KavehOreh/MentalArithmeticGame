using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class CombatManager : MonoBehaviour
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
    private PlayerHealth playerHealth;
    public int victoryPoints = 0;
    public TextMeshProUGUI victoryPointsText;
    
    [Header("Combat Settings")]
    public float timePerProblem = 10f;
    private float currentTime;
    private bool isTimerRunning = false;
    
    [Header("Enemy Settings")]
    public LayerMask enemyLayer;
    public float combatRadius = 3f;
    
    private List<GameObject> currentEnemies = new List<GameObject>();
    private int correctAnswer;
    private bool isDialogOpen = false;
    private int problemsSolved = 0;
    private int totalProblemsNeeded = 3;
    
    void Start()
    {
        if (mathDialogPanel != null)
            mathDialogPanel.SetActive(false);
            
        if (submitButton != null)
            submitButton.onClick.AddListener(CheckAnswer);
            
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseDialog);
            
        UpdateVictoryPointsUI();
    }
    
    void Update()
    {
        if (isTimerRunning)
        {
            currentTime -= Time.deltaTime;
            UpdateTimerUI();
            
            if (currentTime <= 0)
            {
                TimeOut();
            }
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy") && !isDialogOpen)
        {
            StartCombat(collision.gameObject);
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy") && !isDialogOpen)
        {
            StartCombat(other.gameObject);
        }
    }
    
    void StartCombat(GameObject enemy)
    {
        FindNearbyEnemies(enemy.transform.position);
        
        if (currentEnemies.Count > 0)
        {
            OpenMathDialog();
        }
    }
    
    void FindNearbyEnemies(Vector2 center)
    {
        currentEnemies.Clear();
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(center, combatRadius, enemyLayer);
        
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Enemy"))
            {
                currentEnemies.Add(hitCollider.gameObject);
            }
        }
    }
    
    void OpenMathDialog()
    {
        isDialogOpen = true;
        problemsSolved = 0;
        totalProblemsNeeded = currentEnemies.Count;
        
        GenerateMathProblem();
        
        if (mathDialogPanel != null)
            mathDialogPanel.SetActive(true);
            
        currentTime = timePerProblem;
        isTimerRunning = true;
        
        if (answerInputField != null)
        {
            answerInputField.text = "";
            answerInputField.ActivateInputField();
        }
        
        if (resultMessageText != null)
            resultMessageText.text = $"Осталось врагов: {currentEnemies.Count}";
        
        Time.timeScale = 0f;
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
            
        currentTime = timePerProblem;
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
            if (answer == correctAnswer)
            {
                problemsSolved++;
                ShowMessage("Правильно!", Color.green);
                
                if (currentEnemies.Count > 0)
                {
                    Destroy(currentEnemies[0]);
                    currentEnemies.RemoveAt(0);
                }
                
                if (currentEnemies.Count == 0)
                {
                    Victory();
                }
                else
                {
                    GenerateMathProblem();
                }
            }
            else
            {
                ShowMessage("Неправильно! -1 жизнь", Color.red);
                
                if (playerHealth != null)
                    playerHealth.TakeDamage(1);
            }
            
            answerInputField.text = "";
            answerInputField.ActivateInputField();
            
            if (resultMessageText != null)
                resultMessageText.text = $"Осталось врагов: {currentEnemies.Count}";
        }
        else
        {
            ShowMessage("Введите число!", Color.yellow);
        }
    }
    
    void Victory()
    {
        ShowMessage("ПОБЕДА! +1 очко", Color.green);
        victoryPoints++;
        UpdateVictoryPointsUI();
        
        Invoke("CloseDialog", 2f);
    }
    
    void TimeOut()
    {
        isTimerRunning = false;
        ShowMessage("Время вышло! -1 жизнь", Color.red);
        
        if (playerHealth != null)
            playerHealth.TakeDamage(1);
        
        // ИСПРАВЛЕНО: используем свойство CurrentHealth вместо поля currentHealth
        if (playerHealth != null && playerHealth.CurrentHealth > 0)
        {
            currentTime = timePerProblem;
            isTimerRunning = true;
            GenerateMathProblem();
            
            answerInputField.text = "";
            answerInputField.ActivateInputField();
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
    
    void UpdateTimerUI()
    {
        if (timerText != null)
        {
            timerText.text = $"Время: {Mathf.Max(0, currentTime):F1}";
            
            if (currentTime < 3f)
                timerText.color = Color.red;
            else
                timerText.color = Color.white;
        }
    }
    
    void UpdateVictoryPointsUI()
    {
        if (victoryPointsText != null)
            victoryPointsText.text = $"Победы: {victoryPoints}";
    }
    
    void CloseDialog()
    {
        isDialogOpen = false;
        isTimerRunning = false;
        
        if (mathDialogPanel != null)
            mathDialogPanel.SetActive(false);
        
        Time.timeScale = 1f;
    }
    
    // Добавляем метод для проверки взаимодействия
    public void CheckForInteractable()
    {
        // Здесь можно добавить логику ручного взаимодействия
        Debug.Log("Checking for interactable objects...");
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, combatRadius);
    }
}