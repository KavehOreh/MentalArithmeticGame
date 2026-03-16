using UnityEngine;  // Подключаем основные классы Unity

public class InteractableObject : MonoBehaviour
{
    [Header("Dialog Settings")]  // Заголовок в инспекторе
    [SerializeField] private DialogSO dialog;  // Ссылка на диалог этого объекта (перетащите из Project)
    [SerializeField] private bool oneTimeInteraction = true;  // Можно ли взаимодействовать только один раз
    
    [Header("Visual Feedback")]  // Заголовок в инспекторе
    [SerializeField] private SpriteRenderer interactionPrompt;  // Значок (например, клавиша E) над объектом
    
    // Приватные переменные
    private bool playerInRange = false;    // Находится ли игрок в зоне взаимодействия
    private bool hasBeenInteracted = false;  // Было ли уже взаимодействие
    private DialogUI dialogUI;  // Ссылка на систему диалогов
    
    private void Start()  // Вызывается перед первым кадром
    {
        // Ищем систему диалогов на сцене
        dialogUI = FindObjectOfType<DialogUI>();
        
        if (interactionPrompt != null)  // Если есть значок взаимодействия
            interactionPrompt.enabled = false;  // Скрываем его при старте
    }
    
    private void Update()  // Вызывается каждый кадр
    {
        // Если игрок в зоне И объект не был использован И нажата клавиша E
        if (playerInRange && !hasBeenInteracted && Input.GetKeyDown(KeyCode.E))
        {
            Interact();  // Вызываем метод взаимодействия
        }
    }
    
    private void Interact()  // Приватный метод взаимодействия
    {
        // Проверяем, что есть система диалогов и назначен диалог
        if (dialogUI != null && dialog != null)
        {
            dialogUI.StartDialog(dialog);  // Запускаем диалог через систему диалогов
            
            if (oneTimeInteraction)  // Если взаимодействие одноразовое
            {
                hasBeenInteracted = true;  // Отмечаем, что взаимодействие было
                
                if (interactionPrompt != null)  // Если есть значок
                    interactionPrompt.enabled = false;  // Скрываем его
            }
        }
    }
    
    // Вызывается Unity, когда другой объект входит в триггер (коллайдер с галочкой Is Trigger)
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Проверяем, что вошедший объект - игрок (по тегу "Player")
        if (other.CompareTag("Player") && (!oneTimeInteraction || !hasBeenInteracted))
        {
            playerInRange = true;  // Отмечаем, что игрок в зоне
            
            if (interactionPrompt != null)  // Если есть значок
                interactionPrompt.enabled = true;  // Показываем его
        }
    }
    
    // Вызывается Unity, когда другой объект покидает триггер
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))  // Если ушедший объект - игрок
        {
            playerInRange = false;  // Отмечаем, что игрок покинул зону
            
            if (interactionPrompt != null)  // Если есть значок
                interactionPrompt.enabled = false;  // Скрываем его
        }
    }
}