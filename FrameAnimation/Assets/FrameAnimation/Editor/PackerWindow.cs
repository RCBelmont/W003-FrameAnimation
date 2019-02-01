using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RaphaelBelmont.FrameAnimation
{
    public class PackerWindow : EditorWindow
    {
        private PackerSetting _packerSetting;

        [MenuItem("Tools/FrameAnimationPacker")]
        public static void OpenWin()
        {
            EditorWindow win = GetWindow<PackerWindow>();
            win.maxSize = new Vector2(400, 300);
            win.minSize = new Vector2(400, 300);
            win.name = "FrameSheetPacker";
            win.Show();
        }

        void OnEnable()
        {
            //Get or Create SettingData
            CheckSeetingData();
        }

        void CheckSeetingData()
        {
            _packerSetting = PackerLib.CheckSeetingData();
        }

        void OnGUI()
        {
            if (!_packerSetting)
            {
                GUILayout.Label("没有找到配置数据, 请重新导入组件或检查配置文件路径是否有效");
                return;
            }

            GUILayout.Label("一键Pack");
            EditorGUILayout.Space();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("配置文件:", _packerSetting, typeof(Object), false);
            EditorGUI.EndDisabledGroup();
            GUILayout.Label("配置参数:");
            EditorGUI.BeginChangeCheck();
            _packerSetting.OneKeySrc =
                EditorGUILayout.ObjectField("源目录:", _packerSetting.OneKeySrc, typeof(Object), false);
            _packerSetting.OneKeyDst =
                EditorGUILayout.ObjectField("输出目录:", _packerSetting.OneKeyDst, typeof(Object), false);
            _packerSetting.Gap = EditorGUILayout.IntField("间距", _packerSetting.Gap);
            _packerSetting.SheetMaxSize = EditorGUILayout.Vector2Field("拼图尺寸限制:", _packerSetting.SheetMaxSize);
            _packerSetting.SheetMaxSize =
                new Vector2((int) _packerSetting.SheetMaxSize.x, (int) _packerSetting.SheetMaxSize.y);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_packerSetting);
                AssetDatabase.SaveAssets();
            }

            if (GUILayout.Button("开始拼图"))
            {
                string srcPath = AssetDatabase.GetAssetPath(_packerSetting.OneKeySrc);
                string dstPath = AssetDatabase.GetAssetPath(_packerSetting.OneKeyDst);
                PackerLib.OneKeyPack(srcPath, new string[] {srcPath}, dstPath, _packerSetting.Gap,
                    _packerSetting.SheetMaxSize);
            }
        }
    }
}