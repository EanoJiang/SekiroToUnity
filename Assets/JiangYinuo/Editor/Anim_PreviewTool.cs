// Author: EanoJiang
// Created: 2025-07-28
// Description: Animation Preview Tool
// Copyright © EanoJiang
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class Anim_PreviewTool : EditorWindow
{
    private Object unityAsset;
    private Editor assetEditor;
    private bool isAnimationClip;

    // 动画文件夹和动画列表相关字段
    private DefaultAsset folderAsset;
    private List<AnimationClip> animationClips = new List<AnimationClip>();
    private List<string> animationClipNames = new List<string>();
    private int selectedClipIndex = -1;
    private Vector2 animListScrollPos;

    [MenuItem("XSJArtEditorTools/JiangYinuo/2.动画预览工具")]
    public static void OpenWindow()
    {
        Anim_PreviewTool window = EditorWindow.GetWindow<Anim_PreviewTool>();
        
        // 设置窗口标题
        window.titleContent = new GUIContent("动画预览工具");
        
        // 设置窗口大小，确保动画预览区域能完全显示
        float windowWidth = 300f + 512f + 50f;      // 左侧面板300px + 右侧预览区域512px + 一些边距和控件空间
        float windowHeight = 512f + 100f;           // 预览区域512px + 控件空间100px
        
        // 获取Unity编辑器窗口的位置和大小
        Rect editorWindowRect = EditorGUIUtility.GetMainWindowPosition();
        float maxWidth = editorWindowRect.width * 0.8f; // 最大宽度为编辑器窗口宽度的80%
        float maxHeight = editorWindowRect.height * 0.8f; // 最大高度为编辑器窗口高度的80%
        
        // 限制窗口大小
        windowWidth = Mathf.Min(windowWidth, maxWidth);
        windowHeight = Mathf.Min(windowHeight, maxHeight);
        
        // 设置窗口位置（相对于Unity编辑器窗口居中）
        float x = editorWindowRect.x + (editorWindowRect.width - windowWidth) * 0.5f;
        float y = editorWindowRect.y + (editorWindowRect.height - windowHeight) * 0.5f;
        
        // 确保窗口不会超出屏幕边界
        x = Mathf.Max(0, Mathf.Min(x, Screen.currentResolution.width - windowWidth));
        y = Mathf.Max(0, Mathf.Min(y, Screen.currentResolution.height - windowHeight));
        
        window.position = new Rect(x, y, windowWidth, windowHeight);
        
        // 设置最小窗口大小
        window.minSize = new Vector2(800f, 600f);
        
        window.Show();
    }

    void OnEnable()
    {
        // 自适应窗口缩放
        if (position.width < 800f || position.height < 600f)
        {
            // 获取Unity编辑器窗口的位置和大小
            Rect editorWindowRect = EditorGUIUtility.GetMainWindowPosition();
            
            // 设置合适的大小
            float windowWidth = Mathf.Min(862f, editorWindowRect.width * 0.8f);
            float windowHeight = Mathf.Min(612f, editorWindowRect.height * 0.8f);
            
            // 设置窗口位置相对于Unity编辑器窗口居中
            float x = editorWindowRect.x + (editorWindowRect.width - windowWidth) * 0.5f;
            float y = editorWindowRect.y + (editorWindowRect.height - windowHeight) * 0.5f;
            
            // 限制窗口大小
            x = Mathf.Max(0, Mathf.Min(x, Screen.currentResolution.width - windowWidth));
            y = Mathf.Max(0, Mathf.Min(y, Screen.currentResolution.height - windowHeight));
            
            position = new Rect(x, y, windowWidth, windowHeight);
        }
        
        // 设置最小窗口大小
        minSize = new Vector2(800f, 600f);
    }

    void OnDisable()
    {
        CleanupEditor();
    }

    void OnDestroy()
    {
        CleanupEditor();
    }

    void OnRectChanged()
    {
        // 当窗口大小改变时重新绘制
        Repaint();
    }

    private void OnGUI()
    {
        // 使用水平布局，左侧显示动画列表，右侧显示预览
        EditorGUILayout.BeginHorizontal();
        
        // 左侧：动画列表界面
        EditorGUILayout.BeginVertical(GUILayout.Width(300));
        DrawAnimationListInterface();
        EditorGUILayout.EndVertical();
        
        // 右侧：预览界面
        EditorGUILayout.BeginVertical();
        CheckAsset();
        ShowPreview();
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.EndHorizontal();
    }

    private void DrawAnimationListInterface()
    {

        // 文件夹选择
        EditorGUI.BeginChangeCheck();
        folderAsset = (DefaultAsset)EditorGUILayout.ObjectField("动画文件夹", folderAsset, typeof(DefaultAsset), false);
        if (EditorGUI.EndChangeCheck())
        {
            CleanupEditor();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("加载动画列表"))
        {
            LoadAnimationClips();
        }

        if (GUILayout.Button("清除列表"))
        {
            ClearAnimationList();
        }
        EditorGUILayout.EndHorizontal();

        // 显示信息
        if (animationClips.Count > 0)
        {
            EditorGUILayout.HelpBox($"已加载 {animationClips.Count} 个动画文件", MessageType.Info);
        }

        // 动画列表
        if (animationClipNames.Count > 0)
        {
            EditorGUILayout.LabelField("动画列表", EditorStyles.boldLabel);
            
            // 计算可用高度
            float windowHeight = position.height;
            float topMargin = 20f; // 顶部边距
            float labelHeight = EditorGUIUtility.singleLineHeight; // 标签高度
            float buttonHeight = EditorGUIUtility.singleLineHeight * 2; // 按钮区域高度
            float infoBoxHeight = EditorGUIUtility.singleLineHeight * 2; // 信息框高度
            float minListHeight = 150f; // 最小列表高度
            
            // 计算动画列表的可用高度
            float usedHeight = topMargin + labelHeight + buttonHeight + infoBoxHeight + labelHeight;
            float availableHeight = windowHeight - usedHeight - 50f; // 减去一些额外边距
            float listHeight = Mathf.Max(minListHeight, availableHeight);
            
            animListScrollPos = EditorGUILayout.BeginScrollView(animListScrollPos, GUILayout.Height(listHeight));

            for (int i = 0; i < animationClipNames.Count; i++)
            {
                if (i >= animationClips.Count) break;

                bool isSelected = (selectedClipIndex == i);
                GUI.backgroundColor = isSelected ? Color.cyan : Color.white;

                if (GUILayout.Button(animationClipNames[i], EditorStyles.miniButton))
                {
                    SelectAnimation(i);
                }
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndScrollView();
        }
    }

    private void SelectAnimation(int index)
    {
        if (index < 0 || index >= animationClips.Count) return;

        selectedClipIndex = index;
        AnimationClip selectedClip = animationClips[index];
        
        if (selectedClip != null)
        {
            // 清理旧的编辑器
            CleanupEditor();
            
            // 直接设置资源，让CheckAsset处理Editor创建
            unityAsset = selectedClip;
        }
    }

    private void CheckAsset()
    {
        EditorGUI.BeginChangeCheck();
        unityAsset = EditorGUILayout.ObjectField("当前预览资源", unityAsset, typeof(Object), true);
        
        if (EditorGUI.EndChangeCheck() || (unityAsset != null && assetEditor == null))
        {
            isAnimationClip = false;
            CleanupEditor();
            
            if (unityAsset != null)
            {
                var clip = unityAsset as AnimationClip;
                if (clip != null)
                {
                    try
                    {
                        assetEditor = Editor.CreateEditor(clip);
                        if (assetEditor != null)
                        {
                            isAnimationClip = true;
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"创建动画编辑器失败: {e.Message}");
                        assetEditor = null;
                    }
                }
                else
                {
                    try
                    {
                        assetEditor = Editor.CreateEditor(unityAsset);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"创建编辑器失败: {e.Message}");
                        assetEditor = null;
                    }
                }
            }
            
            // 重置列表选择状态
            if (EditorGUI.EndChangeCheck())
            {
                selectedClipIndex = -1;
            }
        }
    }

    private void ShowPreview()
    {
        if (unityAsset == null)
        {
            Rect previewRect = GUILayoutUtility.GetRect(512, 512);
            GUI.Box(previewRect, "请选择要预览的资源", EditorStyles.helpBox);
            return;
        }

        if (assetEditor == null)
        {
            Rect previewRect = GUILayoutUtility.GetRect(512, 512);
            GUI.Box(previewRect, "无法创建预览编辑器", EditorStyles.helpBox);
            return;
        }

        // 检查编辑器和目标是否有效
        if (assetEditor.target == null || assetEditor.target.Equals(null))
        {
            Rect previewRect = GUILayoutUtility.GetRect(512, 512);
            GUI.Box(previewRect, "编辑器目标已失效", EditorStyles.helpBox);
            return;
        }

        try
        {
            // 预览设置 - 对AnimationClip进行特殊处理
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                
                // 只有在编辑器支持预览设置时才调用
                if (assetEditor.HasPreviewGUI())
                {
                    try
                    {
                        assetEditor.OnPreviewSettings();
                    }
                    catch (System.Exception e)
                    {
                        // 如果预览设置失败，显示警告但继续
                        Debug.LogWarning($"预览设置失败: {e.Message}");
                        GUILayout.Label("预览设置不可用", EditorStyles.miniLabel);
                    }
                }
                else
                {
                    GUILayout.Label("无预览设置", EditorStyles.miniLabel);
                }
            }

            // 预览区域
            Rect previewRect = GUILayoutUtility.GetRect(512, 512);
            
            // 检查是否支持预览GUI
            if (!assetEditor.HasPreviewGUI())
            {
                GUI.Box(previewRect, $"'{unityAsset.name}' 不支持预览", EditorStyles.helpBox);
                return;
            }

            try
            {
                if (isAnimationClip)
                {
                    // 对于动画剪辑，尝试使用动画模式
                    AnimationMode.StartAnimationMode();
                    AnimationMode.BeginSampling();
                    
                    try
                    {
                        assetEditor.OnInteractivePreviewGUI(previewRect, EditorStyles.whiteLabel);
                    }
                    finally
                    {
                        AnimationMode.EndSampling();
                        AnimationMode.StopAnimationMode();
                    }
                }
                else
                {
                    assetEditor.OnInteractivePreviewGUI(previewRect, EditorStyles.whiteLabel);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"预览GUI失败: {e.Message}");
                GUI.Box(previewRect, $"预览失败\n{e.Message}", EditorStyles.helpBox);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"预览失败: {e.Message}");
            Rect previewRect = GUILayoutUtility.GetRect(512, 512);
            GUI.Box(previewRect, $"预览失败\n检查控制台获取详细信息", EditorStyles.helpBox);
        }
    }

    private void LoadAnimationClips()
    {
        ClearAnimationList();

        if (folderAsset == null)
        {
            EditorUtility.DisplayDialog("错误", "请先选择一个文件夹", "确定");
            return;
        }

        string folderPath = AssetDatabase.GetAssetPath(folderAsset);

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            EditorUtility.DisplayDialog("错误", "选择的不是有效文件夹", "确定");
            return;
        }

        try
        {
            string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { folderPath });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

                if (clip != null)
                {
                    animationClips.Add(clip);
                    animationClipNames.Add(clip.name);
                }
            }

            if (animationClips.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "在选择的文件夹中没有找到动画文件", "确定");
            }
            else
            {
                Debug.Log($"成功加载 {animationClips.Count} 个动画文件");
            }
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"加载动画时出错: {e.Message}", "确定");
            Debug.LogError($"LoadAnimationClips 错误: {e}");
        }
    }

    private void ClearAnimationList()
    {
        CleanupEditor();
        animationClips.Clear();
        animationClipNames.Clear();
        selectedClipIndex = -1;
        unityAsset = null;
    }

    private void CleanupEditor()
    {
        if (assetEditor != null)
        {
            try
            {
                DestroyImmediate(assetEditor);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"清理编辑器时出错: {e.Message}");
            }
            finally
            {
                assetEditor = null;
            }
        }
    }
} 
#endif