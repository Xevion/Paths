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

    private void Start() {
        _values = new int[width * height];
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
        for (int x = 0; x < state.GetLength(0); x++) {
            for (int y = 0; y < state.GetLength(1); y++)
                this.SetValue(x, y, (int) state[x, y]);
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
}