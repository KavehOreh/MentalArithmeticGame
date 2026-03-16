using UnityEngine;           // Основные классы Unity
using UnityEngine.UI;        // Классы для работы с UI (панели, кнопки)
using System.Collections;    // Для использования корутин (IEnumerator)
using TMPro;                 // Для TextMeshPro (более красивый и настраиваемый текст)

public class DialogUI : MonoBehaviour  // MonoBehaviour - базовый класс для всех скриптов в Unity
{
    [Header("UI Elements")]  // Атрибут для группировки полей в инспекторе Unity
    [SerializeField] private GameObject dialogPanel;           // Ссылка на панель диалога (GameObject)
    [SerializeField] private TextMeshProUGUI speakerText;     // Ссылка на текст с именем говорящего
    [SerializeField] private TextMeshProUGUI dialogText;      // Ссылка на текст самой реплики
    [SerializeField] private Image speakerImage;              // Ссылка на изображение говорящего (опционально)
    
    [Header("Typing Effect")]  // Настройки эффекта печатания текста
    [SerializeField] private float typingSpeed = 0.05f;       // Скорость печатания (время между символами)
    
    // Приватные переменные (не видны в инспекторе)
    private DialogSO currentDialog;        // Текущий диалог, который сейчас воспроизводится
    private int currentDialogPart = 0;      // Индекс текущей части диалога (какой персонаж говорит)
    private int currentSentence = 0;        // Индекс текущего предложения в текущей части
    private bool isTyping = false;          // Флаг, идет ли сейчас печатание текста
    private Coroutine typingCoroutine;      // Ссылка на запущенную корутину печатания
    
    private PlayerInputDemo playerInput;    // Ссылка на компонент управления игроком
    
    private void Awake()  // Вызывается при создании объекта (до Start)
    {
        dialogPanel.SetActive(false);        // Скрываем панель диалога при старте игры
        // Ищем компонент управления игроком на сцене
        playerInput = FindObjectOfType<PlayerInputDemo>();
        
        // Если не нашли, выводим предупреждение в консоль
        if (playerInput == null)
        {
            Debug.LogWarning("PlayerInputDemo не найден на сцене! Управление не будет блокироваться.");
        }
    }
    
    private void Update()  // Вызывается каждый кадр (примерно 60 раз в секунду)
    {
        // Если панель диалога активна И нажат пробел
        if (dialogPanel.activeSelf && Input.GetKeyDown(KeyCode.Space))
        {
            ContinueDialog();  // Продолжаем диалог (следующая реплика)
        }
    }
    
    // Публичный метод для начала диалога (вызывается из других скриптов)
    public void StartDialog(DialogSO dialog)
    {
        currentDialog = dialog;              // Сохраняем ссылку на текущий диалог
        currentDialogPart = 0;                // Начинаем с первой части
        currentSentence = 0;                   // Начинаем с первого предложения
        
        // Блокируем управление игрока (отключаем компонент)
        if (playerInput != null)
            playerInput.enabled = false;      // Игрок не может двигаться во время диалога
        
        dialogPanel.SetActive(true);          // Показываем панель диалога
        DisplayCurrentSentence();              // Начинаем показывать текст
    }
    
    // Приватный метод для отображения текущего предложения
    private void DisplayCurrentSentence()
    {
        // Проверяем, не закончились ли все части диалога
        if (currentDialogPart >= currentDialog.dialogParts.Count)
        {
            EndDialog();  // Завершаем диалог
            return;       // Выходим из метода
        }
        
        var currentPart = currentDialog.dialogParts[currentDialogPart];  // Получаем текущую часть диалога
        
        // Проверяем, не закончились ли предложения в текущей части
        if (currentSentence >= currentPart.sentences.Length)
        {
            // Переходим к следующей части диалога
            currentDialogPart++;      // Увеличиваем индекс части
            currentSentence = 0;       // Сбрасываем индекс предложения
            DisplayCurrentSentence();  // Рекурсивно вызываем метод для новой части
            return;                    // Выходим из метода
        }
        
        // Обновляем информацию о говорящем в UI
        speakerText.text = currentPart.speakerName;  // Устанавливаем имя говорящего
        
        // Запускаем эффект печатания текста
        if (typingCoroutine != null)                  // Если уже есть работающая корутина
            StopCoroutine(typingCoroutine);           // Останавливаем её
            
        // Запускаем новую корутину для печатания текста
        typingCoroutine = StartCoroutine(TypeText(currentPart.sentences[currentSentence]));
    }
    
    // Корутина для эффекта печатания текста (IEnumerator - специальный тип для корутин)
    private IEnumerator TypeText(string text)
    {
        isTyping = true;                    // Устанавливаем флаг, что идет печатание
        dialogText.text = "";                // Очищаем текст в UI
        
        // Перебираем каждый символ в тексте по очереди
        foreach (char letter in text.ToCharArray())
        {
            dialogText.text += letter;       // Добавляем один символ к тексту
            yield return new WaitForSeconds(typingSpeed);  // Ждем указанное время перед следующим символом
        }
        
        isTyping = false;                    // Снимаем флаг печатания (текст полностью напечатан)
    }
    
    // Метод для продолжения диалога (вызывается при нажатии пробела)
    private void ContinueDialog()
    {
        if (isTyping)  // Если текст еще печатается
        {
            // Показываем весь текст сразу (пропускаем анимацию печатания)
            StopCoroutine(typingCoroutine);  // Останавливаем корутину печатания
            var currentPart = currentDialog.dialogParts[currentDialogPart];  // Получаем текущую часть
            dialogText.text = currentPart.sentences[currentSentence];  // Показываем весь текст сразу
            isTyping = false;  // Снимаем флаг печатания
        }
        else  // Если текст уже полностью напечатан
        {
            // Переходим к следующему предложению
            currentSentence++;  // Увеличиваем индекс предложения
            DisplayCurrentSentence();  // Показываем следующее предложение
        }
    }
    
    // Метод для завершения диалога
    private void EndDialog()
    {
        dialogPanel.SetActive(false);  // Скрываем панель диалога
        
        // Разблокируем управление игрока
        if (playerInput != null)
            playerInput.enabled = true;  // Включаем компонент управления обратно
    }
    
    // Публичный метод для принудительного завершения диалога (например, для пропуска кат-сцены)
    public void ForceEndDialog()
    {
        if (typingCoroutine != null)           // Если есть работающая корутина
            StopCoroutine(typingCoroutine);    // Останавливаем её
            
        dialogPanel.SetActive(false);          // Скрываем панель диалога
        
        if (playerInput != null)               // Если есть ссылка на управление
            playerInput.enabled = true;         // Включаем управление игроком
    }
}