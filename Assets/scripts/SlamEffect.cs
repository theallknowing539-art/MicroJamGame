// ================================================================
// SlamEffect.cs
// Attach to: any GameObject in the scene
// Requires: a Canvas assigned in the inspector (Screen Space Overlay)
// ================================================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SlamEffect : MonoBehaviour
{
    public static SlamEffect Instance { get; private set; }

    [Header("Canvas")]
    [SerializeField] private Canvas _canvas;

    [Header("Line Settings")]
    [SerializeField] private int _minLineCount = 6;
    [SerializeField] private int _maxLineCount = 12;
    [SerializeField] private float _minLineWidth = 2f;
    [SerializeField] private float _maxLineWidth = 6f;
    [SerializeField] private float _minLineHeight = 100f;
    [SerializeField] private float _maxLineHeight = 400f;
    [SerializeField] private Color _lineColor = Color.white;
    [SerializeField] private float _minOpacity = 0.3f;
    [SerializeField] private float _maxOpacity = 0.9f;

    [Header("Animation")]
    [SerializeField] private float _moveDuration = 0.2f;       // how long lines travel down
    [SerializeField] private float _fadeDuration = 0.15f;      // how long they fade out after
    [SerializeField] private float _moveDistance = 300f;       // how far down they travel

    private bool _isPlaying = false;

    // ----------------------------------------------------------------
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ----------------------------------------------------------------
    public void PlaySlamEffect()
    {
        if (_isPlaying) return;
        StartCoroutine(SlamRoutine());
    }

    // ----------------------------------------------------------------
    private IEnumerator SlamRoutine()
    {
        _isPlaying = true;

        // get canvas dimensions
        RectTransform canvasRect = _canvas.GetComponent<RectTransform>();
        float canvasWidth  = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;

        // generate random number of lines
        int lineCount = Random.Range(_minLineCount, _maxLineCount + 1);

        // store all line rects and images for animation
        List<RectTransform> rects  = new List<RectTransform>();
        List<Image>         images = new List<Image>();
        List<float>         startY = new List<float>();
        List<float>         speeds = new List<float>();

        for (int i = 0; i < lineCount; i++)
        {
            // create GameObject
            GameObject lineObj = new GameObject($"SlamLine_{i}");
            lineObj.transform.SetParent(_canvas.transform, false);

            // add image component
            Image img = lineObj.AddComponent<Image>();

            // random opacity within range
            float opacity = Random.Range(_minOpacity, _maxOpacity);
            img.color = new Color(_lineColor.r, _lineColor.g, _lineColor.b, opacity);

            // setup rect transform
            RectTransform rect = lineObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);     // anchor to top left
            rect.anchorMax = new Vector2(0, 1);     // anchor to top left
            rect.pivot     = new Vector2(0.5f, 1f); // pivot at top center

            // random width and height
            float width  = Random.Range(_minLineWidth,  _maxLineWidth);
            float height = Random.Range(_minLineHeight, _maxLineHeight);
            rect.sizeDelta = new Vector2(width, height);

            // random horizontal position across the screen
            float randomX = Random.Range(0f, canvasWidth);

            // random starting Y — lines start above the screen at different heights
            float randomStartY = Random.Range(-50f, 100f);
            rect.anchoredPosition = new Vector2(randomX, randomStartY);

            // random speed variation so lines dont all move identically
            float speedVariance = Random.Range(0.8f, 1.2f);

            rects.Add(rect);
            images.Add(img);
            startY.Add(randomStartY);
            speeds.Add(speedVariance);
        }

        // move phase — lines shoot downward
        float elapsed = 0f;
        while (elapsed < _moveDuration)
        {
            float t = elapsed / _moveDuration;

            for (int i = 0; i < rects.Count; i++)
            {
                if (rects[i] == null) continue;

                // move down by moveDistance over the duration
                float currentY = Mathf.Lerp(
                    startY[i],
                    startY[i] - _moveDistance * speeds[i],
                    t
                );

                rects[i].anchoredPosition = new Vector2(
                    rects[i].anchoredPosition.x,
                    currentY
                );
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // fade phase — lines fade out
        elapsed = 0f;
        while (elapsed < _fadeDuration)
        {
            float t = 1f - (elapsed / _fadeDuration);

            for (int i = 0; i < images.Count; i++)
            {
                if (images[i] == null) continue;

                Color c = images[i].color;
                images[i].color = new Color(c.r, c.g, c.b, c.a * t);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // destroy all line objects
        for (int i = 0; i < rects.Count; i++)
        {
            if (rects[i] != null)
                Destroy(rects[i].gameObject);
        }

        _isPlaying = false;
    }
}
/*

**Setup in Unity:**
```
Scene
  └── SlamEffectManager (empty GameObject)
        └── SlamEffect.cs
              └── Canvas field → drag a Screen Space Overlay Canvas in

Canvas (Screen Space - Overlay)
  └── nothing inside — lines are created at runtime
```

Make sure the Canvas is **separate** from your main UI Canvas so the lines render on top of everything else. Set its **Sort Order** to a high number like `10` so it draws above your health bar, stamina bar etc.

---

**Inspector values to tune the feel:**
```
Min Line Count    → 6
Max Line Count    → 12
Min Line Width    → 2
Max Line Width    → 5
Min Line Height   → 100
Max Line Height   → 350
Min Opacity       → 0.3
Max Opacity       → 0.9
Move Duration     → 0.15   (fast snap downward)
Fade Duration     → 0.2    (slightly slower fade)
Move Distance     → 400    (how far they travel down)
Line Color        → white*/