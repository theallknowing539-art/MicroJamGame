using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [Header("Visuals")]
    private SkinnedMeshRenderer[] _renderers; 
    [SerializeField] private float _flashDuration = 0.05f;

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

    [Header("Death Effects")]
    [SerializeField] private float deathAnimationDuration = 2f;
    [SerializeField] private GameObject boneParticlePrefab;

    [Header("Knockback Settings")]
    [SerializeField] private float friction = 10f; // How fast they stop sliding
    private Vector3 _knockbackVelocity;

    [Header("Drop Table")]
    [SerializeField] private DropTable dropTable;

    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Audio")]
    [SerializeField] private AudioClip hitSound; 
    [Range(0f, 1f)] [SerializeField] private float hitVolume = 0.7f;
    private AudioSource _audioSource;

    private static readonly int AnimIsWalking = Animator.StringToHash("IsWalking");
    private NavMeshAgent _agent;
    private Transform    _player;
    private bool         _isDead        = false;
    private bool         _isAttacking   = false;
    private float        _attackTimer   = 0f;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = moveSpeed;
        _agent.stoppingDistance = stoppingDistance;
        _currentHealth = maxHealth;
        _audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        _renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;
    }

    private void Update()
    {
        if (_isDead || _player == null) return;

        // Apply Knockback Physics
        if (_knockbackVelocity.magnitude > 0.1f)
        {
            _agent.Move(_knockbackVelocity * Time.deltaTime);
            _knockbackVelocity = Vector3.Lerp(_knockbackVelocity, Vector3.zero, Time.deltaTime * friction);
        }

        float distanceToPlayer = Vector3.Distance(transform.position, _player.position);
        _attackTimer -= Time.deltaTime;

        if (distanceToPlayer <= attackRange)
        {
            _agent.SetDestination(transform.position);
            animator.SetBool(AnimIsWalking, false);
            if (!_isAttacking && _attackTimer <= 0f) StartCoroutine(AttackRoutine());
        }
        else
        {
            if (!_isAttacking)
            {
                _agent.SetDestination(_player.position);
                animator.SetBool(AnimIsWalking, true);
            }
        }
    }

    public void ApplyKnockback(Vector3 force)
    {
        if (_isDead) return;
        _knockbackVelocity += force;
        
        // Briefly pause the AI pathfinding so they don't fight the push
        StopAllCoroutines();
        _isAttacking = false;
        StartCoroutine(ResetAgentRoutine());
    }

    private IEnumerator ResetAgentRoutine()
    {
        _agent.isStopped = true;
        yield return new WaitForSeconds(0.2f);
        if (!_isDead) _agent.isStopped = false;
    }

    public void TakeDamage(float amount)
    {
        if (_isDead) return;
        _currentHealth -= amount;

        if (_currentHealth <= 0f) StartCoroutine(Die());
        
        StartCoroutine(FlashRed());
        if (HitStopManager.Instance != null) HitStopManager.Instance.Stop(0.03f);
        if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.15f);
    }

    private IEnumerator FlashRed()
    {
        MaterialPropertyBlock props = new MaterialPropertyBlock();
        props.SetColor("_Color", Color.red);
        foreach (var r in _renderers) r.SetPropertyBlock(props);
        yield return new WaitForSecondsRealtime(_flashDuration);
        foreach (var r in _renderers) r.SetPropertyBlock(null);
    }

    private IEnumerator Die()
    {
        _isDead = true;
        _agent.enabled = false;
        if (boneParticlePrefab != null) Instantiate(boneParticlePrefab, transform.position + Vector3.up, Quaternion.identity);
        
        WaveManager.Instance.ReportEnemyDeath();
        RollDrop();
        animator.Play("Death");
        yield return new WaitForSeconds(deathAnimationDuration);
        Destroy(gameObject);
    }

    private void RollDrop()
    {
        if (dropTable == null) return;
        ItemData dropped = dropTable.Roll();
        if (dropped != null && dropped.pickupPrefab != null)
            Instantiate(dropped.pickupPrefab, transform.position + Vector3.up * 0.3f, Quaternion.identity);
    }

    private IEnumerator AttackRoutine()
    {
        _isAttacking = true;
        _attackTimer = attackCooldown;
        animator.Play("Attack");
        yield return new WaitForSeconds(windUpDuration);
        if (!_isDead && Vector3.Distance(transform.position, _player.position) <= attackRange)
            PlayerHealth.Instance.TakeDamage(attackDamage);
        _isAttacking = false;
    }
}