using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class ARTapToPlaceObject : MonoBehaviour
{
    public GameObject placementIndicator;
    public GameObject objectToPlace;  // Added missing declaration for the object to instantiate

    public ARRaycastManager raycastManager;
    private bool placementPoseIsValid = false;
    private Pose placementPose;

    private PlayerInput playerInput;
    private InputAction touchAction;

    void Start()
    {
        raycastManager = FindObjectOfType<ARRaycastManager>();
    }

    void Awake()
    {
        // Corrected usage of GetComponent (removed 'new')
        playerInput = GetComponent<PlayerInput>();
        touchAction = playerInput.actions.FindAction("SingleTouchClick");
    }

    void OnEnable()
    {
        // Subscribe with an overload that accepts InputAction.CallbackContext
        touchAction.started += PlaceObject;
    }

    void OnDisable()
    {
        touchAction.started -= PlaceObject;
    }

    private void UpdatePlacementPose()
    {
        var screenCenter = Camera.current.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));
        var hits = new List<ARRaycastHit>();
        raycastManager.Raycast(screenCenter, hits, TrackableType.Planes);

        placementPoseIsValid = hits.Count > 0;
        if (placementPoseIsValid)
        {
            placementPose = hits[0].pose;
        }
    }

    private void UpdatePlacementIndicator()
    {
        if (placementPoseIsValid)
        {
            placementIndicator.SetActive(true);
            placementIndicator.transform.SetPositionAndRotation(placementPose.position, placementPose.rotation);
        }
        else
        {
            placementIndicator.SetActive(false);
        }
    }

    // Overload to match the InputAction delegate signature
    private void PlaceObject(InputAction.CallbackContext context)
    {
        PlaceObject();
    }

    private void PlaceObject()
    {
        if (!placementPoseIsValid)
        {
            return;
        }

        Instantiate(objectToPlace, placementPose.position, placementPose.rotation);
    }

    void Update()
    {
        UpdatePlacementPose();
        UpdatePlacementIndicator();

        // Use explicit namespace to resolve TouchPhase ambiguity
        if (placementPoseIsValid && Input.touchCount > 0 && Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began)
        {
            PlaceObject();
        }
    }
}