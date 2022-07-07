using UnityEngine;

/// <summary>
/// Frames the grid in the orthographic camera and lets you scroll to zoom and right/middle-drag to
/// pan around it. The grid is a fixed world size now (1 unit per cell) and the camera moves over it,
/// instead of the quad stretching to fill the screen every time - that fought with zooming.
/// Left mouse is left alone for drawing walls.
/// </summary>
public class CameraRig {
    private readonly Camera _camera;

    private float _frameSize = 5f; // ortho size that shows the whole grid at zoom 1
    private float _zoom = 1f;
    private Vector3 _panGrab; // world point under the cursor when a drag starts

    private const float MinZoom = 0.5f;
    private const float MaxZoom = 12f;
    private const float ZoomRate = 3f;

    public CameraRig(Camera camera) {
        _camera = camera;
    }

    /// <summary>Center on the grid and zoom out enough to see all of it. Resets zoom and pan.</summary>
    public void Frame(Vector2 center, Vector2 worldSize) {
        // fit whichever axis is tighter for the current aspect
        _frameSize = Mathf.Max(worldSize.y, worldSize.x / _camera.aspect) / 2f;
        _zoom = 1f;
        float z = _camera.transform.position.z;
        _camera.transform.position = new Vector3(center.x, center.y, z);
        _camera.orthographicSize = _frameSize;
    }

    public void Update() {
        Zoom();
        Pan();
    }

    private void Zoom() {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(scroll, 0f))
            return;

        // pin whatever's under the cursor while zooming so it doesn't drift
        Vector3 before = _camera.ScreenToWorldPoint(Input.mousePosition);
        _zoom = Mathf.Clamp(_zoom * (1f + scroll * ZoomRate), MinZoom, MaxZoom);
        _camera.orthographicSize = _frameSize / _zoom;
        Vector3 after = _camera.ScreenToWorldPoint(Input.mousePosition);
        _camera.transform.position += before - after;
    }

    private void Pan() {
        // right or middle drag - left is the wall brush
        if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            _panGrab = _camera.ScreenToWorldPoint(Input.mousePosition);
        else if (Input.GetMouseButton(1) || Input.GetMouseButton(2)) {
            Vector3 now = _camera.ScreenToWorldPoint(Input.mousePosition);
            _camera.transform.position += _panGrab - now; // keep the grabbed point under the cursor
        }
    }
}
