using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;

public class ARFurnitureManager : MonoBehaviour
{
    [Header("AR Components")]
    public GameObject placementIndicator;
    public ARRaycastManager arRaycastManager;

    [Header("Furniture Settings")]
    public GameObject selectedFurniturePrefab;

    [Header("UI References")]
    public TMP_Dropdown modeDropdown; // Reference to your TMP Dropdown

    // Mode flags: In Add mode both isDeleteMode and isMoveMode are false.
    // In Delete mode, isDeleteMode is true.
    // In Move mode, isMoveMode is true.
    private bool isDeleteMode = false;
    private bool isMoveMode = false;

    private Pose placementPose;
    private bool placementPoseIsValid = false;
    private List<GameObject> placedFurniture = new List<GameObject>();

    // The furniture currently being moved (if any)
    private GameObject movingObject = null;

    private PlayerInput playerInput;
    private InputAction touchAction;

    void Start()
    {
        arRaycastManager = FindObjectOfType<ARRaycastManager>();

        // Programmatically subscribe to the TMP Dropdown's value-changed event.
        if (modeDropdown != null)
        {
            modeDropdown.onValueChanged.AddListener(OnModeDropdownValueChanged);
            // Set initial mode based on the current dropdown value.
            OnModeDropdownValueChanged(modeDropdown.value);
        }
        else
        {
            Debug.LogWarning("Mode Dropdown not assigned!");
        }
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

        // If in Move mode and an object is currently being moved,
        // update its position to match the placement pose.
        if (isMoveMode && movingObject != null && placementPoseIsValid)
        {
            movingObject.transform.position = placementPose.position;
            movingObject.transform.rotation = placementPose.rotation;
        }
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

        // Check if the tap is over a UI element.
        if (IsPointerOverUI(touchPosition))
        {
            Debug.Log("Tap is over UI, ignoring.");
            return;
        }

        // Process the tap based on the current mode.
        if (isMoveMode)
        {
            // If no object is currently moving, try to select one.
            if (movingObject == null)
            {
                // Look for a placed furniture object near the placement indicator.
                // Adjust the threshold distance as needed.
                float threshold = 0.2f;
                foreach (GameObject obj in placedFurniture)
                {
                    if (Vector3.Distance(obj.transform.position, placementPose.position) < threshold)
                    {
                        movingObject = obj;
                        Debug.Log("Object selected for moving: " + obj.name);
                        break;
                    }
                }
                if (movingObject == null)
                {
                    Debug.Log("No object found near the placement indicator to move.");
                }
            }
            else
            {
                // Object is currently moving; place it at the current location.
                Debug.Log("Placing moved object: " + movingObject.name);
                movingObject = null;
            }
        }
        else if (isDeleteMode)
        {
            TryDeleteFurniture(touchPosition);
        }
        else // Add mode
        {
            PlaceFurniture();
        }
    }

    // Custom function to determine if a screen point is over a UI element.
    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };
        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);
        return raycastResults.Count > 0;
    }

    // Instantiates a new furniture object at the placement pose.
    void PlaceFurniture()
    {
        if (!placementPoseIsValid || selectedFurniturePrefab == null)
        {
            Debug.Log("Cannot place furniture: invalid placement pose or no furniture selected.");
            return;
        }
        GameObject furniture = Instantiate(selectedFurniturePrefab, placementPose.position, placementPose.rotation);
        furniture.tag = "Furniture"; // Tag the furniture for deletion/movement.
        placedFurniture.Add(furniture);
        Debug.Log("Furniture placed: " + furniture.name);
    }

    // Uses a raycast from the screen position to delete a furniture object if hit.
    void TryDeleteFurniture(Vector2 screenPosition)
    {
        Ray ray = Camera.current.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log("Raycast hit: " + hit.collider.gameObject.name);
            GameObject target = hit.collider.gameObject;
            if (!target.CompareTag("Furniture") && target.transform.parent != null)
            {
                if (target.transform.parent.CompareTag("Furniture"))
                {
                    target = target.transform.parent.gameObject;
                }
            }
            if (target.CompareTag("Furniture"))
            {
                placedFurniture.Remove(target);
                Destroy(target);
                Debug.Log("Furniture deleted: " + target.name);
            }
            else
            {
                Debug.Log("Hit object is not tagged as Furniture.");
            }
        }
        else
        {
            Debug.Log("Raycast did not hit any object.");
        }
    }

    // Called by the UI to update the selected furniture prefab.
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

    // Callback for TMP Dropdown value change.
    // The dropdown options should be ordered as follows:
    // 0: "MODE: Add"  1: "MODE: Delete"  2: "MODE: Move"
    private void OnModeDropdownValueChanged(int modeIndex)
    {
        Debug.Log("Dropdown changed! New mode index: " + modeIndex);
        if (modeIndex == 0)
        {
            isDeleteMode = false;
            isMoveMode = false;
            Debug.Log("Mode changed to: ADD");
        }
        else if (modeIndex == 1)
        {
            isDeleteMode = true;
            isMoveMode = false;
            Debug.Log("Mode changed to: DELETE");
        }
        else if (modeIndex == 2)
        {
            isDeleteMode = false;
            isMoveMode = true;
            Debug.Log("Mode changed to: MOVE");
        }
        else
        {
            Debug.LogWarning("Unknown dropdown value: " + modeIndex);
        }
    }
}