using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("Spring Settings (Rotation/Recoil)")]
    [SerializeField] private float frequency = 25f; 
    [SerializeField] private float recoverySpeed = 15f; 

    [Header("Hit Shake Settings (Position)")]
    [SerializeField] private float positionShakeMultiplier = 1.0f;
    private float _posShakeTimer;
    private float _posShakeIntensity;
    private Vector3 _originalPos;

    private Vector3 _targetRotation;
    private Vector3 _currentRotation;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _originalPos = transform.localPosition;
    }

    private void Update()
    {
        // --- 1. ROTATION SHAKE (Your original Recoil logic) ---
        _targetRotation = Vector3.Lerp(_targetRotation, Vector3.zero, recoverySpeed * Time.unscaledDeltaTime);
        _currentRotation = Vector3.Slerp(_currentRotation, _targetRotation, frequency * Time.unscaledDeltaTime);
        transform.localRotation = Quaternion.Euler(_currentRotation);

        // --- 2. POSITION SHAKE (New Hit-Impact logic) ---
        if (_posShakeTimer > 0)
        {
            // Use Perlin noise for a high-quality "shiver" effect
            float x = (Mathf.PerlinNoise(Time.unscaledTime * 25f, 0f) - 0.5f) * _posShakeIntensity;
            float y = (Mathf.PerlinNoise(0f, Time.unscaledTime * 25f) - 0.5f) * _posShakeIntensity;
            
            transform.localPosition = _originalPos + new Vector3(x, y, 0);
            _posShakeTimer -= Time.unscaledDeltaTime;
        }
        else
        {
            transform.localPosition = _originalPos;
        }
    }

    // This handles the Rotation (Recoil)
    public void Shake(float intensity, float duration = 0.05f)
    {
        float kickUp = -intensity * 50f;
        float kickSide = Random.Range(-intensity * 20f, intensity * 20f);
        float kickRoll = Random.Range(-intensity * 10f, intensity * 10f);

        _targetRotation += new Vector3(kickUp, kickSide, kickRoll);
    }

    // ADD THIS NEW FUNCTION: This handles the Physical Shiver (Hit Impact)
    public void HitShake(float intensity, float duration)
{
    // Force these to be high for testing
    _posShakeIntensity = 0.5f; 
    _posShakeTimer = 0.2f;
    Debug.Log("Shake Triggered!"); // Check your console for this!
}
}