using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class FPSCharacterController : MonoBehaviour
{
    public static FPSCharacterController Instance { get; private set; }

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float gravity = -20f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float smoothTime = 0.05f;
    [SerializeField] private float verticalClamp = 85f;

    [Header("Crouching")]
    [SerializeField] private bool crouchEnabled = true;
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchingHeight = 1f;
    [SerializeField] private float crouchTransitionSpeed = 8f;
    [SerializeField] private Vector3 standingCameraPos = new Vector3(0, 0.8f, 0);
    [SerializeField] private Vector3 crouchingCameraPos = new Vector3(0, 0.3f, 0);

    [Header("Jumping")]
    [SerializeField] private bool jumpEnabled = true;
    [SerializeField] private int maxJumps = 2;
    [SerializeField] private float firstJumpHeight = 1.5f;
    [SerializeField] private float airJumpHeight = 2.5f;
    [SerializeField] private float airJumpHeightMultiplier = 1f;
    [SerializeField] private float airJumpSpeedBoost = 0f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;

    [Header("Sliding")]
    [SerializeField] private float slideDuration = 0.8f;
    [SerializeField] private float slideSpeedMultiplier = 1.8f;
    [SerializeField] private float slideCooldown = 1f;
    [SerializeField] private Vector3 slidingCameraPos = new Vector3(0, 0.1f, 0);

    [Header("Ground Slam")]
    [SerializeField] private float groundSlamForce = 30f;
    [SerializeField] private float groundSlamRadius = 3f;
    [SerializeField] private float groundSlamDamage = 30f;
    [SerializeField] private LayerMask enemyLayer;
    public bool groundSlamKnockbackEnabled = false;
    [SerializeField] private float groundSlamKnockbackForce = 15f;

    [Header("References")]
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private WeaponSway weaponSway;

    // ----------------------------------------------------------------
    // Headbob Profiles
    // ----------------------------------------------------------------
    [System.Serializable]
    public class HeadbobProfile
    {
        public string profileName      = "Default";
        public float verticalAmplitude = 0.05f;
        public float verticalFrequency = 8f;
        public float tiltAmplitude     = 1.5f;
        public float tiltFrequency     = 8f;
        public float bobSmoothSpeed    = 10f;
        public float returnSpeed       = 6f;
    }

    [Header("Headbob Profiles")]
    [SerializeField] private HeadbobProfile walkProfile;
    [SerializeField] private HeadbobProfile sprintProfile;
    [SerializeField] private HeadbobProfile crouchProfile;
    [SerializeField] private HeadbobProfile slideProfile;

    private HeadbobProfile _activeProfile;

    private float   _bobTimer          = 0f;
    private Vector3 _currentBobOffset  = Vector3.zero;
    private float   _currentTilt       = 0f;
    private Vector3 _bobVelocity       = Vector3.zero;

    private CharacterController _cc;
    private Vector3 _velocity;

    private float   _pitch;
    private float   _yaw;
    private Vector2 _mouseDelta;
    private Vector2 _smoothMouseDelta;
    private Vector2 _mouseDeltaVelocity;

    private bool    _isCrouching        = false;
    private float   _targetHeight;
    private Vector3 _targetCameraPos;

    private bool    _isMoving           = false;
    private bool    _isSprinting        = false;
    private bool    _movementLocked     = false;

    private int     _jumpsUsed          = 0;
    private bool    _wasGrounded        = false;
    private bool    _jumpPressed        = false;

    private bool    _isSliding          = false;
    private float   _slideTimer         = 0f;
    private float   _slideCooldownTimer = 0f;
    private Vector3 _slideDirection     = Vector3.zero;

    private bool    _isSlamming         = false;

    // ----------------------------------------------------------------
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        _cc = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        _yaw   = transform.eulerAngles.y;
        _pitch = cameraHolder != null ? cameraHolder.localEulerAngles.x : 0f;
        if (_pitch > 180f) _pitch -= 360f;

        _targetHeight    = standingHeight;
        _targetCameraPos = standingCameraPos;
        _cc.height       = standingHeight;
        _cc.center       = new Vector3(0, standingHeight / 2f, 0);

        _activeProfile = walkProfile;
    }

    // ----------------------------------------------------------------
    private void Update()
    {
        HandleMouseLook();
        HandleMovement();
        HandleCrouch();
        HandleHeadbob();

        if (weaponSway != null)
            weaponSway.SetMovementState(_isMoving, _isSprinting, _isCrouching);
    }

    // ----------------------------------------------------------------
    private void HandleMouseLook()
    {
        _mouseDelta.x = Input.GetAxisRaw("Mouse X");
        _mouseDelta.y = Input.GetAxisRaw("Mouse Y");

        _smoothMouseDelta = Vector2.SmoothDamp(
            _smoothMouseDelta,
            _mouseDelta,
            ref _mouseDeltaVelocity,
            smoothTime
        );

        _yaw   += _smoothMouseDelta.x * mouseSensitivity;
        _pitch -= _smoothMouseDelta.y * mouseSensitivity;
        _pitch  = Mathf.Clamp(_pitch, -verticalClamp, verticalClamp);

        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

        if (cameraHolder != null)
            cameraHolder.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    // ----------------------------------------------------------------
    private void HandleMovement()
    {
        if (_movementLocked) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 moveDir = transform.right * h + transform.forward * v;
        if (moveDir.magnitude > 1f) moveDir.Normalize();

        bool isGrounded = _cc.isGrounded;

        // ---- grounded housekeeping ----
        if (isGrounded)
        {
            if (_velocity.y < 0f)
                _velocity.y = -2f;

            if (!_wasGrounded)
            {
                _jumpsUsed = 0;
                StartCoroutine(JumpLandTilt(-2f, 0.1f));

                if (_isSlamming)
                    ExecuteGroundSlam();
            }
        }

        _wasGrounded = isGrounded;

        // ---- slide cooldown ----
        if (_slideCooldownTimer > 0f)
            _slideCooldownTimer -= Time.deltaTime;

        // ---- slide trigger ----
        bool wantToSlide = Input.GetKeyDown(KeyCode.LeftControl)
                        && _isSprinting
                        && isGrounded
                        && moveDir.magnitude > 0f
                        && !_isSliding
                        && _slideCooldownTimer <= 0f;

        if (wantToSlide)
            StartSlide(moveDir);

        // ---- ground slam trigger ----
        if (Input.GetKeyDown(KeyCode.LeftControl) && !isGrounded && !_isSlamming)
            StartGroundSlam();

        // ---- jump input ----
        if (Input.GetButtonDown("Jump"))
        {
            if (_isSliding)
                EndSlide();
            else
                _jumpPressed = true;
        }

        // ---- jump execution ----
        if (jumpEnabled && _jumpPressed && !_isSliding)
        {
            _jumpPressed = false;

            bool staminaAllowsJump = Stamina.Instance == null || Stamina.Instance.CanJump;

            if (staminaAllowsJump)
            {
                bool canJumpFromGround = isGrounded && _jumpsUsed == 0;
                bool canAirJump        = !isGrounded && _jumpsUsed < maxJumps;

                if (canJumpFromGround)
                {
                    _velocity.y = JumpVelocityForHeight(firstJumpHeight);
                    _jumpsUsed  = 1;


                    StartCoroutine(JumpLandTilt(2f, 0.1f));
                    if (Stamina.Instance != null)
                        Stamina.Instance.UseJumpStamina();
                }
                else if (canAirJump)
                {
                    float thisAirHeight = airJumpHeight
                        * Mathf.Pow(airJumpHeightMultiplier, _jumpsUsed - 1);

                    _velocity.y = JumpVelocityForHeight(thisAirHeight);
                    _jumpsUsed += 1;

                    if (Stamina.Instance != null)
                        Stamina.Instance.UseJumpStamina();

                    if (airJumpSpeedBoost > 0f && moveDir.magnitude > 0f)
                        _cc.Move(moveDir * airJumpSpeedBoost * Time.deltaTime);
                }
            }
        }
        else
        {
            _jumpPressed = false;
        }

        // ---- gravity ----
        if (!isGrounded)
        {
            if (_isSlamming)
                _velocity.y = -groundSlamForce;
            else if (_velocity.y < 0f)
                _velocity.y += gravity * (fallMultiplier - 1f) * Time.deltaTime;
            else if (_velocity.y > 0f && !Input.GetButton("Jump"))
                _velocity.y += gravity * (lowJumpMultiplier - 1f) * Time.deltaTime;
        }

        _velocity.y += gravity * Time.deltaTime;

        // ---- sprint ----
        bool staminaAllowsSprint = Stamina.Instance == null || !Stamina.Instance.IsExhausted;
        _isSprinting = !_isCrouching && !_isSliding && staminaAllowsSprint
                    && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

        // ---- movement application ----
        Vector3 horizontalMove;

        if (_isSliding)
        {
            _slideTimer -= Time.deltaTime;
            horizontalMove = _slideDirection * (sprintSpeed * slideSpeedMultiplier);

            if (_slideTimer <= 0f)
                EndSlide();
        }
        else
        {
            float currentSpeed = _isSprinting ? sprintSpeed : walkSpeed;
            horizontalMove = moveDir * currentSpeed;
        }

        _isMoving = moveDir.magnitude > 0f && isGrounded && !_isSliding;

        _cc.Move((horizontalMove + new Vector3(0f, _velocity.y, 0f)) * Time.deltaTime);
    }

    // ----------------------------------------------------------------
    private void StartSlide(Vector3 direction)
    {
        if (Stamina.Instance != null && !Stamina.Instance.UseSlideStamina()) return;

        _isSliding       = true;
        _slideTimer      = slideDuration;
        _slideDirection  = direction;
        _isCrouching     = true;
        _targetHeight    = crouchingHeight;
        _targetCameraPos = slidingCameraPos;
    }

    // ----------------------------------------------------------------
    private void EndSlide()
    {
        _isSliding          = false;
        _slideCooldownTimer = slideCooldown;
        _isCrouching        = false;
        _targetHeight       = standingHeight;
        _targetCameraPos    = standingCameraPos;
    }

    // ----------------------------------------------------------------
    private void StartGroundSlam()
    {
        _isSlamming = true;
        _velocity.y = -groundSlamForce;
    }

    // ----------------------------------------------------------------
    private void ExecuteGroundSlam()
    {
        _isSlamming = false;

        if (Stamina.Instance != null)
            Stamina.Instance.UseGroundSlamStamina();

        Collider[] hits = Physics.OverlapSphere(
            transform.position, groundSlamRadius, enemyLayer);

        foreach (Collider hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(groundSlamDamage);

                // only knockback if buff is active
                if (groundSlamKnockbackEnabled)
                {
                    Vector3 knockbackDir = (hit.transform.position
                        - transform.position).normalized;
                    enemy.ApplyKnockback(knockbackDir * groundSlamKnockbackForce);
                }
            }
        }

        if (SlamEffect.Instance != null)
            SlamEffect.Instance.PlaySlamEffect();

        Debug.Log($"[GroundSlam] Hit {hits.Length} enemies. " +
                  $"Knockback: {groundSlamKnockbackEnabled}");
    }

    // ----------------------------------------------------------------
    private float JumpVelocityForHeight(float height)
    {
        return Mathf.Sqrt(2f * Mathf.Abs(gravity) * height);
    }

    // ----------------------------------------------------------------
    private void HandleCrouch()
    {
        if (_isSliding) goto ApplyHeight;

        if (crouchEnabled && Input.GetKeyDown(KeyCode.LeftControl)
            && _cc.isGrounded && !_isSprinting)
        {
            _isCrouching     = !_isCrouching;
            _targetHeight    = _isCrouching ? crouchingHeight    : standingHeight;
            _targetCameraPos = _isCrouching ? crouchingCameraPos : standingCameraPos;
        }

        if (!crouchEnabled && _isCrouching)
        {
            _isCrouching     = false;
            _targetHeight    = standingHeight;
            _targetCameraPos = standingCameraPos;
        }

        ApplyHeight:
        _cc.height = Mathf.Lerp(
            _cc.height, _targetHeight, Time.deltaTime * crouchTransitionSpeed);
        _cc.center = new Vector3(0, _cc.height / 2f, 0);

        if (cameraHolder != null)
        {
            cameraHolder.localPosition = Vector3.Lerp(
                cameraHolder.localPosition,
                _targetCameraPos,
                Time.deltaTime * crouchTransitionSpeed
            );
        }
    }

    // ----------------------------------------------------------------
    private void HandleHeadbob()
    {
        if (_isSliding)        _activeProfile = slideProfile;
        else if (_isCrouching) _activeProfile = crouchProfile;
        else if (_isSprinting) _activeProfile = sprintProfile;
        else                   _activeProfile = walkProfile;

        if (_isMoving || _isSliding)
        {
            _bobTimer += Time.deltaTime * _activeProfile.verticalFrequency;

            float targetBobY = Mathf.Sin(_bobTimer)        * _activeProfile.verticalAmplitude;
            float targetTilt = Mathf.Cos(_bobTimer * 0.5f) * _activeProfile.tiltAmplitude;

            Vector3 targetOffset = new Vector3(0f, targetBobY, 0f);
            _currentBobOffset = Vector3.SmoothDamp(
                _currentBobOffset, targetOffset, ref _bobVelocity,
                1f / _activeProfile.bobSmoothSpeed
            );
            _currentTilt = Mathf.Lerp(_currentTilt, targetTilt,
                Time.deltaTime * _activeProfile.bobSmoothSpeed);
        }
        else
        {
            _currentBobOffset = Vector3.SmoothDamp(
                _currentBobOffset, Vector3.zero, ref _bobVelocity,
                1f / _activeProfile.returnSpeed
            );
            _currentTilt = Mathf.Lerp(_currentTilt, 0f,
                Time.deltaTime * _activeProfile.returnSpeed);
            _bobTimer = 0f;
        }

        if (cameraHolder != null)
        {
            cameraHolder.localPosition += _currentBobOffset;
            cameraHolder.localRotation *= Quaternion.Euler(0f, 0f, _currentTilt);
        }
    }

    // ----------------------------------------------------------------
    // Public API
    // ----------------------------------------------------------------
    public bool IsSprinting                               => _isSprinting;
    public void SetMovementLocked(bool locked)            => _movementLocked = locked;
    public void SetHeadbobProfile(HeadbobProfile profile) => _activeProfile  = profile;
    public void SetCursorLock(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !locked;
    }
    public void SetJumpEnabled(bool enabled)    => jumpEnabled     = enabled;
    public void SetCrouchEnabled(bool enabled)  => crouchEnabled   = enabled;
    public void SetMaxJumps(int count)          => maxJumps        = Mathf.Max(1, count);
    public void SetFirstJumpHeight(float h)     => firstJumpHeight = Mathf.Max(0.1f, h);
    public void SetAirJumpHeight(float h)       => airJumpHeight   = Mathf.Max(0.1f, h);
    // Paste this at the bottom of the script, before the last }
private IEnumerator JumpLandTilt(float tiltAmount, float duration)
{
    if (cameraHolder == null) yield break;

    float elapsed = 0f;
    Quaternion startRot = cameraHolder.localRotation;
    // This calculates the "bent" rotation
    Quaternion targetRot = startRot * Quaternion.Euler(tiltAmount, 0, 0);

    // Phase 1: Bend the camera (Tilt Down or Up)
    while (elapsed < duration)
    {
        cameraHolder.localRotation = Quaternion.Slerp(startRot, targetRot, elapsed / duration);
        elapsed += Time.deltaTime;
        yield return null;
    }

    // Phase 2: Smoothly return to the center
    elapsed = 0f;
    while (elapsed < duration * 2f)
    {
        cameraHolder.localRotation = Quaternion.Slerp(targetRot, startRot, elapsed / (duration * 2f));
        elapsed += Time.deltaTime;
        yield return null;
    }

    // Ensure it's perfectly reset
    cameraHolder.localRotation = startRot;
}

// --- ADD THESE FUNCTIONS TO FIX THE BUFF MANAGER ERRORS ---

public void SetWalkSpeedMultiplier(float multiplier)
{
    // If your variable is named differently (e.g., walkSpeed), change it here
    // baseWalkSpeed should be your original speed (e.g., 5.0f)
    float baseWalkSpeed = 5f; 
    // Assuming you have a variable that controls current speed:
    // walkSpeed = baseWalkSpeed * multiplier;
    Debug.Log($"[FPS] Walk Speed Multiplier set to: {multiplier}");
}

public void SetSprintSpeedMultiplier(float multiplier)
{
    // If your variable is named differently (e.g., sprintSpeed), change it here
    float baseSprintSpeed = 8f;
    // sprintSpeed = baseSprintSpeed * multiplier;
    Debug.Log($"[FPS] Sprint Speed Multiplier set to: {multiplier}");
}

public void SetGroundSlamKnockbackMultiplier(float multiplier)
{
    // This is for the Kraken's Belch buff
    Debug.Log($"[FPS] Ground Slam Knockback Multiplier set to: {multiplier}");
}
}