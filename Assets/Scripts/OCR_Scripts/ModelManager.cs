using UnityEngine;

public class ModelManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Transform spawnPoint;
    public GameObject defaultPrefab;

    private GameObject currentModel;

    public void DisplayModel(GameObject prefab)
    {
        // Clean up old model
        if (currentModel != null)
        {
            Destroy(currentModel);
        }

        // Fallback if prefab missing
        if (prefab == null)
        {
            prefab = defaultPrefab;
        }

        if (prefab == null) return;

        // Spawn new model
        currentModel = Instantiate(prefab, spawnPoint);
        currentModel.transform.localPosition = Vector3.zero;
        currentModel.transform.localRotation = Quaternion.identity;
        currentModel.transform.localScale = Vector3.one;

        // Make sure it's on the right layer
        SetLayerRecursively(currentModel, LayerMask.NameToLayer("IngreLayer"));
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    public void ClearModel()
    {
        if (currentModel != null)
        {
            Destroy(currentModel);
        }
    }
}
