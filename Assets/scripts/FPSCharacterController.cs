using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSCharacterController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float gravity = -20f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float smoothTime = 0.05f;
    [SerializeField] private float verticalClamp = 85f;

    [Header("Crouching")]
    [SerializeField] private bool crouchEnabled = true;         // toggle crouch on/off
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchingHeight = 1f;
    [SerializeField] private float crouchTransitionSpeed = 8f;
    [SerializeField] private Vector3 standingCameraPos = new Vector3(0, 0.8f, 0);
    [SerializeField] private Vector3 crouchingCameraPos = new Vector3(0, 0.3f, 0);

    // ----------------------------------------------------------------
    // JUMP SETTINGS
    // ----------------------------------------------------------------
    [Header("Jumping")]
    [SerializeField] private bool jumpEnabled = true;           // toggle jump on/off

    [Tooltip("Max number of jumps before landing. 1 = normal, 2 = double-jump (Ultrakill style), etc.")]
    [SerializeField] private int maxJumps = 2;

    [Tooltip("Height of the FIRST jump (ground jump), in Unity units.")]
    [SerializeField] private float firstJumpHeight = 1.5f;

    [Tooltip("Height of subsequent air jumps. Can be higher than firstJumpHeight for momentum builds.")]
    [SerializeField] private float airJumpHeight = 2.5f;

    [Tooltip("Multiply jump height by this per each additional air jump (>1 = escalating, <1 = diminishing).")]
    [SerializeField] private float airJumpHeightMultiplier = 1f;

    [Tooltip("Extra horizontal speed burst applied when jumping in the air (feels like Ultrakill momentum).")]
    [SerializeField] private float airJumpSpeedBoost = 0f;

    [Tooltip("How strongly gravity pulls down during a jump. Increase for snappier arcs.")]
    [SerializeField] private float fallMultiplier = 2.5f;

    [Tooltip("Lower gravity while holding jump for floatier ascent (0 = no effect).")]
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
    private float   _bobTimer        = 0f;
    private Vector3 _currentBobOffset = Vector3.zero;
    private float   _currentTilt     = 0f;
    private Vector3 _bobVelocity     = Vector3.zero;

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
    private bool _isMoving    = false;
    private bool _isSprinting = false;

    // jump state
    private int  _jumpsUsed     = 0;        // how many jumps we've consumed this airtime
    private bool _wasGrounded   = false;    // for landing detection
    private bool _jumpPressed   = false;    // buffered jump input

    // ----------------------------------------------------------------
    private void Awake()
    {
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

            // reset jump counter when we land
            if (!_wasGrounded)
                _jumpsUsed = 0;
        }
        _wasGrounded = isGrounded;

        // ---- jump input (buffer one frame so it doesn't get eaten) ----
        if (Input.GetButtonDown("Jump"))
            _jumpPressed = true;

        // ---- jump execution ----
        if (jumpEnabled && _jumpPressed)
        {
            _jumpPressed = false;

            bool canJumpFromGround  = isGrounded && _jumpsUsed == 0;
            bool canAirJump         = !isGrounded && _jumpsUsed < maxJumps;

            if (canJumpFromGround)
            {
                // standard ground jump — derive velocity from desired height
                // v = sqrt(2 * |g| * h)
                _velocity.y  = JumpVelocityForHeight(firstJumpHeight);
                _jumpsUsed   = 1;
            }
            else if (canAirJump)
            {
                // each successive air jump can scale up
                float thisAirHeight = airJumpHeight
                    * Mathf.Pow(airJumpHeightMultiplier, _jumpsUsed - 1);

                // override vertical velocity so every air jump feels powerful
                _velocity.y  = JumpVelocityForHeight(thisAirHeight);
                _jumpsUsed  += 1;

                // optional horizontal boost (Ultrakill-style momentum)
                if (airJumpSpeedBoost > 0f && moveDir.magnitude > 0f)
                {
                    Vector3 boost = moveDir * airJumpSpeedBoost;
                    _cc.Move(boost * Time.deltaTime);
                }
            }
        }
        else
        {
            _jumpPressed = false;
        }

        // ---- better-feeling gravity (fast-fall + low-jump hold) ----
        if (!isGrounded)
        {
            if (_velocity.y < 0f)
            {
                // falling — multiply gravity for snappier arcs
                _velocity.y += gravity * (fallMultiplier - 1f) * Time.deltaTime;
            }
            else if (_velocity.y > 0f && !Input.GetButton("Jump"))
            {
                // rising but not holding jump — cut the arc short
                _velocity.y += gravity * (lowJumpMultiplier - 1f) * Time.deltaTime;
            }
        }

        // ---- standard gravity ----
        _velocity.y += gravity * Time.deltaTime;

        // ---- speed / sprint ----
        _isMoving    = moveDir.magnitude > 0f && isGrounded;
        _isSprinting = !_isCrouching && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        float currentSpeed = _isSprinting ? sprintSpeed : walkSpeed;

        _cc.Move((moveDir * currentSpeed + new Vector3(0f, _velocity.y, 0f)) * Time.deltaTime);
    }

    // ----------------------------------------------------------------
    // Converts a desired jump height to the required initial velocity.
    // Formula: v = sqrt(2 * |gravity| * height)
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

        // if crouch was disabled mid-crouch, stand back up
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

            float targetBobY = Mathf.Sin(_bobTimer)       * _activeProfile.verticalAmplitude;
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

    /// <summary>Override the active headbob profile from another script.</summary>
    public void SetHeadbobProfile(HeadbobProfile profile)  => _activeProfile = profile;

    /// <summary>Lock or unlock the cursor.</summary>
    public void SetCursorLock(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !locked;
    }

    /// <summary>Enable or disable jumping at runtime (e.g. cinematic cutscenes).</summary>
    public void SetJumpEnabled(bool enabled)   => jumpEnabled   = enabled;

    /// <summary>Enable or disable crouching at runtime.</summary>
    public void SetCrouchEnabled(bool enabled) => crouchEnabled = enabled;

    /// <summary>Change max jumps at runtime (drunk powerup, bouncy deck, etc.).</summary>
    public void SetMaxJumps(int count)         => maxJumps = Mathf.Max(1, count);

    /// <summary>Change first jump height at runtime.</summary>
    public void SetFirstJumpHeight(float h)    => firstJumpHeight = Mathf.Max(0.1f, h);

    /// <summary>Change air jump height at runtime.</summary>
    public void SetAirJumpHeight(float h)      => airJumpHeight = Mathf.Max(0.1f, h);
}