using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;

public class LipSyncOptimizer : EditorWindow
{
    private static GameObject _avatar;
    private AnimationClip _mouthOffAnim;
    private static bool _japanese;
    private const string LAYER_PREFIX = "LipSyncOptimizer";

    [MenuItem("GameObject/LipSyncOptimizer", false, 20)]
    public static void Initialize()
    {
        _japanese = true;
        _avatar = Selection.activeGameObject;
        var window = EditorWindow.GetWindow(typeof(LipSyncOptimizer), false, "LipsyncOptimizer", true);
        window.Show();
    }
    private void OnGUI()
    {
        EditorGUILayout.Space();
        if (_japanese)
        {
            if (GUILayout.Button("Switch to English"))
            {
                _japanese = false;
            }
        }
        else
        {
            if (GUILayout.Button("日本語に切り替える"))
            {
                _japanese = true;
            }
        }
        GUILayout.Label("LipSyncOptimizer", EditorStyles.boldLabel);
        ++EditorGUI.indentLevel;
        var m_avatar = new Messages("アバター", "Avatar");
        _avatar = EditorGUILayout.ObjectField(m_avatar.GetString(), _avatar, typeof(GameObject), true) as GameObject;
        var m_anim = new Messages("アニメーション", "Animation");
        _mouthOffAnim = EditorGUILayout.ObjectField(m_anim.GetString(), _mouthOffAnim, typeof(AnimationClip), true) as AnimationClip;
        var m_animInfo = new Messages(
            "リップシンクを除く、口に関する Blend Shape をすべて 0 にしたアニメーションを指定してください。",
            "Specify an animation which sets Blend Shapes other than lip sync 0."
        );
        EditorGUILayout.HelpBox(m_animInfo.GetString(), MessageType.Info);
        var validity = ValidateAvatar();
        using (new EditorGUI.DisabledScope(!validity))
        {
            EditorGUILayout.Space();
            var m_start = new Messages("スタート", "Start");
            if (GUILayout.Button(m_start.GetString()))
            {
                Clean();
                Apply();
            }
            EditorGUILayout.Space();
            var m_clean = new Messages("元に戻す", "Clean");
            if (GUILayout.Button(m_clean.GetString()))
            {
                Clean();
            }
        }
    }
    private bool ValidateAvatar()
    {
        var ret = true;
        if (!_avatar)
        {
            var m = new Messages(
                "アバターが指定されていません。",
                "Avatar is not specified.");
            EditorGUILayout.HelpBox(m.GetString(), MessageType.Error);
            ret = false;
        }
        if (!_mouthOffAnim)
        {
            var m = new Messages(
                "アニメーションが指定されていません。",
                "Animation is not specified."
            );
            EditorGUILayout.HelpBox(m.GetString(), MessageType.Error);
            ret = false;
        }
        var descriptor = GetAvatarDescriptor();
        if (_avatar && !descriptor)
        {
            var m = new Messages(
                "指定されたオブジェクトはアバターではありません。",
                "The specified object is not an avatar.");
            EditorGUILayout.HelpBox(m.GetString(), MessageType.Error);
            ret = false;
        }
        if (_avatar && descriptor && !GetFXController())
        {
            var m = new Messages(
                "アバターに FX レイヤの Animator Controller を設定してください",
                "The specified avatar doesn't have FX Layer Controller.");
            EditorGUILayout.HelpBox(m.GetString(), MessageType.Error);
            ret = false;
        }
        return ret;
    }
    private static AnimatorController FindLayer(VRCAvatarDescriptor descriptor, VRCAvatarDescriptor.AnimLayerType layertype)
    {
        foreach (var layer in descriptor.baseAnimationLayers)
        {
            if (layer.type == layertype)
            {
                if (layer.animatorController)
                    return (AnimatorController)layer.animatorController;
                else
                    return null;
            }
        }
        return null;
    }
    private static AnimatorControllerLayer NewLayer(string layerName)
    {
        var layer = new AnimatorControllerLayer
        {
            name = layerName,
            defaultWeight = 1.0f,
            stateMachine = new AnimatorStateMachine
            {
                name = layerName,
                hideFlags = HideFlags.HideInHierarchy
            }
        };
        return layer;
    }

    private void Clean()
    {
        var controller = GetFXController();
        foreach (var layer in controller.layers)
        {
            if (layer.name.StartsWith(LAYER_PREFIX))
            {
                controller.RemoveLayer(ArrayUtility.IndexOf(controller.layers, layer));
            }
        }
    }
    private void Apply()
    {
        var controller = GetFXController();
        var layer = NewLayer(LAYER_PREFIX);
        var disable = layer.stateMachine.AddState("Disable");
        var enable = layer.stateMachine.AddState("Enable");
    }
    private VRCAvatarDescriptor GetAvatarDescriptor()
    {
        return _avatar.GetComponent<VRCAvatarDescriptor>();
    }
    private AnimatorController GetFXController()
    {
        var descriptor = GetAvatarDescriptor();
        var controller = FindLayer(descriptor, VRCAvatarDescriptor.AnimLayerType.FX);
        return controller;
    }
    private class Messages
    {
        private static string japanese;
        private static string english;
        public Messages(string ja, string en)
        {
            japanese = ja;
            english = en;
        }
        public string GetString()
        {
            return _japanese ? japanese : english;
        }
    }
}
