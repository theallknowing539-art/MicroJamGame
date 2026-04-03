using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("Spring Settings")]
    [Tooltip("How fast the camera bounces (Snappiness)")]
    [SerializeField] private float frequency = 25f; 
    
    [Tooltip("How fast the camera returns to center")]
    [SerializeField] private float recoverySpeed = 15f; 

    private Vector3 _targetRotation;
    private Vector3 _currentRotation;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        // 1. Smoothly pull the target rotation back toward Zero
        _targetRotation = Vector3.Lerp(_targetRotation, Vector3.zero, recoverySpeed * Time.unscaledDeltaTime);
        
        // 2. Spherically interpolate the current rotation toward that target
        _currentRotation = Vector3.Slerp(_currentRotation, _targetRotation, frequency * Time.unscaledDeltaTime);
        
        // 3. Apply the rotation to the camera
        transform.localRotation = Quaternion.Euler(_currentRotation);
    }

    /// <summary>
    /// Call this from DamageGun.cs when firing.
    /// intensity: 0.1f to 0.5f usually feels best.
    /// </summary>
    public void Shake(float intensity, float duration = 0.05f)
    {
        // Add a "Kick" upward (-X) and random horizontal/roll (Y/Z)
        float kickUp = -intensity * 50f;
        float kickSide = Random.Range(-intensity * 20f, intensity * 20f);
        float kickRoll = Random.Range(-intensity * 10f, intensity * 10f);

        _targetRotation += new Vector3(kickUp, kickSide, kickRoll);
    }
}