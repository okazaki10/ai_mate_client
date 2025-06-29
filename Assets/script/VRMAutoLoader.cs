using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UniGLTF;
using UniVRM10;
using VRM;
using static UnityEngine.ParticleSystem;
using UnityEngine.UI;
using System.Reflection;
using System.Linq;
using SFB;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class VRMAutoLoader : MonoBehaviour
{
    [Header("VRM Loading Settings")]
    public Transform parentTransform; // Optional parent for loaded models
    public bool destroyPreviousModel = true;

    public VRMModelManager vrmModelManager;
    public GameObject customModelOutput;
    public RuntimeAnimatorController animatorController;
    public GameObject componentTemplatePrefab;
    public VRMAdvancedAudioMouth vRMAdvancedAudioMouth;
    public MenuManager menuManager;
    public PopUpMessage popUpMessage;

    private GameObject loadedModel;
    private GameObject currentModel;
    private bool isLoading = false;

    void Start()
    {
        // Optionally load a VRM file on start
        // LoadVRMWithFileBrowser();
    }

    void Update()
    {
    
    }

    public void OpenFileDialogAndLoadVRM()
    {
        if (isLoading) return;

        isLoading = true;
        var extensions = new[] { new ExtensionFilter("Model Files", "vrm") };
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Select Model File", "", extensions, false);
        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            menuManager.inputFieldVrmPath.text = paths[0];
            _ = LoadVRMFromPath(paths[0]);
        }

        isLoading = false;
    }

    public async Task LoadVRMFromPath(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            Debug.LogError($"VRM file not found at path: {path}");
            popUpMessage.showMessage($"VRM file not found at path: {path}");
            return;
        }

        try
        {
            Debug.Log($"Loading VRM from: {path}");

            // Destroy previous model if specified
            if (destroyPreviousModel && loadedModel != null)
            {
                DestroyImmediate(loadedModel);
                loadedModel = null;
            }

            byte[] fileData = await Task.Run(() => File.ReadAllBytes(path));
            if (fileData == null || fileData.Length == 0) return;

            // Parse GLB data
            var glbData = new GlbFileParser(path).Parse();
            var vrm10Data = Vrm10Data.Parse(glbData);

            if (vrm10Data != null)
            {
                using var importer10 = new Vrm10Importer(vrm10Data);
                var instance10 = await importer10.LoadAsync(new ImmediateCaller());

                if (instance10.Root != null)
                {
                    loadedModel = instance10.Root;

                    // Set parent if specified
                    if (parentTransform != null)
                    {
                        loadedModel.transform.SetParent(parentTransform);
                    }

                    // Reset position and rotation
                    loadedModel.transform.localPosition = Vector3.zero;
                    loadedModel.transform.localRotation = Quaternion.identity;

                    // Optional: Scale adjustment
                    loadedModel.transform.localScale = Vector3.one;

                    Debug.Log($"Successfully loaded VRM 10: {loadedModel.name}");

                    // Call event for successful load
                    OnVRMLoaded(loadedModel);
                }
                else
                {
                    Debug.LogError("Failed to load VRM: Root object is null");
                    popUpMessage.showMessage("Failed to load VRM: Root object is null");
                }
            }

            if (loadedModel == null)
            {
            
                    using var gltfData = new GlbBinaryParser(fileData, path).Parse();
                    var importer = new VRMImporterContext(new VRMData(gltfData));
                    var instance = await importer.LoadAsync(new ImmediateCaller());
                    if (instance.Root != null) {
                        loadedModel = instance.Root;
                        Debug.Log($"Successfully loaded VRM: {loadedModel.name}");
                }
            }

            if (loadedModel == null)
            {
                return;
            }

            FinalizeLoadedModel(loadedModel, path);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading VRM file: {e.Message}\n{e.StackTrace}");
            popUpMessage.showMessage($"Error loading VRM file: {e.Message}\n{e.StackTrace}");
        }
    }

    private void EnableSkinnedMeshRenderers(GameObject model)
    {
        foreach (var skinnedMesh in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            skinnedMesh.enabled = true;
    }

    private void DisableVRMModel()
    {
        if (vrmModelManager.mainModel != null)
            vrmModelManager.mainModel.SetActive(false);
    }

    private void EnableVRMModel()
    {
        if (vrmModelManager.mainModel != null)
            vrmModelManager.mainModel.SetActive(true);
    }

    private void ClearPreviousCustomModel(bool skipRawImageCleanup = false)
    {
        if (customModelOutput != null)
        {
            foreach (Transform child in customModelOutput.transform)
            {
                if (child.gameObject == vrmModelManager.mainModel)
                    continue;
                CleanupMaterialsAndTextures(child.gameObject);
                CleanupRawImages(child.gameObject);
                Destroy(child.gameObject);
            }
        }

        if (!skipRawImageCleanup)
            CleanupAllRawImagesInScene();
    }

    private void CleanupAllRawImagesInScene()
    {
        var rawImages = GameObject.FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var rawImage in rawImages)
        {
            rawImage.texture = null;
        }
    }

    private void CleanupRawImages(GameObject obj)
    {
        if (obj == null) return;
        var rawImages = obj.GetComponentsInChildren<RawImage>(true);
        foreach (var rawImage in rawImages)
        {
            rawImage.texture = null;
        }
    }

    private void CleanupMaterialsAndTextures(GameObject obj)
    {
        if (obj == null) return;
        foreach (var renderer in obj.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer.materials != null)
            {
                foreach (var mat in renderer.materials)
                {
                    if (mat == null) continue;
                    if (mat.mainTexture != null)
                        mat.mainTexture = null;
                }
            }
        }
    }

    private void AssignAnimatorController(GameObject model)
    {
        var animator = model.GetComponentInChildren<Animator>();
        if (animator != null && animatorController != null)
            animator.runtimeAnimatorController = animatorController;
    }

    private void InjectComponentsFromPrefab(GameObject prefabTemplate, GameObject targetModel)
    {
        if (prefabTemplate == null || targetModel == null) return;

        var templateObj = Instantiate(prefabTemplate);
        var animator = targetModel.GetComponentInChildren<Animator>();

        foreach (var templateComp in templateObj.GetComponents<MonoBehaviour>())
        {
            var type = templateComp.GetType();
            if (targetModel.GetComponent(type) != null)
                continue;
            var newComp = targetModel.AddComponent(type);
            CopyComponentValues(templateComp, newComp);

            if (animator != null)
            {
                var setAnimMethod = type.GetMethod("SetAnimator", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (setAnimMethod != null)
                    setAnimMethod.Invoke(newComp, new object[] { animator });

                var animatorField = type.GetField("animator", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (animatorField != null && animatorField.FieldType == typeof(Animator))
                    animatorField.SetValue(newComp, animator);
            }
        }
        Destroy(templateObj);
    }

    private void CopyComponentValues(Component source, Component destination)
    {
        var type = source.GetType();
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var field in fields)
        {
            if (field.IsDefined(typeof(SerializeField), true) || field.IsPublic)
                field.SetValue(destination, field.GetValue(source));
        }
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(p => p.CanWrite && p.GetSetMethod(true) != null);
        foreach (var prop in props)
        {
            try { prop.SetValue(destination, prop.GetValue(source)); }
            catch { }
        }
    }

    private void FinalizeLoadedModel(GameObject loadedModel, string path)
    {
        DisableVRMModel();
        ClearPreviousCustomModel();

        loadedModel.transform.SetParent(customModelOutput.transform, false);
        loadedModel.transform.localPosition = vrmModelManager.mainModel.transform.localPosition;
        loadedModel.transform.localRotation = vrmModelManager.mainModel.transform.localRotation;
        loadedModel.transform.localScale = vrmModelManager.mainModel.transform.localScale;
        currentModel = loadedModel;
        currentModel.SetActive(true);

        EnableSkinnedMeshRenderers(loadedModel);
        AssignAnimatorController(loadedModel);
        InjectComponentsFromPrefab(componentTemplatePrefab, currentModel);

        vrmModelManager.vrmBlendShapeProxy = currentModel.GetComponent<VRMBlendShapeProxy>();
        vrmModelManager.animator = currentModel.GetComponent<Animator>();
    }

    public void useDefaultModel()
    {
        EnableVRMModel();

        currentModel.SetActive(false);

        vrmModelManager.vrmBlendShapeProxy = vrmModelManager.mainModel.GetComponent<VRMBlendShapeProxy>();
        vrmModelManager.animator = vrmModelManager.mainModel.GetComponent<Animator>();
    }

    // Override this method to add custom behavior after VRM loads
    protected virtual void OnVRMLoaded(GameObject vrmModel)
    {
        // Add any post-loading logic here
        // For example: setup animator, adjust materials, etc.

        // Example: Find and setup animator
        var animator = vrmModel.GetComponent<Animator>();
        if (animator != null)
        {
            Debug.Log("VRM Animator found and ready");
        }

        // Example: Log VRM info
        var vrmMeta = vrmModel.GetComponent<UniVRM10.VRM10ObjectMeta>();
        if (vrmMeta != null)
        {
            Debug.Log($"VRM Name: {vrmMeta.Name}");
            Debug.Log($"VRM Version: {vrmMeta.Version}");
        }
    }

    // Public method to load VRM from Resources or StreamingAssets
    public async Task LoadVRMFromResources(string fileName)
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        await LoadVRMFromPath(path);
    }

    // Method to unload current VRM
    public void UnloadCurrentVRM()
    {
        if (loadedModel != null)
        {
            DestroyImmediate(loadedModel);
            loadedModel = null;
            Debug.Log("VRM model unloaded");
        }
    }

    // Property to check if a VRM is currently loaded
    public bool HasLoadedModel => loadedModel != null;

    // Get reference to currently loaded model
    public GameObject GetLoadedModel() => loadedModel;
}

// Extension class for additional VRM utilities
public static class VRMLoaderExtensions
{
    public static void SetupBasicLighting(this GameObject vrmModel)
    {
        // Add basic lighting setup for VRM model
        var renderers = vrmModel.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            foreach (var material in renderer.materials)
            {
                if (material.HasProperty("_MainTex"))
                {
                    // Basic material adjustments if needed
                }
            }
        }
    }

    public static void EnableVRMComponents(this GameObject vrmModel)
    {
        // Enable VRM-specific components
        var vrmComponents = vrmModel.GetComponentsInChildren<MonoBehaviour>();
        foreach (var component in vrmComponents)
        {
            if (component.GetType().Namespace == "UniVRM10")
            {
                component.enabled = true;
            }
        }
    }
}