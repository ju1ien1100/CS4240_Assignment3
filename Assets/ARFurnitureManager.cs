using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class ARFurnitureManager : MonoBehaviour
{
    [Header("AR Components")]
    public GameObject placementIndicator;
    public ARRaycastManager arRaycastManager;

    [Header("Furniture Settings")]
    public GameObject selectedFurniturePrefab;
    public bool isDeleteMode = false;

    private Pose placementPose;
    private bool placementPoseIsValid = false;
    private List<GameObject> placedFurniture = new List<GameObject>();

    private PlayerInput playerInput;
    private InputAction touchAction;

    void Start()
    {
        arRaycastManager = FindObjectOfType<ARRaycastManager>();
    }

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        touchAction = playerInput.actions.FindAction("SingleTouchClick");
    }

    void OnEnable()
    {
        touchAction.started += OnTap;
    }

    void OnDisable()
    {
        touchAction.started -= OnTap;
    }

    void Update()
    {
        UpdatePlacementPose();
        UpdatePlacementIndicator();
    }

    void UpdatePlacementPose()
    {
        var screenCenter = Camera.current.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));
        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        arRaycastManager.Raycast(screenCenter, hits, TrackableType.Planes);
        placementPoseIsValid = hits.Count > 0;
        if (placementPoseIsValid)
        {
            placementPose = hits[0].pose;
        }
    }

    void UpdatePlacementIndicator()
    {
        if (placementIndicator != null)
        {
            placementIndicator.SetActive(placementPoseIsValid);
            if (placementPoseIsValid)
            {
                placementIndicator.transform.SetPositionAndRotation(placementPose.position, placementPose.rotation);
            }
        }
    }

    // Callback for the touch action using the new Input System
    private void OnTap(InputAction.CallbackContext context)
    {
        Vector2 touchPosition = Vector2.zero;

        // Read touch position correctly based on the control's value type
        if (context.control.valueType == typeof(Vector2))
        {
            touchPosition = context.ReadValue<Vector2>();
        }
        else
        {
            if (Touchscreen.current != null)
            {
                touchPosition = Touchscreen.current.position.ReadValue();
            }
            else
            {
                Debug.LogWarning("No touchscreen device found.");
            }
        }

        Debug.Log("Tap detected at: " + touchPosition);

        // Check if the tap is over a UI element using our custom function
        if (IsPointerOverUI(touchPosition))
        {
            Debug.Log("Tap is over UI, ignoring.");
            return;
        }

        // Process the tap based on the current mode
        if (isDeleteMode)
        {
            TryDeleteFurniture(touchPosition);
        }
        else
        {
            PlaceFurniture();
        }
    }

    // Custom function to determine if a screen point is over a UI element
    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };
        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);

        // Optionally, if you only want to ignore objects with a specific tag (e.g., "UI"), you could check:
        // foreach (RaycastResult result in raycastResults)
        // {
        //     if (result.gameObject.CompareTag("UI"))
        //         return true;
        // }

        return raycastResults.Count > 0;
    }

    // Places the selected furniture at the current valid placement pose
    void PlaceFurniture()
    {
        if (!placementPoseIsValid || selectedFurniturePrefab == null)
        {
            Debug.Log("Cannot place furniture: invalid placement pose or no furniture selected.");
            return;
        }

        GameObject furniture = Instantiate(selectedFurniturePrefab, placementPose.position, placementPose.rotation);
        furniture.tag = "Furniture"; // Tag the furniture for deletion
        placedFurniture.Add(furniture);
        Debug.Log("Furniture placed: " + furniture.name);
    }

    // Uses a raycast from the screen position to delete a furniture object, if hit.
    void TryDeleteFurniture(Vector2 screenPosition)
    {
        Ray ray = Camera.current.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider != null && hit.collider.gameObject.CompareTag("Furniture"))
            {
                GameObject toRemove = hit.collider.gameObject;
                placedFurniture.Remove(toRemove);
                Destroy(toRemove);
                Debug.Log("Furniture deleted: " + toRemove.name);
            }
        }
    }

    // Called by the UI to update the selected furniture prefab
    public void SetSelectedFurniture(GameObject furniturePrefab)
    {
        if (furniturePrefab != null)
        {
            Debug.Log("Selected furniture updated to: " + furniturePrefab.name);
            selectedFurniturePrefab = furniturePrefab;
        }
        else
        {
            Debug.LogError("Received NULL furniturePrefab!");
        }
    }

    // Called by the UI to toggle deletion mode
    public void SetDeleteMode(bool value)
    {
        isDeleteMode = value;
    }
}