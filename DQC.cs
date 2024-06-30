using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Cms;
using static BlackStartX.GestureManager.Editor.Data.GestureManagerStyles.Animations;

public class DenelixQuestCrafter : EditorWindow
{
    // Selected avatar fsa
    private GameObject referenceAvatar;
    private Texture2D matCapTexture;
    private Texture2D headerImage;
    private List<Component> physBones = new List<Component>();
    private List<bool> deleteToggles = new List<bool>();
    private Vector2 scrollPos;
    string texturesPath = "";
    string materialsPath = "";
    string mainPath = "";
    private float compressionValue = 1.0f;
    private int maxSizeValue = 7;

    private enum ShaderType { MatCap, StandardLite, ToonLit }
    private ShaderType selectedShaderType = ShaderType.MatCap;

    // Creates a menu item in the Unity Editor
    [MenuItem("Tools/Denelix Quest Crafter")]
    public static void ShowWindow()
    {
        // Gets the window from Unity inheritance
        var window = GetWindow<DenelixQuestCrafter>("Denelix Quest Crafter");
        window.minSize = new Vector2(500, 600); // Minimum size
        window.maxSize = new Vector2(500, 1900);
    }

    // Overrides the OnGUI method to draw the GUI
    private void OnGUI()
    {
        // Load and display the header image
        if (headerImage == null)
        {
            headerImage = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Denelix Quest Crafter/img.png");
        }
        if (headerImage != null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(headerImage, GUILayout.Width(300), GUILayout.Height(300));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        // Object field for the reference avatar
        referenceAvatar = (GameObject)EditorGUILayout.ObjectField("PC Avatar", referenceAvatar, typeof(GameObject), true);
        EditorGUILayout.LabelField("Select the avatar you want to convert for the quest.", EditorStyles.wordWrappedLabel);

        // Add a horizontal line
        GUILayout.Space(10);
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        GUILayout.Space(10);

        GUILayout.Box("(OPTIONAL)Select a MatCap texture for the avatar. Makes it look cooler :3", GUILayout.ExpandWidth(true), GUILayout.Height(20));

        // Object field for the MatCap texture
        matCapTexture = (Texture2D)EditorGUILayout.ObjectField("MatCap Texture", matCapTexture, typeof(Texture2D), false);

        // Add another horizontal line
        GUILayout.Space(10);
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        GUILayout.Space(10);

        GUILayout.Space(10);
        GUILayout.Box("Default value is 7 (2048). Lower numbers make the file size smaller, and higher numbers make it bigger.", GUILayout.ExpandWidth(true), GUILayout.Height(20));
        GUILayout.BeginHorizontal();
        GUILayout.Space(10);
        GUILayout.Label("Max Size:       ", GUILayout.ExpandWidth(false), GUILayout.Height(20));
        maxSizeValue = EditorGUILayout.IntSlider(maxSizeValue, 1, 10);
        GUILayout.EndHorizontal();
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Compression:", GUILayout.ExpandWidth(false), GUILayout.Height(20));
        compressionValue = EditorGUILayout.Slider(compressionValue, 0.0f, 1.0f);
        GUILayout.EndHorizontal();
        GUILayout.Space(10);

        // Add another horizontal line
        GUILayout.Space(10);
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        GUILayout.Space(10);

        // Load PhysBone components button
        if (GUILayout.Button("Load PhysBone Components"))
        {
            if (referenceAvatar != null)
            {
                deleteToggles.Clear();
                LoadPhysBoneComponents(referenceAvatar);
            }
            else
            {
                Debug.LogError("Reference avatar is not assigned.");
            }
        }

        // Display PhysBone components in an interactable list
        if (physBones.Count > 0)
        {
            GUILayout.Label("PhysBone Components:");
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
            for (int i = 0; i < physBones.Count; i++)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(physBones[i], typeof(Component), true);
                deleteToggles[i] = EditorGUILayout.Toggle(deleteToggles[i], GUILayout.Width(20));
                GUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        GUILayout.Space(10);
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        GUILayout.Space(10);

        if (GUILayout.Button("CONVERT"))
        {
            if (referenceAvatar != null)
            {
                ConvertAvatar();
            }
            else
            {
                Debug.LogError("Reference avatar is not assigned.");
            }
        }
    }

    private void ConvertAvatar()
    {
        if (matCapTexture != null)
        {
            // Make the texture readable and uncompressed directly in this method
            string matCapTexturePath = AssetDatabase.GetAssetPath(matCapTexture);
            var textureImporter = AssetImporter.GetAtPath(matCapTexturePath) as TextureImporter;
            if (textureImporter != null)
            {
                textureImporter.isReadable = true;
                textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
                textureImporter.SaveAndReimport();
            }
            deleteToggles.Clear();
        }

        GameObject duplicateAvatar = DuplicateAvatar();

        string scriptLocation = Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this)));
        mainPath = $"{scriptLocation}/{duplicateAvatar.name}";
        EnsureFolderExists(scriptLocation, duplicateAvatar.name);
        texturesPath = CreateSubDirectory(mainPath, "Textures");
        materialsPath = CreateSubDirectory(mainPath, "Materials");

        Texture2D duplicatedTexture = null;
        if (matCapTexture != null)
        {
            //Do same for texutres cant think rn idk just remmeber
        }

        DuplicateAndAssignMaterials(duplicateAvatar, duplicatedTexture, materialsPath);
        DeletePhysBones(duplicateAvatar);
        Selection.activeObject = duplicateAvatar;
    }

private void DuplicateAndAssignMaterials(GameObject duplicateAvatar, Texture2D duplicatedTexture, string materialsPath)
{
    var originalRenderers = LinkRenderMaterials(referenceAvatar);

    foreach (var rendererTuple in originalRenderers)
    {
            Debug.Log("Renderer: " + rendererTuple.Item1);
        Renderer duplicateRenderer = FindRendererByName(duplicateAvatar, rendererTuple.Item1);

        // If a matching renderer is found in the duplicate avatar. Not really needed but just in case.
        if (duplicateRenderer != null)
        {
            // Iterate through linked renderers and for their Mesh Renderer
            //This makes sure every Quest Material is applied to the correct Mesh
            //This is also a list because each renderer can have multiple materials even if it's one the data structure is still a list
            Material[] newMaterials = new Material[rendererTuple.Item2.Length];
            for (int i = 0; i < rendererTuple.Item2.Length; i++)
            {
                Material originalMaterial = rendererTuple.Item2[i];

                // Define the path for the duplicated material
                string materialPath = $"{materialsPath}/{originalMaterial.name}_Quest.mat";

                // Check if the material already exists at the specified path
                // Without this check, a material that is reused in the original avatar would have missing references except for the last material.
                Material duplicatedMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (duplicatedMaterial == null)
                {
                    duplicatedMaterial = new Material(originalMaterial);
                    duplicatedMaterial.shader = Shader.Find("VRChat/Mobile/MatCap Lit");

                    // Duplicate the textures used by the material
                    var mainTexture = originalMaterial.mainTexture as Texture2D;
                    var duplicatedMainTexture = DuplicateTexture(mainTexture, materialsPath);
                    duplicatedMainTexture.Compress(compressionValue > 0.5f);
                    duplicatedMainTexture.Apply();
                    if (mainTexture != null)
                    {
                        duplicatedMaterial.mainTexture = duplicatedMainTexture;
                    }

                    // Just so you can convert without a MatCap Texture
                    if (duplicatedTexture != null)
                    {
                        duplicatedMaterial.SetTexture("_MatCap", duplicatedTexture);
                    }

                    AssetDatabase.CreateAsset(duplicatedMaterial, materialPath);
                }

                // Load the duplicated material from the asset database and assigns it to the new materials array
                newMaterials[i] = duplicatedMaterial;
            }

            // Assign the new materials array to the sharedMaterials property of the duplicate renderer
            duplicateRenderer.sharedMaterials = newMaterials;
        }
    }
}

    private GameObject DuplicateAvatar()
    {
        GameObject duplicateAvatar = Instantiate(referenceAvatar);
        duplicateAvatar.name = referenceAvatar.name + "_Quest";
        return duplicateAvatar;
    }

    private string CreateSubDirectory(string parentDirectory, string subDirectoryName)
    {
        string subDirectoryPath = $"{parentDirectory}/{subDirectoryName}";
        EnsureFolderExists(parentDirectory, subDirectoryName);
        return subDirectoryPath;
    }

    //Returns the dexture that was duplicated
    private Texture2D DuplicateTexture(Texture2D originalTexture, string path)
    {
        string originalPath = AssetDatabase.GetAssetPath(originalTexture);
        string newTexturePath = $"{path}/{originalTexture.name}_Quest.png";

        // Check if the texture already exists at the new path
        if (File.Exists(newTexturePath))
        {
            File.Delete(newTexturePath);
        }
        FileUtil.CopyFileOrDirectory(originalPath, newTexturePath);
        AssetDatabase.ImportAsset(newTexturePath);
        Texture2D newTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(newTexturePath);
        Debug.LogError("Duplicate files failed"); 
        return newTexture;
    }


    private List<(string, Material[])> LinkRenderMaterials(GameObject avatar)
    {
        var rendererMaterials = new List<(string, Material[])>();
        Renderer[] renderers = avatar.GetComponentsInChildren<Renderer>(true); //TRUE IS IMPORTANT FOR SOMER REASON TO INCLUDE DISABLED OBJECTS
        foreach (Renderer renderer in renderers)
        {
            rendererMaterials.Add((renderer.name, renderer.sharedMaterials));
        }
        return rendererMaterials;
    }

    private Renderer FindRendererByName(GameObject avatar, string name)
    {
        Renderer[] renderers = avatar.GetComponentsInChildren<Renderer>(true); //This too
        foreach (Renderer renderer in renderers)
        {
            if (renderer.name == name)
            {
                return renderer;
            }
        }
        return null;
    }

    private void EnsureFolderExists(string parentFolder, string newFolder)
    {
        string folderPath = $"{parentFolder}/{newFolder}";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder(parentFolder, newFolder);
        }
    }

    private void LoadPhysBoneComponents(GameObject avatar)
    {
        physBones.Clear();
        List<Component> allComponents = new List<Component>();
        allComponents.AddRange(avatar.GetComponentsInChildren<Component>(true));
        foreach (var component in allComponents)
        {
            if (component.GetType().Name.Contains("PhysBone") && !component.GetType().Name.Contains("Collider"))
            {
                physBones.Add(component);
                deleteToggles.Add(false);
            }
        }
    }

    private void GetRootTransform(Component component)
    {
        var rootTransformProperty = component.GetType().GetProperty("rootTransform");
        if (rootTransformProperty != null)
        {
            var rootTransformValue = rootTransformProperty.GetValue(component, null);
            Debug.Log($"Component: {component.name}, Root Transform: {rootTransformValue}");
        }
        else
        {
            Debug.Log($"Component: {component.name} does not have a rootTransform property.");
        }
    }
    //gd
    private void DeletePhysBones(GameObject avatar)
    {
        LoadPhysBoneComponents(avatar);
        for (int i = 0; i < physBones.Count; i++)
        {
            if (deleteToggles[i])
            {
                DestroyImmediate(physBones[i]);
            }
        }
    }
}

//needs to work on disabled objects - done

//Textures to be copied to the new folder - done

//Textures be applied onto the new duplicated materials - done

//Textures able to be modified in the editor (compression)

//VRCFURY Toggles get transferred to the new avatar in a new VRCFury Toggle

//VRC Fury Toggles and VRC Fury FX Controller install too.