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
    public Material gridMaterial; // Maintain reference to the Grid Material the Shader is implanted upon
    public int width;
    public int height;

    // cell states, one texel each. _pixels is the CPU-side copy we edit, then upload to _texture.
    private Color32[] _pixels;
    private Texture2D _texture;

    // Get all property IDs
    private static readonly int GridTex = Shader.PropertyToID("_GridTex");
    private static readonly int GridWidth = Shader.PropertyToID("_GridWidth");
    private static readonly int GridHeight = Shader.PropertyToID("_GridHeight");
    private static readonly int Fade = Shader.PropertyToID("_Fade");
    public Vector2Int Size => new Vector2Int(width, height);

    private void Start() {
        Rebuild(width, height);
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
        gridMaterial.SetTexture(GridTex, _texture);

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
                gridMaterial.SetFloat(GridWidth, width);
                break;
            case PropertyName.GridHeight:
                gridMaterial.SetFloat(GridHeight, height);
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
        gridMaterial.SetFloat(Fade, fade);
    }

    private void OnDestroy() {
        if (_texture != null)
            Destroy(_texture);
    }

    /// <summary>
    /// Loads a whole GridState into the state texture (every cell), then uploads it.
    /// </summary>
    /// <param name="state"></param>
    public void LoadGridState(GridNodeType[,] state) {
        // Loop over matrix and set values via cast Enum to int
        int gridWidth = state.GetLength(0);
        int gridHeight = state.GetLength(1);
        for (int x = 0; x < gridWidth; x++) {
            for (int y = 0; y < gridHeight; y++)
                SetValue(x, y, (int) state[x, y]);
        }

        UpdateShader(PropertyName.Values);
    }

    /// <summary>
    /// A more performant method of loading GridState values into the shader.
    /// </summary>
    /// <param name="state"></param>
    /// <param name="dirtyFlags"></param>
    public void LoadDirtyGridState(GridNodeType[,] state, bool[,] dirtyFlags) {
        int gridWidth = state.GetLength(0);
        for (int x = 0; x < gridWidth; x++) {
            int gridHeight = state.GetLength(1);
            for (int y = 0; y < gridHeight; y++)
                // only set value if the value has been marked as dirty
                if (dirtyFlags[x, y]) {
                    SetValue(x, y, (int) state[x, y]);
                    dirtyFlags[x, y] = false;
                }
        }

        UpdateShader(PropertyName.Values);
    }
    
    /// <summary>
    /// Packs a cell's state index into its texel (red channel). Uploaded to the GPU on the next
    /// UpdateShader(Values).
    /// </summary>
    /// <param name="x">the X coordinate</param>
    /// <param name="y">the Y coordinate</param>
    /// <param name="value">the GridNodeType index</param>
    public void SetValue(int x, int y, int value) {
        _pixels[width * y + x] = new Color32((byte) value, 0, 0, 255);
    }

    /// <summary>
    /// Translate a world position to the approximate position on the Grid.
    /// May not return valid grid coordinates (outside the range).
    /// </summary>
    /// <param name="worldPosition">a Vector3 World Position; Z coordinates are inconsequential.</param>
    /// <returns>A Vector2Int representing a grid position from the bottom left.</returns>
    public Vector2Int GetGridPosition(Vector3 worldPosition) {
        Vector3 localScale = transform.localScale;
        Vector2 gridPosition = (worldPosition + (localScale / 2f)) / new Vector2(localScale.x, localScale.y);
        return Size - new Vector2Int(
            (int) (gridPosition.x * width),
            (int) (gridPosition.y * height)) - Vector2Int.one;
    }

    /// <summary>
    /// Translates a position on the grid into a real World Position.
    /// </summary>
    /// <param name="gridPosition">The XY position on the grid</param>
    /// <returns>A Vector3 centered on the grid square in the World</returns>
    public Vector3 GetWorldPosition(Vector2Int gridPosition) {
        Transform ttransform = transform;
        Vector3 localScale = ttransform.localScale;
        Vector2 topRight = ttransform.position + (localScale / 2f);
        var singleSquare = new Vector2(localScale.x / width, localScale.y / height);
        Vector2 worldPosition = topRight - (singleSquare * (gridPosition + Vector2Int.one)) + (singleSquare / 2f);
        return worldPosition;
    }
}