﻿using System;
using System.Linq;
using UnityEngine;

public enum PropertyName {
    GridSize,
    ValueLength,
    Values
}

/// <summary>
/// A simple Grid Rendering Controller using MeshRenderer.
/// </summary>
public class GridController : MonoBehaviour {
    public Material gridMaterial;
    public int size = 32;

    private int[] _values;
    private ComputeBuffer _buffer;

    // Get all property IDs
    private static readonly int ValueLength = Shader.PropertyToID("_valueLength");
    private static readonly int Values = Shader.PropertyToID("_values");
    private static readonly int GridSize = Shader.PropertyToID("_GridSize");

    private void Start() {
        _values = new int[size * size];
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
            case PropertyName.GridSize:
                gridMaterial.SetFloat(GridSize, size);
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
    /// Loads a GridState 
    /// </summary>
    /// <param name="gridState"></param>
    public void LoadGridState(GridState gridState) {
        // Loop over matrix and set values via cast Enum to int
        foreach(int x in Enumerable.Range(0, gridState.Grid.Count - 1))
            foreach(int y in Enumerable.Range(0, gridState.Grid[0].Count - 1))
                this.SetValue(x, y, (int) gridState.Grid[x][y]);
    }
    
    /// <summary>
    /// Sets a value in the 1D array at a particular 2D coordinate
    /// </summary>
    /// <param name="x">the X coordinate</param>
    /// <param name="y">the Y coordinate</param>
    /// <param name="value">the integer value</param>
    public void SetValue(int x, int y, int value) {
        _values[size * y + x] = value;
    }
    
    /// <summary>
    /// Returns the value at a 2D coordinate within the 1D array
    /// </summary>
    /// <param name="x">the X coordinate</param>
    /// <param name="y">the Y coordinate</param>
    /// <returns>a integer value</returns>
    public int GetValue(int x, int y) {
        return _values[size * y + x];
    }

    /// <summary>
    /// Converts a 2D coordinate into a 1D array index
    /// </summary>
    /// <param name="x">the X coordinate</param>
    /// <param name="y">the Y coordinate</param>
    /// <returns>the integer array index</returns>
    public int GetIndex(int x, int y) {
        return size * y + x;
    }
}