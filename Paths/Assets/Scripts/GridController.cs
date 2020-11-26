using System;
using UnityEngine;

public enum PropertyName {
    GridWidth,
    GridHeight,
    ValueLength,
    Values
}

/// <summary>
/// A simple Grid Rendering system controlling a Shader through a ComputeBuffer
/// </summary>
public class GridController : MonoBehaviour {
    public Material gridMaterial; // Maintain reference to the Grid Material the Shader is implanted upon
    public int width;
    public int height;
    
    // Value management
    private int[] _values;
    private ComputeBuffer _buffer;

    // Get all property IDs
    private static readonly int ValueLength = Shader.PropertyToID("_valueLength");
    private static readonly int Values = Shader.PropertyToID("_values");
    private static readonly int GridWidth = Shader.PropertyToID("_GridWidth");
    private static readonly int GridHeight = Shader.PropertyToID("_GridHeight");
    public Vector2Int Size => new Vector2Int(width, height);

    private void Start() {
        _values = new int[width * height];
        // TODO: Decide at some point how to improve how the ComputerBuffer's size is allocated.
        _buffer = new ComputeBuffer((int) Mathf.Pow(2048, 2), 4);

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
                _buffer.SetData(_values);
                gridMaterial.SetBuffer(Values, _buffer);
                break;
            case PropertyName.ValueLength:
                gridMaterial.SetFloat(ValueLength, _values.Length);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(property), property, null);
        }
    }

    private void OnApplicationQuit() {
        // Release ComputeBuffer memory
        _buffer.Release();
    }

    /// <summary>
    /// Loads a GridState into the Grid Shader's StructuredBuffer
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
    /// Sets a value in the 1D array at a particular 2D coordinate
    /// </summary>
    /// <param name="x">the X coordinate</param>
    /// <param name="y">the Y coordinate</param>
    /// <param name="value">the integer value</param>
    public void SetValue(int x, int y, int value) {
        _values[width * y + x] = value;
    }

    /// <summary>
    /// Returns the value at a 2D coordinate within the 1D array
    /// </summary>
    /// <param name="x">the X coordinate</param>
    /// <param name="y">the Y coordinate</param>
    /// <returns>a integer value</returns>
    public int GetValue(int x, int y) {
        return _values[width * y + x];
    }

    /// <summary>
    /// Converts a 2D coordinate into a 1D array index
    /// </summary>
    /// <param name="x">the X coordinate</param>
    /// <param name="y">the Y coordinate</param>
    /// <returns>the integer array index</returns>
    public int GetIndex(int x, int y) {
        return width * y + x;
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