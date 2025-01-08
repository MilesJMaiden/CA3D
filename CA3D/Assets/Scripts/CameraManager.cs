using System;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    #region Fields and Properties
    // Variables

    // Mouse panning variables
    private Vector3 lastMousePosition;
    public float panSpeed = 20f;

    // Zoom variables
    public float zoomSpeed = 10f;
    public float minZoom = 20f;
    public float maxZoom = 100f;

    // Rotation variables
    public float rotationSpeed = 100f;

    private Camera mainCamera;

    // Reference to the terrain
    public Terrain terrain;

    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        HandleMousePanning();
        HandleMouseZoom();
        HandleMouseRotation();
    }

    #endregion

    /// <summary>
    /// Handles mouse panning on the terrain.
    /// </summary>
    private void HandleMousePanning()
    {
        if (Input.GetMouseButtonDown(0))
        {
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(0))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            Vector3 move = new Vector3(-delta.x, -delta.y, 0) * panSpeed * Time.deltaTime;
            mainCamera.transform.Translate(move, Space.Self);
            lastMousePosition = Input.mousePosition;
        }
    }

    /// <summary>
    /// Handles mouse zooming on the terrain.
    /// </summary>
    private void HandleMouseZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0.0f)
        {
            if (mainCamera.orthographic)
            {
                mainCamera.orthographicSize -= scroll * zoomSpeed;
                mainCamera.orthographicSize = Mathf.Clamp(mainCamera.orthographicSize, minZoom, maxZoom);
            }
            else
            {
                mainCamera.fieldOfView -= scroll * zoomSpeed;
                mainCamera.fieldOfView = Mathf.Clamp(mainCamera.fieldOfView, minZoom, maxZoom);
            }
        }
    }

    /// <summary>
    /// Handles mouse rotation on the terrain.
    /// </summary>
    private void HandleMouseRotation()
    {
        if (Input.GetMouseButtonDown(1))
        {
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(1))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            float rotationX = delta.y * rotationSpeed * Time.deltaTime;
            float rotationY = -delta.x * rotationSpeed * Time.deltaTime;

            // Rotate around the terrain's center
            if (terrain != null)
            {
                Vector3 terrainCenter = terrain.transform.position + terrain.terrainData.size / 2f;
                mainCamera.transform.RotateAround(terrainCenter, Vector3.up, rotationY);
                mainCamera.transform.RotateAround(terrainCenter, mainCamera.transform.right, rotationX);
            }

            lastMousePosition = Input.mousePosition;
        }
    }
}
