using System;
using UnityEngine;

public enum PropertyName {
    GridWidth,
    GridHeight,
    Values
}

/// <summary>
/// A simple Grid Rendering system that drives the Grid Shader. Cell states live in a Texture2D
/// (one texel per cell, state index in the red channel) the shader samples - a StructuredBuffer
/// did the job on desktop but doesn't exist on WebGL, a texture runs everywhere.
/// </summary>
public class GridController : MonoBehaviour {
    public int width;
    public int height;

    // a per-renderer instance of the grid material. everything the code sets (texture, colors,
    // fade) goes here, never to the shared .mat asset - poking the asset from script leaks the
    // runtime values back into it in the editor.
    private Material _material;

    // cell states, one texel each. _pixels is the CPU-side copy we edit, then upload to _texture.
    private Color32[] _pixels;
    private Texture2D _texture;

    // Get all property IDs
    private static readonly int GridTex = Shader.PropertyToID("_GridTex");
    private static readonly int GridWidth = Shader.PropertyToID("_GridWidth");
    private static readonly int GridHeight = Shader.PropertyToID("_GridHeight");
    private static readonly int Fade = Shader.PropertyToID("_Fade");
    private static readonly int Now = Shader.PropertyToID("_Now");
    private static readonly int TransitionTime = Shader.PropertyToID("_TransitionTime");
    private static readonly int LineColor = Shader.PropertyToID("_LineColor");
    private static readonly int EmptyColor = Shader.PropertyToID("_EmptyColor");
    private static readonly int WallColor = Shader.PropertyToID("_WallColor");
    private static readonly int StartColor = Shader.PropertyToID("_StartColor");
    private static readonly int EndColor = Shader.PropertyToID("_EndColor");
    private static readonly int SeenColor = Shader.PropertyToID("_SeenColor");
    private static readonly int ExpandedColor = Shader.PropertyToID("_ExpandedColor");
    private static readonly int PathColor = Shader.PropertyToID("_PathColor");
    private static readonly int HoverColor = Shader.PropertyToID("_HoverColor");
    private static readonly int HoverCell = Shader.PropertyToID("_HoverCell");
    private static readonly int HoverStrength = Shader.PropertyToID("_HoverStrength");
    private static readonly int BrushPreview = Shader.PropertyToID("_BrushPreview");
    public Vector2Int Size => new Vector2Int(width, height);

    // cursor highlight - the cell under the mouse and a strength eased in/out so it doesn't snap.
    // nothing sets these on touch, so the overlay just stays off there.
    private Vector2 _hoverCell = new Vector2(-1, -1);
    private float _hoverTarget;
    private float _hoverStrength;
    private bool _brushPreview;

    private void Awake() {
        // .material instantiates a copy owned by this renderer; released in OnDestroy
        _material = GetComponent<MeshRenderer>().material;
    }

    // the timestamp is 16 bits of 0.02s units, so time wraps here. the shader compares stamps
    // against _Now on the same clock; both sides have to agree on the period.
    private const float TimePeriod = 65536 * 0.02f;

    private void Start() {
        Rebuild(width, height);
    }

    private void Update() {
        // the shader eases cell transitions against this clock, wrapped like the stamps are
        _material.SetFloat(Now, Time.time % TimePeriod);

        _hoverStrength = Mathf.MoveTowards(_hoverStrength, _hoverTarget, Time.deltaTime * 8f);
        _material.SetVector(HoverCell, _hoverCell);
        _material.SetFloat(HoverStrength, _hoverStrength);
        _material.SetFloat(BrushPreview, _brushPreview ? 1f : 0f);
    }

    /// <summary>
    /// Push a palette into the grid material. Deliberately no sRGB-to-linear conversion: uGUI
    /// passes the same theme tokens through as raw vertex colors, so converting here would render
    /// the grid darker than the panels sitting next to it. Raw values keep them on one encode.
    /// </summary>
    public void ApplyTheme(GridTheme theme) {
        _material.SetColor(LineColor, theme.line);
        _material.SetColor(EmptyColor, theme.empty);
        _material.SetColor(WallColor, theme.wall);
        _material.SetColor(StartColor, theme.start);
        _material.SetColor(EndColor, theme.end);
        _material.SetColor(SeenColor, theme.seen);
        _material.SetColor(ExpandedColor, theme.expanded);
        _material.SetColor(PathColor, theme.path);
        _material.SetColor(HoverColor, theme.hover);
    }

    /// <summary>
    /// Highlight the cell under the cursor. brushPreview hints the wall the left button would
    /// draw. Call every frame the cursor is over the grid; ClearHover when it leaves.
    /// </summary>
    public void SetHover(Vector2Int cell, bool brushPreview) {
        _hoverCell = cell;
        _brushPreview = brushPreview;
        _hoverTarget = 1f;
    }

    /// <summary>Fade the cursor highlight out - cursor left the grid or moved onto the UI.</summary>
    public void ClearHover() {
        _hoverTarget = 0f;
    }

    /// <summary>
    /// (Re)allocate the pixel array + state texture for a grid size and push everything to the
    /// shader. The texture is sized to the grid, and resizing destroys the old one first. Call
    /// this to change the grid size at runtime.
    /// </summary>
    public void Rebuild(int newWidth, int newHeight) {
        width = newWidth;
        height = newHeight;
        _pixels = new Color32[width * height];

        if (_texture != null)
            Destroy(_texture);
        // linear:true - we're packing a raw state index in the red byte, not a colour. without it
        // the texture samples as sRGB and the project's linear space gamma-decodes the byte, so the
        // low indices (start/end/walls) collapse to ~0 and the grid comes up blank.
        _texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true) {
            filterMode = FilterMode.Point, // one texel = one cell, never interpolate between states
            wrapMode = TextureWrapMode.Clamp
        };
        _material.SetTexture(GridTex, _texture);

        // Update all Shader properties
        foreach (PropertyName property in Enum.GetValues(typeof(PropertyName)))
            UpdateShader(property);
    }

    /// <summary>
    /// Updates Shader material properties.
    /// </summary>
    /// <param name="property">PropertyName item representing Shader property to be updated</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void UpdateShader(PropertyName property) {
        switch (property) {
            case PropertyName.GridWidth:
                _material.SetFloat(GridWidth, width);
                break;
            case PropertyName.GridHeight:
                _material.SetFloat(GridHeight, height);
                break;
            case PropertyName.Values:
                _texture.SetPixels32(_pixels);
                _texture.Apply(false);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(property), property, null);
        }
    }

    /// <summary>
    /// Global brightness multiplier on the whole grid (1 = full). Used to dim while editing.
    /// </summary>
    public void SetFade(float fade) {
        _material.SetFloat(Fade, fade);
    }

    /// <summary>
    /// How long a cell takes to ease from its previous state to the current one. Scaled down at
    /// high playback speeds so fast sweeps don't smear into each other.
    /// </summary>
    public void SetTransitionTime(float seconds) {
        _material.SetFloat(TransitionTime, seconds);
    }

    private void OnDestroy() {
        if (_texture != null)
            Destroy(_texture);
        if (_material != null)
            Destroy(_material);
    }

    /// <summary>
    /// Loads a whole GridState into the state texture (every cell), then uploads it.
    /// </summary>
    public void LoadGridState(GridNodeType[,] state, bool animate = true) {
        // Loop over matrix and set values via cast Enum to int
        int gridWidth = state.GetLength(0);
        int gridHeight = state.GetLength(1);
        for (int x = 0; x < gridWidth; x++) {
            for (int y = 0; y < gridHeight; y++)
                SetValue(x, y, (int) state[x, y], animate);
        }

        UpdateShader(PropertyName.Values);
    }

    /// <summary>
    /// A more performant method of loading GridState values into the shader.
    /// </summary>
    public void LoadDirtyGridState(GridNodeType[,] state, bool[,] dirtyFlags, bool animate = true) {
        int gridWidth = state.GetLength(0);
        for (int x = 0; x < gridWidth; x++) {
            int gridHeight = state.GetLength(1);
            for (int y = 0; y < gridHeight; y++)
                // only set value if the value has been marked as dirty
                if (dirtyFlags[x, y]) {
                    SetValue(x, y, (int) state[x, y], animate);
                    dirtyFlags[x, y] = false;
                }
        }

        UpdateShader(PropertyName.Values);
    }

    /// <summary>
    /// Packs a cell into its texel: current state in R, the state it came from in G, and when it
    /// changed in BA (16-bit, 0.02s units of Time.time - wraps every ~22 min, the shader handles
    /// the wrap when it computes the age). Only cells whose state actually changed are restamped,
    /// so full repaints don't retrigger animations. animate=false snaps: prev is written equal to
    /// current so there's no transition to play - scrubs and reloads use it, otherwise a seek
    /// would fire hundreds of transitions in one frame.
    /// Per-cell scalar data (a g-cost heatmap, say) would go in a second texture next to this one,
    /// not in here - all four channels are spoken for.
    /// </summary>
    public void SetValue(int x, int y, int value, bool animate = true) {
        int i = width * y + x;
        var v = (byte) value;
        if (_pixels[i].r == v)
            return;
        ushort stamp = (ushort) Mathf.RoundToInt(Time.time * 50f);
        _pixels[i] = new Color32(v, animate ? _pixels[i].r : v, (byte) (stamp & 0xFF), (byte) (stamp >> 8));
    }

    /// <summary>
    /// World position to grid cell, the one mapping every picker (editing, hover, gizmos) goes
    /// through. Returns whether the position actually lands on the grid. The quad's UVs run
    /// opposite to world XY, so cell (0,0) is the far corner - GetWorldPosition mirrors this.
    /// </summary>
    public bool CellAt(Vector3 worldPosition, out Vector2Int cell) {
        Transform t = transform;
        Vector3 local = worldPosition - t.position;
        Vector3 scale = t.localScale;
        // floor, not cast: a click just off the low edge is cell -1, not cell 0 - a truncate
        // wraps it through the flip onto the far column/row and paints a cell you never touched
        int x = width - 1 - Mathf.FloorToInt((local.x / scale.x + 0.5f) * width);
        int y = height - 1 - Mathf.FloorToInt((local.y / scale.y + 0.5f) * height);
        cell = new Vector2Int(x, y);
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    /// <summary>The world position at the center of a grid cell. Inverse of CellAt.</summary>
    public Vector3 GetWorldPosition(Vector2Int gridPosition) {
        Transform t = transform;
        Vector3 scale = t.localScale;
        var cellSize = new Vector2(scale.x / width, scale.y / height);
        // counted back from the far corner, matching CellAt's flip
        float x = (width - 1 - gridPosition.x + 0.5f) * cellSize.x - scale.x / 2f;
        float y = (height - 1 - gridPosition.y + 0.5f) * cellSize.y - scale.y / 2f;
        return t.position + new Vector3(x, y);
    }
}