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
    private float _currentHealth;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float stoppingDistance = 1.5f;   // how close before it stops and attacks

    [Header("Attack")]
    [SerializeField] private float attackDamage = 15f;
    [SerializeField] private float attackRange = 2f;           // must be within this to attack
    [SerializeField] private float windUpDuration = 0.8f;      // seconds before the hit lands
    [SerializeField] private float attackCooldown = 1.5f;      // seconds between attacks

    [Header("Death")]
    [SerializeField] private float deathAnimationDuration = 2f;

    [Header("Drop Table")]
    [SerializeField] private DropTable dropTable;

    [Header("References")]
    [SerializeField] private Animator animator;

    // ----------------------------------------------------------------
    // Animator parameter names — must match your Animator exactly
    // ----------------------------------------------------------------
    private static readonly int AnimIsWalking  = Animator.StringToHash("IsWalking");
    private static readonly int AnimAttack     = Animator.StringToHash("Attack");
    private static readonly int AnimDeath      = Animator.StringToHash("Death");

    // ----------------------------------------------------------------
    // Private state
    // ----------------------------------------------------------------
    private NavMeshAgent _agent;
    private Transform    _player;
    private bool         _isDead       = false;
    private bool         _isAttacking  = false;
    private float        _attackTimer  = 0f;

    // ----------------------------------------------------------------
    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed           = moveSpeed;
        _agent.stoppingDistance = stoppingDistance;

        _currentHealth = maxHealth;
    }

    // ----------------------------------------------------------------
    private void Start()
    {
        // find the player by tag — make sure your Player is tagged "Player"
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            _player = playerObj.transform;
    }

    // ----------------------------------------------------------------
    private void Update()
    {
        if (_isDead || _player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, _player.position);

        _attackTimer -= Time.deltaTime;

        if (distanceToPlayer <= attackRange)
        {
            // stop moving and attack
            _agent.SetDestination(transform.position);
            animator.SetBool(AnimIsWalking, false);

            if (!_isAttacking && _attackTimer <= 0f)
                StartCoroutine(AttackRoutine());
        }
        else
        {
            // chase the player
            _agent.SetDestination(_player.position);
            animator.SetBool(AnimIsWalking, true);
        }
    }

    // ----------------------------------------------------------------
    private IEnumerator AttackRoutine()
    {
        _isAttacking = true;
        _attackTimer = attackCooldown;

        // face the player before attacking
        Vector3 direction = (_player.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z));

        // trigger wind-up animation
        animator.SetTrigger(AnimAttack);

        // wait for wind-up to complete before dealing damage
        yield return new WaitForSeconds(windUpDuration);

        // only deal damage if player is still in range after wind-up
        // prevents hits landing after the player has dodged away
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
    private IEnumerator Die()
    {
        _isDead = true;

        // stop everything
        _agent.isStopped = true;
        _agent.enabled   = false;
        StopCoroutine(nameof(AttackRoutine));

        // play death animation
        animator.SetTrigger(AnimDeath);

        // report to wave manager immediately so wave can complete
        WaveManager.Instance.ReportEnemyDeath();

        // roll the drop table
        RollDrop();

        // wait for death animation to finish
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
            // spawn slightly above the skeleton so it doesn't clip into the floor
            Vector3 spawnPos = transform.position + Vector3.up * 0.3f;
            Instantiate(dropped.pickupPrefab, spawnPos, Quaternion.identity);
        }
    }
}