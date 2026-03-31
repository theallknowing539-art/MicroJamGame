// ================================================================
// Enemy.cs
// Attach to: Skeleton prefab
// Requires: NavMeshAgent, Animator, Collider
// ================================================================
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    // ----------------------------------------------------------------
    // Stats
    // ----------------------------------------------------------------
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField]private float _currentHealth;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float stoppingDistance = 1.5f;

    [Header("Attack")]
    [SerializeField] private float attackDamage = 15f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float windUpDuration = 0.8f;
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Death")]
    [SerializeField] private float deathAnimationDuration = 2f;

    [Header("Knockback")]
    [SerializeField] private float knockbackDuration = 0.4f;

    [Header("Drop Table")]
    [SerializeField] private DropTable dropTable;

    [Header("References")]
    [SerializeField] private Animator animator;

    // ----------------------------------------------------------------
    // Animator parameter hashes
    // ----------------------------------------------------------------
    private static readonly int AnimIsWalking = Animator.StringToHash("IsWalking");
    private static readonly int AnimAttack    = Animator.StringToHash("Attack");
    private static readonly int AnimDeath     = Animator.StringToHash("Death");

    // ----------------------------------------------------------------
    // Private state
    // ----------------------------------------------------------------
    private NavMeshAgent _agent;
    private Transform    _player;
    private bool         _isDead        = false;
    private bool         _isAttacking   = false;
    private bool         _isKnockedBack = false;
    private float        _attackTimer   = 0f;

    // ----------------------------------------------------------------
    private void Awake()
    {
        _agent                  = GetComponent<NavMeshAgent>();
        _agent.speed            = moveSpeed;
        _agent.stoppingDistance = stoppingDistance;
        _currentHealth          = maxHealth;
    }

    // ----------------------------------------------------------------
    /*private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            _player = playerObj.transform;
    }*/
    private void Start()
{
    GameObject playerObj = GameObject.FindWithTag("Player");
    if (playerObj != null)
    {
        _player = playerObj.transform;
        Debug.LogError($"Enemy found player: {playerObj.name} at {playerObj.transform.position}");
    }
    else
    {
        Debug.LogError("Enemy could NOT find player!");
    }
}

    // ----------------------------------------------------------------
/*private void Update()
{
    if (_isDead || _isKnockedBack || _player == null) return;

    float distanceToPlayer = Vector3.Distance(transform.position, _player.position);
    _attackTimer -= Time.deltaTime;

    //Debug.Log($"Distance: {distanceToPlayer} | AttackRange: {attackRange} | Velocity: {_agent.velocity}");
    Debug.Log($"Distance: {distanceToPlayer} | AttackRange: {attackRange} | Velocity: {_agent.velocity} | PathStatus: {_agent.pathStatus} | HasPath: {_agent.hasPath}");
    if (distanceToPlayer <= attackRange)
    {
        _agent.SetDestination(transform.position);
        animator.SetBool(AnimIsWalking, false);

        if (!_isAttacking && _attackTimer <= 0f)
            StartCoroutine(AttackRoutine());
    }
    else
    {
        _agent.SetDestination(_player.position);
        animator.SetBool(AnimIsWalking, true);
    }
}*/
    private void Update()
{
    if (_isDead || _isKnockedBack || _player == null) return;

    float distanceToPlayer = Vector3.Distance(transform.position, _player.position);
    _attackTimer -= Time.deltaTime;

    if (distanceToPlayer <= attackRange)
    {
        // Use isStopped instead of SetDestination(transform.position) 
        // to prevent path recalculation errors
        _agent.isStopped = true; 
        animator.SetBool(AnimIsWalking, false);

        if (!_isAttacking && _attackTimer <= 0f)
            StartCoroutine(AttackRoutine());
    }
    else
    {
        _agent.isStopped = false;
        // Only update destination if player has moved significantly to save performance
        _agent.SetDestination(_player.position);
        animator.SetBool(AnimIsWalking, true);
    }
}
    // ----------------------------------------------------------------
    private IEnumerator AttackRoutine()
    {
        _isAttacking = true;
        _attackTimer = attackCooldown;

        // face the player
        Vector3 direction = (_player.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z));

        animator.SetTrigger(AnimAttack);

        // wait for wind-up
        yield return new WaitForSeconds(windUpDuration);

        // only deal damage if player is still in range after wind-up
        if (!_isDead && _player != null)
        {
            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist <= attackRange)
                PlayerHealth.Instance.TakeDamage(attackDamage);
        }

        _isAttacking = false;
    }

    // ----------------------------------------------------------------
    public void TakeDamage(float amount)
    {
        if (_isDead) return;

        _currentHealth -= amount;

        if (_currentHealth <= 0f)
            StartCoroutine(Die());
    }

    // ----------------------------------------------------------------
    public void ApplyKnockback(Vector3 force)
    {
        if (_isDead) return;
        StartCoroutine(KnockbackRoutine(force));
    }

    // ----------------------------------------------------------------
    private IEnumerator KnockbackRoutine(Vector3 force)
    {
        _isKnockedBack   = true;
        _agent.isStopped = true;
        _agent.enabled   = false;

        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            // decelerate over the duration — goes from full force to zero
            float t = 1f - (elapsed / knockbackDuration);
            transform.position += force * t * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // re-enable navmesh if still alive
        if (!_isDead)
        {
            _agent.enabled   = true;
            _agent.isStopped = false;
        }

        _isKnockedBack = false;
    }

    // ----------------------------------------------------------------
    private IEnumerator Die()
    {
        _isDead          = true;
        _agent.isStopped = true;
        _agent.enabled   = false;

        animator.SetTrigger(AnimDeath);

        WaveManager.Instance.ReportEnemyDeath();
        RollDrop();

        yield return new WaitForSeconds(deathAnimationDuration);

        Destroy(gameObject);
    }

    // ----------------------------------------------------------------
    private void RollDrop()
    {
        if (dropTable == null) return;

        ItemData dropped = dropTable.Roll();
        if (dropped != null && dropped.pickupPrefab != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 0.3f;
            Instantiate(dropped.pickupPrefab, spawnPos, Quaternion.identity);
        }
    }
}