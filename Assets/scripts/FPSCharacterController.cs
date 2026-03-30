using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSCharacterController : MonoBehaviour
{
    // ----------------------------------------------------------------
    // Singleton
    // ----------------------------------------------------------------
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
    [SerializeField] private float slideSpeedMultiplier = 1.8f;     // how much faster than sprint
    [SerializeField] private float slideCooldown = 1f;              // prevent spam
    [SerializeField] private Vector3 slidingCameraPos = new Vector3(0, 0.1f, 0);

    [Header("Ground Slam")]
    [SerializeField] private float groundSlamForce = 30f;           // downward velocity applied
    [SerializeField] private float groundSlamRadius = 3f;           // radius for enemy knockback
    [SerializeField] private float groundSlamDamage = 30f;          // damage dealt to nearby enemies
    [SerializeField] private LayerMask enemyLayer;

    [Header("References")]
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private WeaponSway weaponSway;

    // ----------------------------------------------------------------
    // Headbob Profiles
    // ----------------------------------------------------------------
    [System.Serializable]
    public class HeadbobProfile
    {
        public string profileName = "Default";

        [Header("Vertical Bob")]
        public float verticalAmplitude = 0.05f;
        public float verticalFrequency = 8f;

        [Header("Tilt")]
        public float tiltAmplitude = 1.5f;
        public float tiltFrequency = 8f;

        [Header("Smoothing")]
        public float bobSmoothSpeed = 10f;
        public float returnSpeed = 6f;
    }

    [Header("Headbob Profiles")]
    [SerializeField] private HeadbobProfile walkProfile;
    [SerializeField] private HeadbobProfile sprintProfile;
    [SerializeField] private HeadbobProfile crouchProfile;
    [SerializeField] private HeadbobProfile slideProfile;

    private HeadbobProfile _activeProfile;

    // headbob state
    private float   _bobTimer         = 0f;
    private Vector3 _currentBobOffset = Vector3.zero;
    private float   _currentTilt      = 0f;
    private Vector3 _bobVelocity      = Vector3.zero;

    // --- private core ---
    private CharacterController _cc;
    private Vector3 _velocity;

    // mouse look
    private float   _pitch;
    private float   _yaw;
    private Vector2 _mouseDelta;
    private Vector2 _smoothMouseDelta;
    private Vector2 _mouseDeltaVelocity;

    // crouching
    private bool    _isCrouching      = false;
    private float   _targetHeight;
    private Vector3 _targetCameraPos;

    // movement state
    private bool    _isMoving         = false;
    private bool    _isSprinting      = false;
    private bool    _movementLocked   = false;

    // jump state
    private int     _jumpsUsed        = 0;
    private bool    _wasGrounded      = false;
    private bool    _jumpPressed      = false;

    // slide state
    private bool    _isSliding        = false;
    private float   _slideTimer       = 0f;
    private float   _slideCooldownTimer = 0f;
    private Vector3 _slideDirection   = Vector3.zero;

    // ground slam state
    private bool    _isSlamming       = false;
    private bool    _wasAirborne      = false;

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

        // pass movement state to weapon sway every frame
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

                // just landed — check if we were slamming
                if (_isSlamming)
                    ExecuteGroundSlam();
            }
        }

        // track airborne state for slam landing
        _wasAirborne = !isGrounded;
        _wasGrounded = isGrounded;

        // ---- slide cooldown tick ----
        if (_slideCooldownTimer > 0f)
            _slideCooldownTimer -= Time.deltaTime;

        // ---- slide trigger ----
        // sprint + crouch while grounded and moving and not already sliding
        bool wantToSlide = Input.GetKeyDown(KeyCode.LeftControl)
                        && _isSprinting
                        && isGrounded
                        && moveDir.magnitude > 0f
                        && !_isSliding
                        && _slideCooldownTimer <= 0f;

        if (wantToSlide)
            StartSlide(moveDir);

        // ---- ground slam trigger ----
        // crouch key while airborne and not already slamming
        if (Input.GetKeyDown(KeyCode.LeftControl) && !isGrounded && !_isSlamming)
            StartGroundSlam();

        // ---- jump input ----
        if (Input.GetButtonDown("Jump"))
        {
            // cancel slide on jump
            if (_isSliding)
                EndSlide();
            else
                _jumpPressed = true;
        }

        // ---- jump execution ----
        if (jumpEnabled && _jumpPressed && !_isSliding)
        {
            _jumpPressed = false;

            bool canJumpFromGround = isGrounded && _jumpsUsed == 0;
            bool canAirJump        = !isGrounded && _jumpsUsed < maxJumps;

            if (canJumpFromGround)
            {
                _velocity.y = JumpVelocityForHeight(firstJumpHeight);
                _jumpsUsed  = 1;
            }
            else if (canAirJump)
            {
                float thisAirHeight = airJumpHeight
                    * Mathf.Pow(airJumpHeightMultiplier, _jumpsUsed - 1);

                _velocity.y = JumpVelocityForHeight(thisAirHeight);
                _jumpsUsed += 1;

                if (airJumpSpeedBoost > 0f && moveDir.magnitude > 0f)
                    _cc.Move(moveDir * airJumpSpeedBoost * Time.deltaTime);
            }
        }
        else
        {
            _jumpPressed = false;
        }

        // ---- gravity ----
        if (!isGrounded)
        {
            // during ground slam fall much faster
            float gravMultiplier = _isSlamming ? groundSlamForce / Mathf.Abs(gravity) : 1f;

            if (_velocity.y < 0f)
                _velocity.y += gravity * (fallMultiplier - 1f) * gravMultiplier * Time.deltaTime;
            else if (_velocity.y > 0f && !Input.GetButton("Jump"))
                _velocity.y += gravity * (lowJumpMultiplier - 1f) * Time.deltaTime;
        }

        _velocity.y += gravity * Time.deltaTime;

        // ---- movement application ----
        _isSprinting = !_isCrouching && !_isSliding
                    && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

        Vector3 horizontalMove;

        if (_isSliding)
        {
            // during slide ignore input — commit to slide direction
            _slideTimer -= Time.deltaTime;
            float slideSpeed = sprintSpeed * slideSpeedMultiplier;
            horizontalMove = _slideDirection * slideSpeed;

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
        _isSliding       = true;
        _slideTimer      = slideDuration;
        _slideDirection  = direction;

        // crouch the collider down for the slide
        _isCrouching     = true;
        _targetHeight    = crouchingHeight;
        _targetCameraPos = slidingCameraPos;   // even lower than crouch
    }

    // ----------------------------------------------------------------
    private void EndSlide()
    {
        _isSliding            = false;
        _slideCooldownTimer   = slideCooldown;
        _isCrouching          = false;
        _targetHeight         = standingHeight;
        _targetCameraPos      = standingCameraPos;
    }

    // ----------------------------------------------------------------
    private void StartGroundSlam()
    {
        _isSlamming = true;
        // slam the velocity downward hard
        _velocity.y = -groundSlamForce;
    }

    // ----------------------------------------------------------------
    private void ExecuteGroundSlam()
    {
        _isSlamming = false;

        // find all enemies in radius and damage them
        Collider[] hits = Physics.OverlapSphere(transform.position, groundSlamRadius, enemyLayer);
        foreach (Collider hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null)
                enemy.TakeDamage(groundSlamDamage);
        }

        Debug.Log($"[GroundSlam] Hit {hits.Length} enemies in radius {groundSlamRadius}.");
    }

    // ----------------------------------------------------------------
    private float JumpVelocityForHeight(float height)
    {
        return Mathf.Sqrt(2f * Mathf.Abs(gravity) * height);
    }

    // ----------------------------------------------------------------
    private void HandleCrouch()
    {
        // crouching is handled by slide state when sliding
        if (_isSliding) goto ApplyHeight;

        if (crouchEnabled && Input.GetKeyDown(KeyCode.LeftControl) && _cc.isGrounded && !_isSprinting)
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
        _cc.height = Mathf.Lerp(_cc.height, _targetHeight, Time.deltaTime * crouchTransitionSpeed);
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
}