using UnityEditor;
using UnityEngine;

public class Tool : EditorWindow
{   
   public GameObject model =null;
   public static bool isUp = false;

   public static Tool m_mainWindow;
   [MenuItem("XSJArtTools/FixBoneRotation")]

   
   public static void OpenWindow()
   {
        m_mainWindow = EditorWindow.GetWindow<Tool>(); //创建窗口
        m_mainWindow.Show(); //打开窗口
   }

    private void OnGUI()
    {
        GUILayout.BeginVertical("HelpBox");
        GUILayout.Space(5);

        GUILayout.BeginHorizontal();
        GUILayout.Label("需要修正的标准骨架Avatar", GUILayout.Width(200));
        model = EditorGUILayout.ObjectField(model, typeof(GameObject), true, GUILayout.ExpandWidth(true), GUILayout.MinWidth(200)) as GameObject;
        GUILayout.Space(10);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("修复选择模型的Avatar", GUILayout.Width(200), GUILayout.ExpandWidth(true)))
        {
            // 用延迟调用，避免OnGUI栈错乱
            var m = model;
            EditorApplication.delayCall += () => FixBone(m);
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

    }


    public static void FixBone(GameObject model)
    {

        //var model = Selection.activeGameObject;
        if (model == null || !IsHumanoid(model))
        {
            Debug.LogError("请选择配置了Humanoid Avatar的模型！");
            return;
        }                                                                                                         


        var animator = model.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("模型缺少Animator组件！");
            return;
        }

        // 获取左手Distal骨骼
        //大拇指
        Transform LeftThumbDistal = animator.GetBoneTransform(HumanBodyBones.LeftThumbDistal);
        //食指
        Transform LeftIndexDistal = animator.GetBoneTransform(HumanBodyBones.LeftIndexDistal);
        //中指
        Transform LeftMiddleDistal = animator.GetBoneTransform(HumanBodyBones.LeftMiddleDistal);
        //无名指
        Transform LeftRingDistal = animator.GetBoneTransform(HumanBodyBones.LeftRingDistal);
        //小拇指
        Transform LeftLittleDistal = animator.GetBoneTransform(HumanBodyBones.LeftLittleDistal);

        // 获取右手Distal骨骼
        //大拇指
        Transform RightThumbDistal = animator.GetBoneTransform(HumanBodyBones.RightThumbDistal);
        //食指
        Transform RightIndexDistal = animator.GetBoneTransform(HumanBodyBones.RightIndexDistal);
        //中指
        Transform RightMiddleDistal = animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal);
        //无名指
        Transform RightRingDistal = animator.GetBoneTransform(HumanBodyBones.RightRingDistal);
        //小拇指
        Transform RightLittleDistal = animator.GetBoneTransform(HumanBodyBones.RightLittleDistal);


        //左手归零
        LeftThumbDistal.localRotation = Quaternion.identity;
        LeftIndexDistal.localRotation = Quaternion.identity;
        LeftMiddleDistal.localRotation = Quaternion.identity;
        LeftRingDistal.localRotation = Quaternion.identity;
        LeftLittleDistal.localRotation = Quaternion.identity;


    
        //右手归零
        RightThumbDistal.localRotation = Quaternion.identity;
        RightIndexDistal.localRotation = Quaternion.identity;
        RightMiddleDistal.localRotation = Quaternion.identity;
        RightRingDistal.localRotation = Quaternion.identity;
        RightLittleDistal.localRotation = Quaternion.identity;

        isUp = true;

        //animator.ApplyModifiedProperties();
        EditorUtility.SetDirty(animator);
        AssetDatabase.SaveAssets();

    }

    // 检查模型是否为Humanoid
    private static bool IsHumanoid(GameObject model)
    {
        var animator = model.GetComponent<Animator>();
        return animator != null && animator.isHuman;
    }

}

