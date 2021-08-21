using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

public class LipSyncOptimizer : EditorWindow
{
    private static GameObject _avatar;
    private AnimationClip _mouthOffAnim;
    private static bool _japanese = true;
    private const string LAYER_PREFIX = "LipSyncOptimizer";
    private const string PARAM_VISEME = "Viseme";

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
            "リップシンクに関するもの (VRC.v_～から始まる名前のもの) を除く、口に関する Blend Shape をすべて 0 にしたアニメーションを指定してください。",
            "Specify an animation with all blend shapes for the mouth set to 0, !!! expect for lip sync (Starts with \"VRC.v_\") !!!"
        );
        EditorGUILayout.HelpBox(m_animInfo.GetString(), MessageType.Info);
        var avatarValidity = ValidateAvatar();
        var validity = avatarValidity && ValidateAnimation();
        using (new EditorGUI.DisabledScope(!validity))
        {
            EditorGUILayout.Space();
            var m_start = new Messages("スタート", "Start");
            if (GUILayout.Button(m_start.GetString()))
            {
                Clean();
                Apply();
            }
        }
        using (new EditorGUI.DisabledScope(!avatarValidity))
        {
            EditorGUILayout.Space();
            var m_clean = new Messages("元に戻す", "Clean");
            if (GUILayout.Button(m_clean.GetString()))
            {
                Clean();
            }
        }
    }

    private bool ValidateAnimation()
    {
        var ret = true;
        if (!_mouthOffAnim)
        {
            var m = new Messages(
                "アニメーションが指定されていません。",
                "Animation is not specified."
            );
            EditorGUILayout.HelpBox(m.GetString(), MessageType.Error);
            ret = false;
        }
        return ret;
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
        for (var i = 0; i < controller.layers.Length; i++)
        {
            var layer = controller.layers[i];
            if (layer.name.StartsWith(LAYER_PREFIX))
            {
                controller.RemoveLayer(i);
            }
        }
    }
    private void Apply()
    {
        var controller = GetFXController();
        controller.AddParameter(PARAM_VISEME, AnimatorControllerParameterType.Int);
        var layer = NewLayer(LAYER_PREFIX);
        var disable = layer.stateMachine.AddState("Disable");
        var enable = layer.stateMachine.AddState("Enable");
        enable.motion = _mouthOffAnim;

        var tracking = enable.AddStateMachineBehaviour<VRCAnimatorTrackingControl>();
        tracking.trackingMouth = VRC_AnimatorTrackingControl.TrackingType.Tracking;

        enable.writeDefaultValues = false;
        disable.writeDefaultValues = false;
        var to_enable = disable.AddTransition(enable, false);
        to_enable.AddCondition(AnimatorConditionMode.NotEqual, 0.0f, PARAM_VISEME);
        to_enable.duration = 0.0f;
        var to_disable = enable.AddTransition(disable, false);
        to_disable.AddCondition(AnimatorConditionMode.Equals, 0.0f, PARAM_VISEME);
        to_disable.duration = 0.0f;
        controller.AddLayer(layer);
    }
    private VRCAvatarDescriptor GetAvatarDescriptor()
    {
        VRCAvatarDescriptor descriptor;
        try
        {
            descriptor = _avatar.GetComponent<VRCAvatarDescriptor>();
        }
        catch
        {
            descriptor = null;
        }
        return descriptor;
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
