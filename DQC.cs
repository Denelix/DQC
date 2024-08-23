using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System;

public class DenelixQuestCrafter : EditorWindow
{
    // Selected avatar fsa
    private GameObject referenceAvatar;
    private Texture2D matCapTexture;
    private Texture2D headerImage;

    private List<Component> physBones = new List<Component>();
    private List<bool> deleteToggles = new List<bool>();
    private List<(Mesh mesh, Vector3 meshSize)> meshComponents = new List<(Mesh, Vector3)>(); //Mesh and size in one tuple

    private Vector2 scrollPos;
    private static System.Random random = new System.Random();

    private const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
    string texturesPath = "";
    string materialsPath = "";
    string mainPath = "";

    private int compressionValue = 100;
    private int maxSizeValue = 32;

    private enum ShaderType { MatCap, StandardLite, ToonLit }
    private ShaderType selectedShaderType = ShaderType.MatCap;

    [MenuItem("Tools/Denelix Quest Crafter")]
    public static void ShowWindow()
    {
        var window = GetWindow<DenelixQuestCrafter>("Denelix Quest Crafter");
        window.minSize = new Vector2(500, 600); // Minimum size
        window.maxSize = new Vector2(500, 1900);
    }

    private void OnGUI()
    {
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
        referenceAvatar = (GameObject)EditorGUILayout.ObjectField("PC Avatar", referenceAvatar, typeof(GameObject), true);
        EditorGUILayout.LabelField("Select the avatar you want to convert for the quest.", EditorStyles.wordWrappedLabel);

        GUILayout.Space(10);
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        GUILayout.Space(10);

        GUILayout.Box("(OPTIONAL)Select a MatCap texture for the avatar. Makes it look cooler :3", GUILayout.ExpandWidth(true), GUILayout.Height(20));

        matCapTexture = (Texture2D)EditorGUILayout.ObjectField("MatCap Texture", matCapTexture, typeof(Texture2D), false);

        GUILayout.Space(10);
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        GUILayout.Space(10);

        GUILayout.Space(10);
        GUILayout.Box("Default value is 2048. Lower numbers make the file size smaller, and higher numbers make it bigger.", GUILayout.ExpandWidth(true), GUILayout.Height(20));
        GUILayout.BeginHorizontal();
        GUILayout.Space(10);
        GUILayout.Label("Max Size:       ", GUILayout.ExpandWidth(false), GUILayout.Height(20));
        maxSizeValue = EditorGUILayout.IntSlider(maxSizeValue, 32, 16384);
        GUILayout.EndHorizontal();
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Compression:", GUILayout.ExpandWidth(false), GUILayout.Height(20));
        compressionValue = EditorGUILayout.IntSlider(compressionValue, 1, 100);
        GUILayout.EndHorizontal();
        GUILayout.Space(10);

        GUILayout.Space(10);
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        GUILayout.Space(10);

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

        if (GUILayout.Button("Load Mesh Components"))
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

    //===================================================================================================================

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
                textureImporter.textureCompression = TextureImporterCompression.Compressed;
                textureImporter.SaveAndReimport();
            }
            deleteToggles.Clear();
        }

        GameObject duplicateAvatar = DuplicateAvatar();
        ClearParticles(duplicateAvatar);
        string scriptLocation = Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this)));
        mainPath = $"{scriptLocation}/{duplicateAvatar.name}";
        EnsureFolderExists(scriptLocation, duplicateAvatar.name);
        ClearFolder(materialsPath);
        texturesPath = CreateSubDirectory(mainPath, "Textures");
        materialsPath = CreateSubDirectory(mainPath, "Materials");

        Texture2D duplicatedTexture = null;
        if (matCapTexture != null)
        {
            //Do same for texutres cant think rn idk just remmeber
        }

        //This Can be turned to assetPath instead  for better flexibility
        DuplicateAndAssignMaterials(duplicateAvatar, duplicatedTexture, materialsPath);
        DeletePhysBones(duplicateAvatar);
        Selection.activeObject = duplicateAvatar;
    }

    private void DuplicateAndAssignMaterials(GameObject duplicateAvatar, Texture2D matCapSlot, string materialsPath)
    {
        Material duplicatedMaterial = null;
        var originalRenderers = LinkRenderMaterials(referenceAvatar);
        foreach (var rendererTuple in originalRenderers)
        {
            Renderer duplicateRenderer = FindRendererByName(duplicateAvatar, rendererTuple.Item1); //Item1 is the tuple's Renderer name.

            // If a matching renderer is found in the duplicate avatar. Not really needed but just in case.
            if (duplicateRenderer != null)
            {
                // Iterate through linked renderers and for their Mesh Renderer
                //This makes sure every Quest Material is applied to the correct Mesh
                //This is also a list because each renderer can have multiple materials even if it's one the data structure is still a list
                Material[] newMaterials = new Material[rendererTuple.Item2.Length];
                Debug.Log("New materials created using amount of materials added to the renderer");
                for (int i = 0; i < rendererTuple.Item2.Length; i++)
                {
                    Debug.Log("Renderer: " + rendererTuple.Item1 + " | Material: " + rendererTuple.Item2[i].name);
                    Material originalMaterial = rendererTuple.Item2[i];
                    string materialPath = "";

                    //debugging
                    //var MaterialsTest = "";
                    //foreach (var material in rendererTuple.Item2) { MaterialsTest.Insert(MaterialsTest.Length,material.name); }
                    //Define the path for the duplicated material
                    //Debug.Log(rendererTuple.Item1 + " [Renderer + Materials] " + MaterialsTest);

                    if (originalMaterial != null)
                    {
                        materialPath = $"{materialsPath}/{originalMaterial.name}_Quest.mat";
                    }
                    // Check if the material already exists at the specified path
                    // Without this check, a material that is reused in the original avatar would have missing references except for the last material.
                    Debug.Log("Created list for materials for this renderer");
                    if (!System.IO.File.Exists(materialPath) && originalMaterial != null)
                    {
                        duplicatedMaterial = new Material(originalMaterial);
                        var f = false;
                        //Particles cannot be converted the same as regular materials
                        if (duplicatedMaterial.shader.name.ToLower().Contains("particle") && rendererTuple.Item2.Length==0)
                        {
                            duplicatedMaterial.shader = Shader.Find("VRChat/Mobile/Particles/Additive");
                            f = true;
                        }
                        else
                        {
                            duplicatedMaterial.shader = Shader.Find("VRChat/Mobile/MatCap Lit");

                            // Just so you can convert without a MatCap Texture
                            if (matCapSlot != null)
                            {
                                duplicatedMaterial.SetTexture("_MatCap", matCapSlot);
                            }
                            if (duplicatedMaterial != null)
                                AssetDatabase.CreateAsset(duplicatedMaterial, materialPath);
                        }
                        // Duplicate the textures used by the material
                        var mainTexture = originalMaterial.mainTexture as Texture2D;

                        //If mainttexture doesn't have a texture it will return null, so it must skip this.
                        if (mainTexture != null || f)
                        {
                            var duplicatedMainTexture = DuplicateTexture(mainTexture, materialsPath);

                            //
                            TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath($"{materialsPath}/{mainTexture.name}_Quest.png");
                            Debug.Log($"{materialsPath}/{mainTexture.name}_Quest.png");
                            importer.crunchedCompression = true;
                            importer.maxTextureSize = maxSizeValue;
                            importer.compressionQuality = compressionValue;
                            importer.SaveAndReimport();
                            duplicatedMaterial.mainTexture = duplicatedMainTexture;
                        }

                    }
                    else
                    {
                        //Apply pre-exsisting converted material
                        if (originalMaterial != null)
                        duplicatedMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    }

                    // Load the duplicated material from the asset database and assigns it to the new materials array
                    if (originalMaterial == null)
                    {
                        originalMaterial = new Material(Shader.Find("Standard")); // or another default shader
                    }
                    newMaterials[i] = duplicatedMaterial;
                }

                // Assign the new materials array to the sharedMaterials property of the duplicate renderer
                duplicateRenderer.sharedMaterials = newMaterials;
            }
        }

    }

    //Incase matching objhect names are found, for now might change later until I find a better way to identify exact objects.
    public static string GenerateRandomString(int length)
    {
        StringBuilder result = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            result.Append(chars[random.Next(chars.Length)]);
        }
        return result.ToString();
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
        Texture2D newTexture = null;
        string originalPath = AssetDatabase.GetAssetPath(originalTexture);
        string newTexturePath = $"{path}/{originalTexture.name}_Quest.png";

        if (originalPath.Contains("unity_builtin_extra"))
        {
            Debug.LogError("Cannot copy texture from unity_builtin_extra.");
            return null;
        }

        // Check if the texture already exists at the new path and if not then this material has no texture.
        if (File.Exists(newTexturePath))
        {
            File.Delete(newTexturePath);
        }
        FileUtil.CopyFileOrDirectory(originalPath, newTexturePath);
        AssetDatabase.ImportAsset(newTexturePath);
        newTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(newTexturePath);
        return newTexture;
    }


    private List<(string, Material[])> LinkRenderMaterials(GameObject avatar)
    {
        var rendererMaterials = new List<(string, Material[])>();
        Renderer[] renderers = avatar.GetComponentsInChildren<Renderer>(true); //TRUE IS IMPORTANT FOR SOMER REASON TO INCLUDE DISABLED OBJECTS
        foreach (Renderer renderer in renderers)
        {
            //rendererMaterials.Add((renderer.name, renderer.sharedMaterials));
            rendererMaterials.Add((GetObjectPath(renderer.gameObject), renderer.sharedMaterials));
        }
        return rendererMaterials;
    }

    private Renderer FindRendererByName(GameObject avatar, string path)
    {
        Renderer[] renderers = avatar.GetComponentsInChildren<Renderer>(true); //This too
        var dupePath = "";
        if (path.Contains(referenceAvatar.name)) path = path.Replace("/"+referenceAvatar.name, "");
        foreach (Renderer renderer in renderers)
        {
            /*         if (renderer.name == name)
                        {
                            return renderer;
                        }*/
            dupePath = GetObjectPath(renderer.gameObject);
            if (dupePath.Contains("/" + referenceAvatar.name + "_Quest")) dupePath = dupePath.Replace("/"+referenceAvatar.name+"_Quest", "");
            if (dupePath == path)
            {
                Debug.Log(dupePath + " + " + path);
                return renderer;
            }
        }
        return null;
    }

    private string GetObjectPath(GameObject obj)
    {
        string path = "/" + obj.name;

        //while is to not do string name = transform.parent.parent.parent.parent.parent.parent.parent.parent.name;
        //while ((obj.transform.parent.name != referenceAvatar.name) || (obj.transform.parent.name+"_Quest" != referenceAvatar.name+"_Quest"))
        try
        {
            while (obj.transform.parent.gameObject != null)//Only gameobject can be null
            {
                obj = obj.transform.parent.gameObject;
                path = "/" + obj.name + path;
            }
        }
        catch { return path; }
        return path;
    }

    private void EnsureFolderExists(string parentFolder, string newFolder)
    {
        string folderPath = $"{parentFolder}/{newFolder}";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder(parentFolder, newFolder);
        }
    }
    private void ClearFolder(string parentFolder)
    {
        string folderPath = $"{parentFolder}";
        if (AssetDatabase.IsValidFolder(folderPath))
            AssetDatabase.DeleteAsset(folderPath);
        else
            Debug.LogError("The specified folder path is not valid.");
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


    private void LoadMeshComponents(GameObject avatar)
    {
        meshComponents.Clear();
        List<Component> allComponents = new List<Component>();
        allComponents.AddRange(avatar.GetComponentsInChildren<MeshRenderer>(true));
        allComponents.AddRange(avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true));

        foreach (var component in allComponents)
        {
            if (component is Renderer renderer)
            {
                var meshFilter = component.GetComponent<MeshFilter>();
                var mesh = meshFilter != null ? meshFilter.sharedMesh : null;

                if (mesh != null)
                {
                    var meshSize = mesh.bounds.size;
                    meshComponents.Add((mesh, meshSize));
                }
            }
        }
    }

    private void ClearParticles(GameObject avatar)
    {
        List<Component> allComponents = new List<Component>();
        allComponents.AddRange(avatar.GetComponentsInChildren<Component>(true));
        foreach (var component in allComponents)
        {
            if (component.GetType().Name.Contains("Particle"))
            {
                DestroyImmediate(component);
            }
        }
        //Yeah.
        Transform[] allTransforms = avatar.GetComponentsInChildren<Transform>(true);
        foreach (var transform in allTransforms)
        {
            Debug.Log(transform.name);
            if (transform.name == "ThiccWater")
            {
                ClearAllComponents(transform);
            }
            if (transform.name == "ThiccWater")
            {
                ClearAllComponents(transform);
            }
        }
    }

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

    private void ClearAllComponents(Transform transform)
    {
        foreach (Transform child in transform)
        {
            ClearAllComponents(child);
        }

        Component[] components = transform.GetComponents<Component>();
        foreach (var component in components)
        {
            if (!(component is Transform)) // Keep the Transform component
            {
                DestroyImmediate(component);
            }
        }
    }
}

//needs to work on disabled objects - donec

//Textures to be copied to the new folder - done

//Textures be applied onto the new duplicated materials - done

//Textures able to be modified in the editor (compression) - done

//mesh size counter

//VRCFURY Toggles get transferred to the new avatar in a new VRCFury Toggle

//VRC Fury Toggles and VRC Fury FX Controller install too.

//