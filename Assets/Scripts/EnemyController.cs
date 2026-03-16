using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Enemy Settings")]
    public float moveSpeed = 2f;
    public int damage = 1;
    public float chaseRadius = 5f;
    
    [Header("Effects")]
    public GameObject deathEffect;
    public AudioClip deathSound;
    
    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private bool isChasing = false;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        
        // Находим игрока
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }
    
    void Update()
    {
        if (player == null) return;
        
        // Проверяем дистанцию до игрока
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        isChasing = distanceToPlayer <= chaseRadius;
        
        // Анимация
        if (animator != null)
            animator.SetBool("IsChasing", isChasing);
    }
    
    void FixedUpdate()
    {
        if (player == null || !isChasing) return;
        
        // Движение к игроку
        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed;
        
        // Поворот спрайта
        if (spriteRenderer != null)
            spriteRenderer.flipX = direction.x < 0;
    }
    
    void OnDestroy()
    {
        // Эффекты при уничтожении
        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);
            
        if (deathSound != null)
            AudioSource.PlayClipAtPoint(deathSound, transform.position);
    }
    
    void OnDrawGizmosSelected()
    {
        // Визуализация радиуса преследования
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRadius);
    }
}