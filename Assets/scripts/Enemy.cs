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
    [SerializeField] private GameObject boneParticlePrefab; // Shatter effect

    [Header("Knockback")]
    [SerializeField] private float knockbackDuration = 0.4f;
    [SerializeField] private float knockbackSpeed = 8f;

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
    private bool         _isKnockedBack = false;
    private float        _attackTimer   = 0f;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = moveSpeed;
        _agent.stoppingDistance = stoppingDistance;
        _agent.autoBraking = false;
        _currentHealth = maxHealth;
        _audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        _renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        _agent.Warp(transform.position);
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;
        animator.SetBool(AnimIsWalking, false);
    }

    private void Update()
    {
        if (_isDead || _isKnockedBack || _player == null) return;

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
            if (_isAttacking) _isAttacking = false;
            _agent.SetDestination(_player.position);
            animator.SetBool(AnimIsWalking, true);
        }
    }

    private IEnumerator AttackRoutine()
    {
        _isAttacking = true;
        _attackTimer = attackCooldown;
        Vector3 direction = (_player.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z));
        animator.SetBool(AnimIsWalking, false);
        animator.Play("Attack");
        yield return new WaitForSeconds(windUpDuration);
        if (!_isDead && _player != null)
        {
            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist <= attackRange) PlayerHealth.Instance.TakeDamage(attackDamage);
        }
        yield return new WaitForSeconds(attackCooldown - windUpDuration);
        _isAttacking = false;
    }

    public void TakeDamage(float amount)
    {
        if (_isDead) return;

        _currentHealth -= amount;

        if (_currentHealth <= 0f)
        {
            if (_audioSource != null && hitSound != null)
            {
                _audioSource.clip = hitSound;
                _audioSource.volume = hitVolume;
                _audioSource.Play();
                Invoke("StopEnemySound", 0.5f); 
            }
            StartCoroutine(Die());
        }

        if (HitStopManager.Instance != null) HitStopManager.Instance.Stop(0.03f); 
        StartCoroutine(FlashRed());
        if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.15f);
    }

    private void StopEnemySound()
    {
        if (_audioSource != null) _audioSource.Stop();
    }

    private IEnumerator FlashRed()
    {
        if (_renderers == null || _renderers.Length == 0) yield break;
        MaterialPropertyBlock props = new MaterialPropertyBlock();
        props.SetColor("_Color", Color.red);
        props.SetColor("_BaseColor", Color.red); 
        foreach (var renderer in _renderers) renderer.SetPropertyBlock(props);
        yield return new WaitForSecondsRealtime(_flashDuration);
        foreach (var renderer in _renderers) renderer.SetPropertyBlock(null);
    }

    public void ApplyKnockback(Vector3 force)
    {
        if (_isDead) return;
        StartCoroutine(KnockbackRoutine(force));
    }

    private IEnumerator KnockbackRoutine(Vector3 force)
    {
        _isKnockedBack = true;
        _agent.isStopped = true;
        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            float t = 1f - (elapsed / knockbackDuration);
            transform.position += force * t * knockbackSpeed * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }
        _agent.isStopped = false;
        if (_player != null) _agent.SetDestination(_player.position);
        _isKnockedBack = false;
    }

private IEnumerator Die()
{
    _isDead      = true;
    _isAttacking = false;

    _agent.isStopped = true;
    _agent.velocity  = Vector3.zero;

    animator.SetBool(AnimIsWalking, false);

    yield return null;

    _agent.enabled = false;
    animator.Play("Death");

    WaveManager.Instance.ReportEnemyDeath();

    // register kill for buff system
    if (KillTracker.Instance != null)
        KillTracker.Instance.RegisterKill();

    RollDrop();

    yield return new WaitForSeconds(deathAnimationDuration);
    Destroy(gameObject);
}

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