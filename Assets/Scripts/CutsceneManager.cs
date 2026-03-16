using UnityEngine;           // Основные классы Unity
using System.Collections;    // Для использования корутин

public class CutsceneManager : MonoBehaviour
{
    [Header("Start Cutscene")]  // Заголовок в инспекторе
    [SerializeField] private DialogSO startCutsceneDialog;  // Диалог для начальной кат-сцены
    [SerializeField] private float delayBeforeStart = 0.5f;  // Задержка перед началом (секунды)
    
    [Header("Components")]  // Заголовок в инспекторе
    [SerializeField] private DialogUI dialogUI;  // Ссылка на систему диалогов (перетащите из Hierarchy)
    [SerializeField] private GameObject cutsceneCamera;  // Отдельная камера для кат-сцены
    [SerializeField] private GameObject player;  // Ссылка на объект игрока (перетащите из Hierarchy)
    
    private PlayerInputDemo playerInput;  // Компонент управления игроком
    private bool cutscenePlaying = false;  // Флаг, идет ли сейчас кат-сцена
    
    private void Start()  // Вызывается перед первым кадром
    {
        // Получаем компонент PlayerInputDemo с объекта игрока
        if (player != null)
            playerInput = player.GetComponent<PlayerInputDemo>();
        else  // Если игрок не назначен, ищем на сцене
            playerInput = FindObjectOfType<PlayerInputDemo>();
        
        // Запускаем корутину с кат-сценой
        StartCoroutine(PlayStartCutscene());
    }
    
    // Корутина для воспроизведения начальной кат-сцены
    private IEnumerator PlayStartCutscene()
    {
        cutscenePlaying = true;  // Отмечаем, что кат-сцена началась
        
        // Отключаем управление игроком
        if (playerInput != null)
            playerInput.enabled = false;  // Игрок не может двигаться во время кат-сцены
        
        // Включаем камеру кат-сцены
        if (cutsceneCamera != null)
            cutsceneCamera.SetActive(true);  // Показываем вид от камеры кат-сцены
        
        // Ждем указанное время перед началом диалога
        yield return new WaitForSeconds(delayBeforeStart);  // Приостанавливаем корутину на delayBeforeStart секунд
        
        // Запускаем диалог кат-сцены
        if (dialogUI != null && startCutsceneDialog != null)
        {
            dialogUI.StartDialog(startCutsceneDialog);  // Начинаем диалог через систему диалогов
            
            // Ждем окончания диалога
            while (dialogUI.gameObject.activeSelf)  // Пока панель диалога активна
            {
                yield return null;  // Ждем один кадр и проверяем снова
            }
        }
        
        // Выключаем камеру кат-сцены
        if (cutsceneCamera != null)
            cutsceneCamera.SetActive(false);  // Возвращаем обычный вид (основную камеру)
        
        // Включаем управление игроком
        if (playerInput != null)
            playerInput.enabled = true;  // Игрок снова может двигаться
        
        cutscenePlaying = false;  // Отмечаем, что кат-сцена закончилась
    }
    
    // Публичный метод для пропуска кат-сцены (можно привязать к кнопке)
    public void SkipCutscene()
    {
        if (cutscenePlaying && dialogUI != null)  // Если кат-сцена идет
        {
            dialogUI.ForceEndDialog();  // Принудительно завершаем диалог
            StopAllCoroutines();         // Останавливаем все корутины
            
            if (cutsceneCamera != null)
                cutsceneCamera.SetActive(false);  // Выключаем камеру кат-сцены
            
            if (playerInput != null)
                playerInput.enabled = true;  // Включаем управление игроком
            
            cutscenePlaying = false;  // Отмечаем, что кат-сцена закончилась
        }
    }
}