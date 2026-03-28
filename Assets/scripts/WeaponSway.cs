using UnityEngine;

/// <summary>
/// Attach this to your weapon holder (the child of the camera that holds the gun).
/// It works with an empty GameObject now — just drop the model in as a child later.
///
/// HIERARCHY EXPECTED:
///   [Player]
///     └─ CameraHolder
///          └─ WeaponHolder  ← this script goes here
///               └─ GunModel (can be empty GO for now, replace with mesh later)
/// </summary>
public class WeaponSway : MonoBehaviour
{
    // ----------------------------------------------------------------
    // SWAY — weapon lags behind mouse movement
    // ----------------------------------------------------------------
    [System.Serializable]
    public class SwaySettings
    {
        [Tooltip("How strongly the weapon lags behind mouse movement. 0 = no sway.")]
        public float swayAmount       = 0.04f;

        [Tooltip("Max distance the weapon can drift from center.")]
        public float maxSwayAmount    = 0.12f;

        [Tooltip("How fast the weapon returns to center. Higher = snappier.")]
        public float swaySmoothSpeed  = 8f;

        [Tooltip("Rotational sway in degrees. Weapon tilts as you move the mouse.")]
        public float rotSwayAmount    = 4f;

        [Tooltip("Max degrees of rotational sway.")]
        public float maxRotSwayAmount = 10f;

        [Tooltip("How fast rotation returns to neutral.")]
        public float rotSmoothSpeed   = 8f;
    }

    // ----------------------------------------------------------------
    // BOB — idle breathing + movement bob on the weapon
    // ----------------------------------------------------------------
    [System.Serializable]
    public class BobSettings
    {
        [Header("Idle Breathing")]
        [Tooltip("Slow up/down drift when standing still. Looks like the sailor is breathing.")]
        public float idleAmplitude  = 0.005f;
        public float idleFrequency  = 1.2f;

        [Header("Walk Bob")]
        public float walkAmplitude  = 0.01f;
        public float walkFrequency  = 8f;

        [Header("Sprint Bob")]
        public float sprintAmplitude = 0.02f;
        public float sprintFrequency = 14f;

        [Header("Crouch Bob")]
        public float crouchAmplitude = 0.006f;
        public float crouchFrequency = 5f;

        [Header("Smoothing")]
        [Tooltip("How fast the bob offset blends in.")]
        public float bobSmoothSpeed = 10f;

        [Tooltip("How fast the weapon returns to neutral when stopping.")]
        public float returnSpeed    = 6f;
    }

    // ----------------------------------------------------------------
    // RECOIL — programmatic kick on fire
    // ----------------------------------------------------------------
    [System.Serializable]
    public class RecoilSettings
    {
        [Tooltip("Kick-back distance along Z when firing.")]
        public float kickbackAmount    = 0.05f;

        [Tooltip("Upward positional kick on fire.")]
        public float kickUpAmount      = 0.01f;

        [Tooltip("Rotational kick (pitch up) on fire, in degrees.")]
        public float rotKickAmount     = 3f;

        [Tooltip("How fast the kick is applied (lerp speed towards kicked position).")]
        public float recoilSpeed       = 20f;

        [Tooltip("How fast the weapon recovers to rest after recoil.")]
        public float recoverySpeed     = 6f;
    }

    // ----------------------------------------------------------------
    [Header("Sway")]
    [SerializeField] private SwaySettings sway;

    [Header("Bob")]
    [SerializeField] private BobSettings bob;

    [Header("Recoil")]
    [SerializeField] private RecoilSettings recoil;

    [Header("State (read from your FPS controller)")]
    [Tooltip("Is the player currently moving?")]
    [SerializeField] private bool isMoving   = false;
    [Tooltip("Is the player sprinting?")]
    [SerializeField] private bool isSprinting = false;
    [Tooltip("Is the player crouching?")]
    [SerializeField] private bool isCrouching = false;

    // ----------------------------------------------------------------
    // Runtime state
    // ----------------------------------------------------------------

    // origins
    private Vector3    _restPosition;
    private Quaternion _restRotation;

    // sway
    private Vector3    _swayPos       = Vector3.zero;
    private Quaternion _swayRot       = Quaternion.identity;

    // bob
    private float      _bobTimer      = 0f;
    private Vector3    _bobOffset     = Vector3.zero;
    private Vector3    _bobVelocity   = Vector3.zero;

    // recoil
    private Vector3    _recoilPosTarget   = Vector3.zero;
    private Vector3    _recoilPosCurrent  = Vector3.zero;
    private Quaternion _recoilRotTarget   = Quaternion.identity;
    private Quaternion _recoilRotCurrent  = Quaternion.identity;

    // ----------------------------------------------------------------
    private void Awake()
    {
        _restPosition = transform.localPosition;
        _restRotation = transform.localRotation;
    }

    // ----------------------------------------------------------------
    private void Update()
    {
        UpdateSway();
        UpdateBob();
        UpdateRecoil();
        ApplyAll();
    }

    // ----------------------------------------------------------------
    private void UpdateSway()
    {
        float mouseX = Input.GetAxisRaw("Mouse X");
        float mouseY = Input.GetAxisRaw("Mouse Y");

        // positional sway — weapon drifts opposite to mouse
        float targetX = Mathf.Clamp(-mouseX * sway.swayAmount, -sway.maxSwayAmount, sway.maxSwayAmount);
        float targetY = Mathf.Clamp(-mouseY * sway.swayAmount, -sway.maxSwayAmount, sway.maxSwayAmount);
        Vector3 targetSwayPos = new Vector3(targetX, targetY, 0f);

        _swayPos = Vector3.Lerp(_swayPos, targetSwayPos, Time.deltaTime * sway.swaySmoothSpeed);

        // rotational sway — weapon tilts with the mouse
        float targetRotX = Mathf.Clamp(mouseY  * sway.rotSwayAmount, -sway.maxRotSwayAmount, sway.maxRotSwayAmount);
        float targetRotY = Mathf.Clamp(-mouseX * sway.rotSwayAmount, -sway.maxRotSwayAmount, sway.maxRotSwayAmount);
        // small Z roll feels very natural
        float targetRotZ = Mathf.Clamp(-mouseX * sway.rotSwayAmount * 0.5f, -sway.maxRotSwayAmount, sway.maxRotSwayAmount);
        Quaternion targetSwayRot = Quaternion.Euler(targetRotX, targetRotY, targetRotZ);

        _swayRot = Quaternion.Slerp(_swayRot, targetSwayRot, Time.deltaTime * sway.rotSmoothSpeed);
    }

    // ----------------------------------------------------------------
    private void UpdateBob()
    {
        float amplitude, frequency;

        if (isMoving && isSprinting)
        {
            amplitude = bob.sprintAmplitude;
            frequency = bob.sprintFrequency;
        }
        else if (isMoving && isCrouching)
        {
            amplitude = bob.crouchAmplitude;
            frequency = bob.crouchFrequency;
        }
        else if (isMoving)
        {
            amplitude = bob.walkAmplitude;
            frequency = bob.walkFrequency;
        }
        else
        {
            // idle breathing
            amplitude = bob.idleAmplitude;
            frequency = bob.idleFrequency;
        }

        // advance timer — never reset for idle so breathing is seamless
        if (isMoving)
            _bobTimer += Time.deltaTime * frequency;
        else
            _bobTimer += Time.deltaTime * frequency; // idle also ticks

        // figure-8 bob: X uses cosine so it offsets the Y, giving a slight lateral drift
        float targetBobY = Mathf.Sin(_bobTimer)       * amplitude;
        float targetBobX = Mathf.Cos(_bobTimer * 0.5f) * amplitude * 0.5f;
        Vector3 targetBob = new Vector3(targetBobX, targetBobY, 0f);

        if (isMoving)
        {
            _bobOffset = Vector3.SmoothDamp(_bobOffset, targetBob, ref _bobVelocity,
                1f / bob.bobSmoothSpeed);
        }
        else
        {
            // blend between idle breath and full stop
            _bobOffset = Vector3.SmoothDamp(_bobOffset, targetBob, ref _bobVelocity,
                1f / bob.returnSpeed);
        }
    }

    // ----------------------------------------------------------------
    private void UpdateRecoil()
    {
        // lerp current recoil towards the kicked target (snap forward)
        _recoilPosCurrent = Vector3.Lerp(_recoilPosCurrent, _recoilPosTarget,
            Time.deltaTime * recoil.recoilSpeed);
        _recoilRotCurrent = Quaternion.Slerp(_recoilRotCurrent, _recoilRotTarget,
            Time.deltaTime * recoil.recoilSpeed);

        // then recover target back to neutral (drift back)
        _recoilPosTarget = Vector3.Lerp(_recoilPosTarget, Vector3.zero,
            Time.deltaTime * recoil.recoverySpeed);
        _recoilRotTarget = Quaternion.Slerp(_recoilRotTarget, Quaternion.identity,
            Time.deltaTime * recoil.recoverySpeed);
    }

    // ----------------------------------------------------------------
    // Combine everything and set local transform once per frame
    // ----------------------------------------------------------------
    private void ApplyAll()
    {
        Vector3 finalPos = _restPosition
            + _swayPos
            + _bobOffset
            + _recoilPosCurrent;

        Quaternion finalRot = _restRotation
            * _swayRot
            * _recoilRotCurrent;

        transform.localPosition = finalPos;
        transform.localRotation = finalRot;
    }

    // ----------------------------------------------------------------
    // PUBLIC API — call these from your weapon/player scripts
    // ----------------------------------------------------------------

    /// <summary>
    /// Call this every time the gun fires.
    /// The kick will be applied and automatically recover.
    /// </summary>
    public void ApplyRecoil()
    {
        // push the target to the kicked position
        // each shot stacks on top of the last (clamp if you want a max kick)
        _recoilPosTarget += new Vector3(0f, recoil.kickUpAmount, -recoil.kickbackAmount);
        _recoilRotTarget *= Quaternion.Euler(-recoil.rotKickAmount, 0f, 0f);
    }

    /// <summary>
    /// Call this from your FPS controller each frame, or use the Inspector booleans
    /// for quick testing without writing glue code.
    /// </summary>
    public void SetMovementState(bool moving, bool sprinting, bool crouching)
    {
        isMoving    = moving;
        isSprinting = sprinting;
        isCrouching = crouching;
    }

    /// <summary>Swap the full sway settings at runtime (e.g. ADS vs hip-fire).</summary>
    public void SetSwaySettings(SwaySettings newSettings)   => sway  = newSettings;

    /// <summary>Swap the full bob settings at runtime.</summary>
    public void SetBobSettings(BobSettings newSettings)     => bob   = newSettings;

    /// <summary>Swap the full recoil settings at runtime (e.g. different guns).</summary>
    public void SetRecoilSettings(RecoilSettings newSettings) => recoil = newSettings;
}