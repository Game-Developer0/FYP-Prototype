using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class MiniMapRedDotMarker : MonoBehaviour
{
    [Header("Detection")]
    public Transform player;
    public string playerTag = "Player";
    public float dragonAreaDistance = 120f;

    [Header("Minimap Dot Settings")]
    public string minimapIconLayerName = "MinimapIcon";
    public float heightAboveTarget = 5f;
    public float dotSize = 6f;
    public Material dotMaterial;

    [Header("Dragon Body Minimap Visibility")]
    public bool hideDragonBodyWhenPlayerInArea = true;
    public string hiddenFromMinimapLayerName = "HideFromMinimap";

    [Tooltip("Leave empty if this script is on the main dragon root object.")]
    public Transform dragonVisualRoot;

    private GameObject dotObject;
    private Renderer dotRenderer;

    private readonly Dictionary<GameObject, int> originalLayers = new Dictionary<GameObject, int>();
    private bool dragonBodyHidden = false;

    private void Awake()
    {
        if (dragonVisualRoot == null)
            dragonVisualRoot = transform;

        FindPlayerIfMissing();
        CacheOriginalLayers();
        CreateDot();

        if (dotObject != null)
            dotObject.SetActive(false);
    }

    private void Start()
    {
        FindPlayerIfMissing();
    }

    private void LateUpdate()
    {
        FindPlayerIfMissing();

        if (dotObject == null)
            return;

        bool playerInsideArea = IsPlayerInsideDragonArea();

        if (playerInsideArea)
        {
            dotObject.SetActive(true);

            if (hideDragonBodyWhenPlayerInArea && !dragonBodyHidden)
            {
                HideDragonBodyFromMinimap();
            }

            UpdateDotPosition();
        }
        else
        {
            dotObject.SetActive(false);

            if (dragonBodyHidden)
            {
                RestoreDragonBodyLayers();
            }
        }
    }

    private void FindPlayerIfMissing()
    {
        if (player != null)
            return;

        GameObject foundPlayer = GameObject.FindGameObjectWithTag(playerTag);

        if (foundPlayer != null)
        {
            player = foundPlayer.transform;
        }
    }

    private bool IsPlayerInsideDragonArea()
    {
        if (player == null)
            return false;

        Vector2 playerXZ = new Vector2(player.position.x, player.position.z);
        Vector2 dragonXZ = new Vector2(transform.position.x, transform.position.z);

        float distance = Vector2.Distance(playerXZ, dragonXZ);
        return distance <= dragonAreaDistance;
    }

    private void UpdateDotPosition()
    {
        dotObject.transform.position = transform.position + Vector3.up * heightAboveTarget;
        dotObject.transform.rotation = Quaternion.identity;
        dotObject.transform.localScale = new Vector3(dotSize, 0.2f, dotSize);
    }

    private void CacheOriginalLayers()
    {
        originalLayers.Clear();

        Renderer[] renderers = dragonVisualRoot.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            GameObject rendererObject = renderer.gameObject;

            if (!originalLayers.ContainsKey(rendererObject))
            {
                originalLayers.Add(rendererObject, rendererObject.layer);
            }
        }
    }

    private void HideDragonBodyFromMinimap()
    {
        int hiddenLayer = LayerMask.NameToLayer(hiddenFromMinimapLayerName);

        if (hiddenLayer == -1)
        {
            Debug.LogWarning("Layer '" + hiddenFromMinimapLayerName + "' was not found. Create this layer first.");
            return;
        }

        foreach (KeyValuePair<GameObject, int> layerData in originalLayers)
        {
            if (layerData.Key != null)
            {
                layerData.Key.layer = hiddenLayer;
            }
        }

        dragonBodyHidden = true;
    }

    private void RestoreDragonBodyLayers()
    {
        foreach (KeyValuePair<GameObject, int> layerData in originalLayers)
        {
            if (layerData.Key != null)
            {
                layerData.Key.layer = layerData.Value;
            }
        }

        dragonBodyHidden = false;
    }

    private void CreateDot()
    {
        dotObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        dotObject.name = gameObject.name + "_MiniMap_Red_Dot";

        Collider dotCollider = dotObject.GetComponent<Collider>();
        if (dotCollider != null)
        {
            Destroy(dotCollider);
        }

        int minimapLayer = LayerMask.NameToLayer(minimapIconLayerName);

        if (minimapLayer != -1)
        {
            SetLayerRecursively(dotObject, minimapLayer);
        }
        else
        {
            Debug.LogWarning("Layer '" + minimapIconLayerName + "' was not found. Create this layer first.");
        }

        dotRenderer = dotObject.GetComponent<Renderer>();

        if (dotMaterial != null)
        {
            dotRenderer.material = dotMaterial;
        }
        else
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            Material newMaterial = new Material(shader);
            newMaterial.name = "Runtime_Minimap_Red_Dot_Material";

            if (newMaterial.HasProperty("_BaseColor"))
            {
                newMaterial.SetColor("_BaseColor", Color.red);
            }

            if (newMaterial.HasProperty("_Color"))
            {
                newMaterial.SetColor("_Color", Color.red);
            }

            dotRenderer.material = newMaterial;
        }

        dotRenderer.shadowCastingMode = ShadowCastingMode.Off;
        dotRenderer.receiveShadows = false;
    }

    private void SetLayerRecursively(GameObject targetObject, int layer)
    {
        targetObject.layer = layer;

        foreach (Transform child in targetObject.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private void OnDestroy()
    {
        RestoreDragonBodyLayers();

        if (dotObject != null)
        {
            Destroy(dotObject);
        }
    }
}