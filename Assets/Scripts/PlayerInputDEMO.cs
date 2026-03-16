using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputDemo : MonoBehaviour
{
    private Rigidbody2D rb;
    private float speed = 5f;
    private Vector2 moveInput;
    private SpriteRenderer sprite;
    
    [Header("Input Settings")]
    private bool isInputEnabled = true; // Флаг для включения/отключения ввода

    void Start()
    {       
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
    } 
    
    void Update()
    {
        // Двигаемся только если ввод включен
        if (isInputEnabled)
        {
            rb.linearVelocity = moveInput * speed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero; // Останавливаемся, если ввод отключен
        }
    }
    
    public void OnMove(InputAction.CallbackContext context)
    {
        // Обрабатываем ввод только если он включен
        if (isInputEnabled)
        {
            moveInput = context.ReadValue<Vector2>();
            
            // Поворачиваем спрайт в зависимости от направления
            if (moveInput.x != 0)
            {
                sprite.flipX = moveInput.x > 0;
            }
        }
    }
    
    // Добавляем метод, который ожидает PlayerHealth
    public void SetInputEnabled(bool enabled)
    {
        isInputEnabled = enabled;
        
        // Если ввод отключаем, сбрасываем скорость
        if (!enabled)
        {
            moveInput = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
        }
        
        Debug.Log($"Input enabled: {enabled}");
    }
    
    // Дополнительные методы для удобства
    public void EnableInput()
    {
        SetInputEnabled(true);
    }
    
    public void DisableInput()
    {
        SetInputEnabled(false);
    }
    
    // Проверка, включен ли ввод
    public bool IsInputEnabled()
    {
        return isInputEnabled;
    }
}