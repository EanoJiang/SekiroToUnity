// Author: EanoJiang
// Created: 2025-07-28
// Description: Model_Anim_Fbx_ImportTool
// Copyright © EanoJiang

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.IO;
using System.Linq;

public class Model_Anim_Fbx_ImportTool : EditorWindow
{
    [MenuItem("XSJArtEditorTools/JiangYinuo/1.一键设置并打包Prefab工具")]
    public static void Open() => GetWindow<Model_Anim_Fbx_ImportTool>("1.一键设置并打包Prefab工具");

    /* ========================= 标签页 ========================= */
    private int selectedTab = 0;
    private readonly string[] tabNames = { "模型FBX导入设置", "动画FBX导入设置" };

    /* ========================= 模型FBX数据 ========================= */
    private string modelFolderPath = "";
    private string[] modelFiles = new string[0];
    private ModelImporter importer;

    /* Rig */
    private ModelImporterAnimationType animationType = ModelImporterAnimationType.Human;
    private ModelImporterAvatarSetup avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
    private bool optimizeGameObjects = false;

    /* Materials */
    private ModelImporterMaterialImportMode materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
    private ModelImporterMaterialLocation materialLocation = ModelImporterMaterialLocation.External;
    private ModelImporterMaterialName materialName = ModelImporterMaterialName.BasedOnModelNameAndMaterialName;
    private ModelImporterMaterialSearch materialSearch = ModelImporterMaterialSearch.RecursiveUp;

    /* 法线贴图导入设置 */
    private TextureImporterType normalMapTextureType = TextureImporterType.NormalMap;
    private TextureImporterShape normalMapTextureShape = TextureImporterShape.Texture2D;

    /* ========================= 动画FBX数据 ========================= */
    private string fbxFolder = "";
    private DefaultAsset folderAsset;

    /* ========================= GUI ========================= */
    private Vector2 scroll;

    private void OnGUI()
    {
        // 标签页选择
        selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
        EditorGUILayout.Space();

        scroll = EditorGUILayout.BeginScrollView(scroll);

        switch (selectedTab)
        {
            case 0:
                DrawModelFbxTab();
                break;
            case 1:
                DrawAnimFbxTab();
                break;
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawModelFbxTab()
    {
        EditorGUILayout.LabelField("模型FBX导入设置", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);
        /* 1. 模型文件夹路径 */

        EditorGUILayout.LabelField("1) 选择模型文件夹", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("拖放Model路径到输入框", MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUI.BeginChangeCheck();
            
            // 创建输入框的Rect用于拖放检测
            Rect textFieldRect = EditorGUILayout.GetControlRect();
            modelFolderPath = EditorGUI.TextField(textFieldRect, "Model Folder Path", modelFolderPath);
            
            // 在输入框上检测拖放
            if (textFieldRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.DragUpdated)
                {
                    bool isValidDrop = false;
                    
                    // 检查是否从Project窗口拖放
                    if (DragAndDrop.paths.Length > 0)
                    {
                        string draggedPath = DragAndDrop.paths[0];
                        
                        // 检查是否为文件夹
                        if (Directory.Exists(draggedPath))
                        {
                            isValidDrop = true;
                        }
                        // 检查是否为Unity中的文件夹资源
                        else if (AssetDatabase.IsValidFolder(draggedPath))
                        {
                            isValidDrop = true;
                        }
                    }
                    
                    DragAndDrop.visualMode = isValidDrop ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Rejected;
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    string droppedPath = DragAndDrop.paths[0];
                    
                    // 处理从Project窗口拖放的文件夹
                    if (AssetDatabase.IsValidFolder(droppedPath))
                    {
                        modelFolderPath = droppedPath;
                    }
                    // 处理从文件系统拖放的文件夹
                    else if (Directory.Exists(droppedPath))
                    {
                        modelFolderPath = FileUtil.GetProjectRelativePath(droppedPath);
                    }
                    
                    RefreshModelFiles();
                    Event.current.Use();
                }
            }
            
            if (EditorGUI.EndChangeCheck())
                RefreshModelFiles();

            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Model Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    modelFolderPath = FileUtil.GetProjectRelativePath(path);
                    RefreshModelFiles();
                }
            }
        }

        /* 显示找到的模型文件 */
        if (modelFiles.Length > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"找到 {modelFiles.Length} 个模型文件:", EditorStyles.boldLabel);
            
            foreach (string modelFile in modelFiles)
            {
                EditorGUILayout.LabelField($"• {Path.GetFileName(modelFile)}", EditorStyles.miniLabel);
            }
        }
        else if (!string.IsNullOrEmpty(modelFolderPath))
        {
            EditorGUILayout.HelpBox("在指定文件夹中未找到模型文件", MessageType.Warning);
        }

        if (importer == null && modelFiles.Length > 0)
        {
            // 使用第一个模型文件作为预览
            RefreshImporter(modelFiles[0]);
        }

        if (importer == null)
        {
            return;
        }

        EditorGUILayout.Space();

        /* 2. 自动设置说明 */
        EditorGUILayout.LabelField("2) 自动应用以下设置", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("批量设置模型导入参数", MessageType.Info);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Rig 设置:", EditorStyles.boldLabel);
        animationType = (ModelImporterAnimationType)EditorGUILayout.EnumPopup("Animation Type", animationType);
        avatarSetup = (ModelImporterAvatarSetup)EditorGUILayout.EnumPopup("Avatar Definition", avatarSetup);
        optimizeGameObjects = EditorGUILayout.Toggle("Optimize Game Objects", optimizeGameObjects);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Materials 设置:", EditorStyles.boldLabel);
        materialImportMode = (ModelImporterMaterialImportMode)EditorGUILayout.EnumPopup("Material Creation Mode", materialImportMode);
        materialLocation = (ModelImporterMaterialLocation)EditorGUILayout.EnumPopup("Location", materialLocation);
        materialName = (ModelImporterMaterialName)EditorGUILayout.EnumPopup("Naming", materialName);
        materialSearch = (ModelImporterMaterialSearch)EditorGUILayout.EnumPopup("Search", materialSearch);

        EditorGUILayout.Space();

        /* 3. 应用按钮 */
        GUI.enabled = modelFiles.Length > 0;
        if (GUILayout.Button($"应用到 {modelFiles.Length} 个模型", GUILayout.Height(30)))
        {
            ApplyToAllModels();
        }
        GUI.enabled = true;

        EditorGUILayout.Space();

        /* 4. 贴图匹配 */
        EditorGUILayout.LabelField("3) 贴图匹配", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("自动匹配路径下的贴图文件到对应材质球,对应关系如下：\n_a(BaseColor)\n_n(Normal)\n_m(Mask)", MessageType.Info);
        
        // 法线贴图导入设置
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("法线贴图导入设置:", EditorStyles.boldLabel);
        normalMapTextureType = (TextureImporterType)EditorGUILayout.EnumPopup("Texture Type", normalMapTextureType);
        normalMapTextureShape = (TextureImporterShape)EditorGUILayout.EnumPopup("Texture Shape", normalMapTextureShape);
        
        if (GUILayout.Button("为每个材质球匹配对应贴图", GUILayout.Height(30)))
        {
            AutoMatchTextures(normalMapTextureType, normalMapTextureShape);
        }
    }

    private void DrawAnimFbxTab()
    {
        EditorGUILayout.LabelField("1) 批量设置动画FBX的Avatar属性", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        // 路径选择，支持拖入文件夹
        EditorGUILayout.LabelField("Fbx Folder Path：");
        EditorGUILayout.BeginHorizontal();
        fbxFolder = EditorGUILayout.TextField(fbxFolder);
        folderAsset = (DefaultAsset)EditorGUILayout.ObjectField(folderAsset, typeof(DefaultAsset), false);
        EditorGUILayout.EndHorizontal();
        if (folderAsset != null)
        {
            string path = AssetDatabase.GetAssetPath(folderAsset);
            if (AssetDatabase.IsValidFolder(path))
            {
                fbxFolder = path;
            }
        }
        GUILayout.Space(10);

        if (GUILayout.Button("批量设置动画FBX的Avatar属性", GUILayout.Height(30)))
        {
            SetFbxAvatar(fbxFolder);
        }

        GUILayout.Space(20);

        EditorGUILayout.LabelField("2) 批量生成Prefab", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("注意：要先在Project面板选中Fbx文件夹 / 多个Fbx文件！！！", MessageType.Info);
        if (GUILayout.Button("批量生成Prefab到Prefab文件夹", GUILayout.Height(30)))
        {
            BatchCreatePrefabs();
        }
    }

    /* ========================= 模型FBX逻辑 ========================= */
    private void RefreshModelFiles()
    {
        if (string.IsNullOrEmpty(modelFolderPath) || !Directory.Exists(modelFolderPath))
        {
            modelFiles = new string[0];
            return;
        }

        // 支持的模型文件扩展名
        string[] supportedExtensions = { ".fbx", ".obj", ".dae", ".3ds" };
        
        modelFiles = Directory.GetFiles(modelFolderPath, "*.*", SearchOption.AllDirectories)
            .Where(file => supportedExtensions.Contains(Path.GetExtension(file).ToLower()))
            .ToArray();
    }

    private void RefreshImporter(string modelPath)
    {
        importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        // 设置为默认值
        if (importer != null)
        {
            animationType = ModelImporterAnimationType.Human;
            avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            optimizeGameObjects = false;

            materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            materialLocation = ModelImporterMaterialLocation.External;
            materialName = ModelImporterMaterialName.BasedOnMaterialName;
            materialSearch = ModelImporterMaterialSearch.RecursiveUp;
        }
    }

    private void ApplyToAllModels()
    {
        if (modelFiles.Length == 0) return;

        int appliedCount = 0;
        
        foreach (string modelFile in modelFiles)
        {
            ModelImporter fileImporter = AssetImporter.GetAtPath(modelFile) as ModelImporter;
            if (fileImporter != null)
            {
                fileImporter.animationType = animationType;
                fileImporter.avatarSetup = avatarSetup;
                fileImporter.optimizeGameObjects = optimizeGameObjects;

                fileImporter.materialImportMode = materialImportMode;
                fileImporter.materialLocation = materialLocation;
                fileImporter.materialName = materialName;
                fileImporter.materialSearch = materialSearch;

                fileImporter.SaveAndReimport();
                appliedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"已应用设置到 {appliedCount} 个模型文件");
    }

    private void AutoMatchTextures(TextureImporterType textureType, TextureImporterShape textureShape)
    {
        if (string.IsNullOrEmpty(modelFolderPath))
        {
            EditorUtility.DisplayDialog("错误", "请先选择模型文件夹", "确定");
            return;
        }

        string materialsPath = Path.Combine(modelFolderPath, "Materials");
        if (!Directory.Exists(materialsPath))
        {
            EditorUtility.DisplayDialog("错误", $"未找到Materials目录: {materialsPath}", "确定");
            return;
        }

        int matchedCount = 0;
        int materialCount = 0;

        // 获取Materials目录下的所有材质文件
        string[] materialFiles = Directory.GetFiles(materialsPath, "*.mat", SearchOption.TopDirectoryOnly);
        
        foreach (string materialFile in materialFiles)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialFile);
            if (material == null) continue;

            materialCount++;
            string materialName = Path.GetFileNameWithoutExtension(materialFile);
            bool materialMatched = false;

            // 从模型文件夹路径中查找对应的贴图文件
            string[] textureFiles = Directory.GetFiles(modelFolderPath, "*.png", SearchOption.AllDirectories);
            
            foreach (string textureFile in textureFiles)
            {
                string textureName = Path.GetFileNameWithoutExtension(textureFile);
                
                // 检查贴图文件名是否以材质名开头
                if (textureName.StartsWith(materialName))
                {
                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(textureFile);
                    if (texture == null) continue;

                    // 根据后缀匹配到对应的材质属性
                    if (textureName.EndsWith("_a"))
                    {
                        // BaseColor (Albedo)
                        material.SetTexture("_MainTex", texture);
                        materialMatched = true;
                        Debug.Log($"匹配BaseColor贴图: {textureName} -> {materialName}");
                    }
                    else if (textureName.EndsWith("_n"))
                    {
                        // 先设置法线贴图的导入设置
                        TextureImporter textureImporter = AssetImporter.GetAtPath(textureFile) as TextureImporter;
                        if (textureImporter != null)
                        {
                            textureImporter.textureType = textureType;
                            textureImporter.textureShape = textureShape;
                            textureImporter.SaveAndReimport();
                            Debug.Log($"设置法线贴图导入设置: {textureName}");
                        }

                        // Normal Map
                        material.SetTexture("_BumpMap", texture);
                        materialMatched = true;
                        Debug.Log($"匹配Normal贴图: {textureName} -> {materialName}");
                    }
                    else if (textureName.EndsWith("_m"))
                    {
                        // Mask (Metallic/Smoothness)
                        material.SetTexture("_MetallicGlossMap", texture);
                        materialMatched = true;
                        Debug.Log($"匹配Mask贴图: {textureName} -> {materialName}");
                    }
                }
            }

            if (materialMatched)
            {
                matchedCount++;
                EditorUtility.SetDirty(material);
            }
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("完成", $"处理了 {materialCount} 个材质，成功匹配 {matchedCount} 个材质的贴图", "确定");
        Debug.Log($"贴图匹配完成: {matchedCount}/{materialCount} 个材质");
    }

    /* ========================= 动画FBX逻辑 ========================= */
    public static void SetFbxAvatar(string fbxFolder)
    {
        // 自动查找以 a000_000000.fbx 结尾的主FBX
        string mainFbxSuffix = "a000_000000.fbx";
        string[] candidates = Directory.GetFiles(fbxFolder, "*.fbx", SearchOption.AllDirectories);
        string mainFbxPath = null;
        foreach (var path in candidates)
        {
            if (Path.GetFileName(path).EndsWith(mainFbxSuffix))
            {
                mainFbxPath = path.Replace("\\", "/");
                break;
            }
        }
        if (mainFbxPath == null)
        {
            Debug.LogError("未找到主FBX（后缀为 a000_000000.fbx）");
            return;
        }
        string mainAvatarPath = mainFbxPath;

        // 先导入主Avatar
        ModelImporter mainImporter = AssetImporter.GetAtPath(mainFbxPath) as ModelImporter;
        if (mainImporter == null)
        {
            Debug.LogError("未找到主FBX: " + mainFbxPath);
            return;
        }
        mainImporter.animationType = ModelImporterAnimationType.Human;
        mainImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        mainImporter.sourceAvatar = null; // 显式清空
        AssetDatabase.ImportAsset(mainFbxPath);

        // 获取主Avatar引用
        GameObject mainFbx = AssetDatabase.LoadAssetAtPath<GameObject>(mainFbxPath);
        if (mainFbx == null)
        {
            Debug.LogError("主FBX加载失败: " + mainFbxPath);
            return;
        }
        Avatar mainAvatar = null;
        foreach (var comp in mainFbx.GetComponentsInChildren<Animator>(true))
        {
            if (comp.avatar != null)
            {
                mainAvatar = comp.avatar;
                break;
            }
        }
        if (mainAvatar == null)
        {
            Debug.LogError("主FBX未生成Avatar，请检查Rig设置。");
            return;
        }

        // 批量设置其他FBX
        string[] fbxFiles = Directory.GetFiles(fbxFolder, "*.fbx", SearchOption.AllDirectories);
        foreach (var fbxPath in fbxFiles)
        {
            string assetPath = fbxPath.Replace("\\", "/");
            if (assetPath == mainFbxPath) continue;

            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null) continue;

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = mainAvatar;
            AssetDatabase.ImportAsset(assetPath);
            Debug.Log($"已设置: {assetPath}");
        }

        Debug.Log("批量设置完成！");
    }

    private void BatchCreatePrefabs()
    {
        var selected = Selection.GetFiltered<Object>(SelectionMode.Assets);
        int prefabCount = 0;
        foreach (var obj in selected)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (Directory.Exists(path))
            {
                // 文件夹，递归查找FBX
                string[] fbxFiles = Directory.GetFiles(path, "*.fbx", SearchOption.AllDirectories);
                foreach (var fbx in fbxFiles)
                {
                    prefabCount += CreatePrefabForFbx(fbx);
                }
            }
            else if (path.ToLower().EndsWith(".fbx"))
            {
                prefabCount += CreatePrefabForFbx(path);
            }
        }
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成", $"共生成{prefabCount}个Prefab。", "确定");
    }

    private int CreatePrefabForFbx(string fbxPath)
    {
        string assetPath = fbxPath.Replace("\\", "/");
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (fbx == null) return 0;
        // 获取FBX文件夹的父目录
        string fbxDir = Path.GetDirectoryName(assetPath).Replace("\\", "/");
        string parentDir = Directory.GetParent(fbxDir).FullName.Replace("\\", "/");
        // 兼容Unity路径
        if (parentDir.Contains("Assets/"))
        {
            int idx = parentDir.IndexOf("Assets/");
            parentDir = parentDir.Substring(idx);
        }
        string prefabDir = Path.Combine(parentDir, "Prefab").Replace("\\", "/");
        if (!AssetDatabase.IsValidFolder(prefabDir))
        {
            string parent = parentDir;
            string newFolder = "Prefab";
            AssetDatabase.CreateFolder(parent, newFolder);
        }
        string prefabName = Path.GetFileNameWithoutExtension(assetPath) + ".prefab";
        string prefabPath = Path.Combine(prefabDir, prefabName).Replace("\\", "/");
        PrefabUtility.SaveAsPrefabAsset(fbx, prefabPath);
        Debug.Log($"生成Prefab: {prefabPath}");
        return 1;
    }
}
#endif 