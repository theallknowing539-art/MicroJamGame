using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SlamEffect : MonoBehaviour
{
    public static SlamEffect Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Canvas _canvas;
    [SerializeField] private Transform _cameraTransform; // Drag Main Camera here

    [Header("Shake Settings")]
    [SerializeField] private float _shakeDuration = 0.3f;
    [SerializeField] private float _shakeMagnitude = 0.2f;

    [Header("Line Settings")]
    [SerializeField] private int _minLineCount = 8;
    [SerializeField] private int _maxLineCount = 15;
    [SerializeField] private Color _lineColor = Color.white;

    [Header("Animation")]
    [SerializeField] private float _moveDuration = 0.15f;
    [SerializeField] private float _fadeDuration = 0.2f;
    [SerializeField] private float _moveDistance = 500f;

    private bool _isPlaying = false;
    private Vector3 _originalCamPos;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        if (_cameraTransform == null) _cameraTransform = Camera.main.transform;

        if (_cameraTransform == null)
    {
        if (Camera.main != null)
            _cameraTransform = Camera.main.transform;
        else
            Debug.LogError("[SlamEffect] No Main Camera found in scene!");
    }
    }

    public void PlaySlamEffect()
    {
        if (_isPlaying) return;
        _originalCamPos = _cameraTransform.localPosition;
        StartCoroutine(SlamRoutine());
        StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;
        while (elapsed < _shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * _shakeMagnitude;
            float y = Random.Range(-1f, 1f) * _shakeMagnitude;

            _cameraTransform.localPosition = _originalCamPos + new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        _cameraTransform.localPosition = _originalCamPos;
    }

    private IEnumerator SlamRoutine()
    {
        _isPlaying = true;
        
        // --- Speed Line Logic (Existing) ---
        RectTransform canvasRect = _canvas.GetComponent<RectTransform>();
        float canvasWidth = canvasRect.rect.width;
        int lineCount = Random.Range(_minLineCount, _maxLineCount + 1);

        List<RectTransform> rects = new List<RectTransform>();
        List<Image> images = new List<Image>();

        for (int i = 0; i < lineCount; i++)
        {
            GameObject lineObj = new GameObject("SlamLine");
            lineObj.transform.SetParent(_canvas.transform, false);
            Image img = lineObj.AddComponent<Image>();
            img.color = _lineColor;

            RectTransform rect = lineObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            float width = Random.Range(2f, 6f);
            float height = Random.Range(150f, 400f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(Random.Range(0f, canvasWidth), Random.Range(-50f, 100f));

            rects.Add(rect);
            images.Add(img);
        }

        // Animation Move
        float elapsed = 0f;
while (elapsed < _moveDuration)
{
    // Use Vector2.down instead of Vector3.down to match anchoredPosition
    float moveStep = (_moveDistance * Time.deltaTime) / _moveDuration;
    
    foreach (var r in rects) 
    {
        if (r != null)
            r.anchoredPosition += Vector2.down * moveStep;
    }
    
    elapsed += Time.deltaTime;
    yield return null;
}

        // Fade out and cleanup
        elapsed = 0f;
        while (elapsed < _fadeDuration)
        {
            float alpha = 1f - (elapsed / _fadeDuration);
            foreach (var img in images) img.color = new Color(_lineColor.r, _lineColor.g, _lineColor.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (var r in rects) if(r != null) Destroy(r.gameObject);
        _isPlaying = false;
    }
}