using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.InputSystem;


public class ARTapToPlaceObject : MonoBehaviour
{
    public GameObject placementIndicator;
    public ARRaycastManager raycastManager;
    private bool placementPoseIsValid = false;
    private Pose placementPose;

    private PlayerInput playerInput;
    private InputAction touchAction;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        raycastManager = FindObjectOfType<ARRaycastManager>();
    }

    void Awake()
    {
        playerInput = new GetComponent<PlayerInput>();
        touchAction = playerInput.actions.FindAction("SingleTouchClick");
    }

    void OnEnable()
    {
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

    private void PlaceObject()
    {
        if (!placementPoseIsValid)
        {
            return;
        }

        Instantiate(objectToPlace, placementPose.position, placementPose.rotation);
    }

    // Update is called once per frame
    void Update()
    {
        UpdatePlacementPose();
        UpdatePlacementIndicator();

        if (placementPoseIsValid && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            PlaceObject();
        }
    }
}
