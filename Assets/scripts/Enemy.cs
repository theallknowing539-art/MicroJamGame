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

    [Header("Visuals")]
private SkinnedMeshRenderer[] _renderers; // Drag the skeleton mesh here
[SerializeField] private float _flashDuration = 0.1f;
private Color _originalColor;

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    private float _currentHealth;

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
    [SerializeField] private float knockbackSpeed = 8f;

    [Header("Drop Table")]
    [SerializeField] private DropTable dropTable;

    [Header("References")]
    [SerializeField] private Animator animator;

    // ----------------------------------------------------------------
    // Animator parameter hashes
    // ----------------------------------------------------------------
    private static readonly int AnimIsWalking = Animator.StringToHash("IsWalking");

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
        _agent.autoBraking      = false;
        _currentHealth          = maxHealth;
    }

    // ----------------------------------------------------------------
    private void Start()
    {
      _renderers = GetComponentsInChildren<SkinnedMeshRenderer>();

    if (_renderers.Length == 0)
    {
        Debug.LogError($"[Enemy] No SkinnedMeshRenderers found on {gameObject.name}!");
    }
        _agent.Warp(transform.position);

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            _player = playerObj.transform;

        animator.SetBool(AnimIsWalking, false);

    }

    // ----------------------------------------------------------------
    private void Update()
    {
        if (_isDead || _isKnockedBack || _player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, _player.position);
        _attackTimer -= Time.deltaTime;

        if (distanceToPlayer <= attackRange)
        {
            _agent.SetDestination(transform.position);
            animator.SetBool(AnimIsWalking, false);

            if (!_isAttacking && _attackTimer <= 0f)
                StartCoroutine(AttackRoutine());
        }
        else
        {
            if (_isAttacking)
                _isAttacking = false;

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
        transform.rotation = Quaternion.LookRotation(
            new Vector3(direction.x, 0f, direction.z));

        // force play attack animation
        animator.SetBool(AnimIsWalking, false);
        animator.Play("Attack");

        // wait for wind-up
        yield return new WaitForSeconds(windUpDuration);

        // only deal damage if player still in range after wind-up
        if (!_isDead && _player != null)
        {
            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist <= attackRange)
                PlayerHealth.Instance.TakeDamage(attackDamage);
        }

        // wait for rest of attack animation to finish
        yield return new WaitForSeconds(attackCooldown - windUpDuration);

        _isAttacking = false;
    }

    // ----------------------------------------------------------------
    public void TakeDamage(float amount)
    {
        if (_isDead) return;

        _currentHealth -= amount;
        if (HitStopManager.Instance != null)
         HitStopManager.Instance.Stop(0.06f); 

        // Trigger Flash Effect
        StartCoroutine(FlashRed());

        if (_currentHealth <= 0f)
            StartCoroutine(Die());

        
    }
    private IEnumerator FlashRed()
{
    if (_renderers == null || _renderers.Length == 0) yield break;

    // Create the "Red Tint" instructions
    MaterialPropertyBlock props = new MaterialPropertyBlock();
    props.SetColor("_Color", Color.red);
    props.SetColor("_BaseColor", Color.red); // For URP shaders

    // Turn EVERY part red
    foreach (var renderer in _renderers)
    {
        renderer.SetPropertyBlock(props);
    }
    
    // Wait for the hit-stop duration
    yield return new WaitForSecondsRealtime(_flashDuration);

    // Reset EVERY part back to normal
    foreach (var renderer in _renderers)
    {
        renderer.SetPropertyBlock(null);
    }
}
    // ----------------------------------------------------------------
    // Knockback — only moves transform, never touches NavMeshAgent
    // ----------------------------------------------------------------
    public void ApplyKnockback(Vector3 force)
    {
        if (_isDead) return;
        StartCoroutine(KnockbackRoutine(force));
    }

    // ----------------------------------------------------------------
    private IEnumerator KnockbackRoutine(Vector3 force)
    {
        _isKnockedBack = true;

        // tell the agent to stay still without disabling it
        _agent.isStopped = true;

        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            // decelerate over duration
            float t = 1f - (elapsed / knockbackDuration);

            // move transform directly — agent is stopped but still enabled
            // so it snaps back to navmesh automatically when isStopped = false
            transform.position += force * t * knockbackSpeed * Time.deltaTime;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // resume agent from new position
        _agent.isStopped = false;

        if (_player != null)
            _agent.SetDestination(_player.position);

        _isKnockedBack = false;
    }

    // ----------------------------------------------------------------
    private IEnumerator Die()
    {
        _isDead      = true;
        _isAttacking = false;

        _agent.isStopped = true;
        _agent.velocity  = Vector3.zero;

        animator.SetBool(AnimIsWalking, false);
        Time.timeScale = 0.4f; // Slow down to 40% speed
        Time.fixedDeltaTime = 0.02f * Time.timeScale; // K
        // wait one frame before playing death
        yield return null;

        _agent.enabled = false;
        animator.Play("Death");

        WaveManager.Instance.ReportEnemyDeath();
        RollDrop();

        yield return new WaitForSeconds(deathAnimationDuration);
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
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
/*
```

---

**What changed for knockback:**

The agent is never disabled — only `isStopped = true` is set which pauses pathfinding but keeps the agent alive and connected to the NavMesh. The transform is moved directly during the knockback duration. When `isStopped = false` is set afterward the agent snaps back onto the NavMesh from its new position automatically and resumes chasing.
```
Knockback starts
  → agent.isStopped = true    (paused, still on NavMesh)
  → transform moves away from player each frame
  → knockback ends
  → agent.isStopped = false   (resumes from new position)
  → SetDestination(player)    (starts chasing again)*/