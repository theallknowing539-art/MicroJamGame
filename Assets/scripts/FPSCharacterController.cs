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
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchingHeight = 1f;
    [SerializeField] private float crouchTransitionSpeed = 8f;
    [SerializeField] private Vector3 standingCameraPos = new Vector3(0, 0.8f, 0);
    [SerializeField] private Vector3 crouchingCameraPos = new Vector3(0, 0.3f, 0);

    [Header("References")]
    [SerializeField] private Transform cameraHolder;

    // ----------------------------------------------------------------
    // Headbob profile — one of these exists per movement state
    // You can create as many as you want and swap them in from
    // other scripts for your game mechanic
    // ----------------------------------------------------------------
    [System.Serializable]
    public class HeadbobProfile
    {
        public string profileName = "Default";

        [Header("Vertical Bob")]
        public float verticalAmplitude = 0.05f;     // how far up/down
        public float verticalFrequency = 8f;         // how fast up/down

        [Header("Tilt")]
        public float tiltAmplitude = 1.5f;           // degrees of z tilt
        public float tiltFrequency = 8f;             // how fast the tilt oscillates

        [Header("Smoothing")]
        public float bobSmoothSpeed = 10f;           // how snappy the bob is
        public float returnSpeed = 6f;               // how fast it resets when standing still
    }

    [Header("Headbob Profiles")]
    [SerializeField] private HeadbobProfile walkProfile;
    [SerializeField] private HeadbobProfile sprintProfile;
    [SerializeField] private HeadbobProfile crouchProfile;

    // current active profile — can be set from outside for game mechanics
    private HeadbobProfile _activeProfile;

    // headbob state
    private float _bobTimer = 0f;
    private Vector3 _currentBobOffset = Vector3.zero;
    private float _currentTilt = 0f;
    private Vector3 _bobVelocity = Vector3.zero;

    // --- private ---
    private CharacterController _cc;
    private Vector3 _velocity;

    // mouse look
    private float _pitch;
    private float _yaw;
    private Vector2 _mouseDelta;
    private Vector2 _smoothMouseDelta;
    private Vector2 _mouseDeltaVelocity;

    // crouching
    private bool _isCrouching = false;
    private float _targetHeight;
    private Vector3 _targetCameraPos;

    // movement state
    private bool _isMoving = false;
    private bool _isSprinting = false;

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

        // default active profile
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

        _isMoving   = moveDir.magnitude > 0f && _cc.isGrounded;
        _isSprinting = !_isCrouching && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        float currentSpeed = _isSprinting ? sprintSpeed : walkSpeed;

        if (_cc.isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;

        _velocity.y += gravity * Time.deltaTime;

        _cc.Move((moveDir * currentSpeed + new Vector3(0f, _velocity.y, 0f)) * Time.deltaTime);
    }

    // ----------------------------------------------------------------
    private void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            _isCrouching     = !_isCrouching;
            _targetHeight    = _isCrouching ? crouchingHeight    : standingHeight;
            _targetCameraPos = _isCrouching ? crouchingCameraPos : standingCameraPos;
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
        // pick the right profile based on movement state
        if (_isCrouching)       _activeProfile = crouchProfile;
        else if (_isSprinting)  _activeProfile = sprintProfile;
        else                    _activeProfile = walkProfile;

        if (_isMoving)
        {
            // advance the bob timer — drives the sine wave
            _bobTimer += Time.deltaTime * _activeProfile.verticalFrequency;

            // vertical bob — sine wave on Y
            float targetBobY = Mathf.Sin(_bobTimer) * _activeProfile.verticalAmplitude;

            // tilt — cosine so it's offset from the vertical, feels natural
            float targetTilt = Mathf.Cos(_bobTimer * 0.5f) * _activeProfile.tiltAmplitude;

            // smooth towards target
            Vector3 targetOffset = new Vector3(0f, targetBobY, 0f);
            _currentBobOffset = Vector3.SmoothDamp(
                _currentBobOffset,
                targetOffset,
                ref _bobVelocity,
                1f / _activeProfile.bobSmoothSpeed
            );
            _currentTilt = Mathf.Lerp(_currentTilt, targetTilt, Time.deltaTime * _activeProfile.bobSmoothSpeed);
        }
        else
        {
            // smoothly return to neutral when not moving
            _currentBobOffset = Vector3.SmoothDamp(
                _currentBobOffset,
                Vector3.zero,
                ref _bobVelocity,
                1f / _activeProfile.returnSpeed
            );
            _currentTilt = Mathf.Lerp(_currentTilt, 0f, Time.deltaTime * _activeProfile.returnSpeed);

            // reset timer so bob always starts from the same point
            _bobTimer = 0f;
        }

        // apply bob offset and tilt on top of whatever the camera holder is already doing
        if (cameraHolder != null)
        {
            cameraHolder.localPosition += _currentBobOffset;
            cameraHolder.localRotation *= Quaternion.Euler(0f, 0f, _currentTilt);
        }
    }

    // ----------------------------------------------------------------
    // Call this from any other script to override the active bob profile
    // This is your hook for the game mechanic
    public void SetHeadbobProfile(HeadbobProfile profile)
    {
        _activeProfile = profile;
    }

    // ----------------------------------------------------------------
    public void SetCursorLock(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !locked;
    }
}