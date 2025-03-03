using UnityEngine;

public class FurnitureSelectionUI : MonoBehaviour
{
    public ARFurnitureManager arFurnitureManager;

    // This method will be called by UI buttons; assign it in the Inspector for each button.
    // Each button should pass the appropriate furniture prefab as a parameter.
    public void SelectFurniture(GameObject furniturePrefab)
    {
        if (arFurnitureManager != null)
        {
            Debug.Log("Furniture selected: " + furniturePrefab.name);
            arFurnitureManager.SetSelectedFurniture(furniturePrefab);
        }
        else
        {
            Debug.LogError("ARFurnitureManager is not assigned in the FurnitureSelectionUI script!");
        }
    }
}