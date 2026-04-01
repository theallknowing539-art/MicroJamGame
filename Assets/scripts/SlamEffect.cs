// ================================================================
// SlamEffect.cs
// Attach to: Main Camera
// ================================================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlamEffect : MonoBehaviour
{
    public static SlamEffect Instance { get; private set; }

    [Header("Line Settings")]
    [SerializeField] private int _lineCount = 8;
    [SerializeField] private float _lineWidth = 3f;
    [SerializeField] private float _expandDuration = 0.07f;
    [SerializeField] private float _fadeDuration = 0.2f;
    [SerializeField] private Color _lineColor = Color.white;

    private class SlamLine
    {
        public Vector2 start;
        public Vector2 end;
        public float alpha;
    }

    private List<SlamLine> _activeLines = new List<SlamLine>();
    private Material _lineMaterial;
    private bool _isPlaying = false;

    // ----------------------------------------------------------------
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        _lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
        _lineMaterial.hideFlags = HideFlags.HideAndDontSave;
        _lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        _lineMaterial.SetInt("_Cull",     (int)UnityEngine.Rendering.CullMode.Off);
        _lineMaterial.SetInt("_ZWrite", 0);
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
        _activeLines.Clear();

        // expand phase — lines shoot from top to bottom
        float elapsed = 0f;
        while (elapsed < _expandDuration)
        {
            float t = elapsed / _expandDuration;
            _activeLines.Clear();

            for (int i = 0; i < _lineCount; i++)
            {
                // evenly space lines horizontally across the screen
                float xPos = (i + 0.5f) / _lineCount;

                // start at top, shoot downward
                float startY = 1f;
                float endY   = Mathf.Lerp(0.9f, 0f, t);

                _activeLines.Add(new SlamLine
                {
                    start = new Vector2(xPos, startY),
                    end   = new Vector2(xPos, endY),
                    alpha = 1f
                });
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // fade phase
        elapsed = 0f;
        while (elapsed < _fadeDuration)
        {
            float alpha = 1f - (elapsed / _fadeDuration);

            foreach (SlamLine line in _activeLines)
                line.alpha = alpha;

            elapsed += Time.deltaTime;
            yield return null;
        }

        _activeLines.Clear();
        _isPlaying = false;
    }

    // ----------------------------------------------------------------
    private void OnPostRender()
    {
        if (_activeLines.Count == 0) return;

        _lineMaterial.SetPass(0);

        GL.PushMatrix();
        GL.LoadOrtho();

        GL.Begin(GL.QUADS);

        foreach (SlamLine line in _activeLines)
        {
            Color c = new Color(_lineColor.r, _lineColor.g, _lineColor.b, line.alpha);
            GL.Color(c);

            // perpendicular to line direction for width
            Vector2 dir      = (line.end - line.start).normalized;
            Vector2 perp     = new Vector2(-dir.y, dir.x);
            Vector2 halfWidth = perp * (_lineWidth / Screen.height);

            GL.Vertex3(line.start.x - halfWidth.x, line.start.y - halfWidth.y, 0);
            GL.Vertex3(line.start.x + halfWidth.x, line.start.y + halfWidth.y, 0);
            GL.Vertex3(line.end.x   + halfWidth.x, line.end.y   + halfWidth.y, 0);
            GL.Vertex3(line.end.x   - halfWidth.x, line.end.y   - halfWidth.y, 0);
        }

        GL.End();
        GL.PopMatrix();
    }
}