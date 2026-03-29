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

    [Header("References")]
    [SerializeField] private Transform cameraHolder;

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
    private bool    _isCrouching     = false;
    private float   _targetHeight;
    private Vector3 _targetCameraPos;

    // movement state
    private bool _isMoving      = false;
    private bool _isSprinting   = false;
    private bool _movementLocked = false;

    // jump state
    private int  _jumpsUsed   = 0;
    private bool _wasGrounded = false;
    private bool _jumpPressed = false;

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
    }

    // ----------------------------------------------------------------
    private void HandleMouseLook()
    {
        // mouse look still works during hangover so the player
        // can look around and feel the disorientation
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
        // movement is locked during hangover stagger
        if (_movementLocked) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 moveDir = transform.right * h + transform.forward * v;
        if (moveDir.magnitude > 1f) moveDir.Normalize();

        bool isGrounded = _cc.isGrounded;

        if (isGrounded)
        {
            if (_velocity.y < 0f)
                _velocity.y = -2f;

            if (!_wasGrounded)
                _jumpsUsed = 0;
        }
        _wasGrounded = isGrounded;

        if (Input.GetButtonDown("Jump"))
            _jumpPressed = true;

        if (jumpEnabled && _jumpPressed)
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

        if (!isGrounded)
        {
            if (_velocity.y < 0f)
                _velocity.y += gravity * (fallMultiplier - 1f) * Time.deltaTime;
            else if (_velocity.y > 0f && !Input.GetButton("Jump"))
                _velocity.y += gravity * (lowJumpMultiplier - 1f) * Time.deltaTime;
        }

        _velocity.y += gravity * Time.deltaTime;

        _isMoving    = moveDir.magnitude > 0f && isGrounded;
        _isSprinting = !_isCrouching && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        float currentSpeed = _isSprinting ? sprintSpeed : walkSpeed;

        _cc.Move((moveDir * currentSpeed + new Vector3(0f, _velocity.y, 0f)) * Time.deltaTime);
    }

    // ----------------------------------------------------------------
    private float JumpVelocityForHeight(float height)
    {
        return Mathf.Sqrt(2f * Mathf.Abs(gravity) * height);
    }

    // ----------------------------------------------------------------
    private void HandleCrouch()
    {
        if (crouchEnabled && Input.GetKeyDown(KeyCode.LeftControl))
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
        if (_isCrouching)      _activeProfile = crouchProfile;
        else if (_isSprinting) _activeProfile = sprintProfile;
        else                   _activeProfile = walkProfile;

        if (_isMoving)
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
    public void SetMovementLocked(bool locked)   => _movementLocked  = locked;
    public void SetHeadbobProfile(HeadbobProfile profile) => _activeProfile = profile;
    public void SetCursorLock(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !locked;
    }
    public void SetJumpEnabled(bool enabled)     => jumpEnabled      = enabled;
    public void SetCrouchEnabled(bool enabled)   => crouchEnabled    = enabled;
    public void SetMaxJumps(int count)           => maxJumps         = Mathf.Max(1, count);
    public void SetFirstJumpHeight(float h)      => firstJumpHeight  = Mathf.Max(0.1f, h);
    public void SetAirJumpHeight(float h)        => airJumpHeight    = Mathf.Max(0.1f, h);
}