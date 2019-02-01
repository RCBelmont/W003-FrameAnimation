using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Sprites;

namespace RaphaelBelmont.FrameAnimation
{
    public class PackerLib : Editor
    {
        //关联文件判断以获取设置文件存储路径
        [SerializeField] public UnityEngine.Object SettingFolder;
        //模块单例
        private static PackerLib _instance;
        public static PackerLib Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PackerLib();
                }

                return _instance;
            }
        }
        //打包结果
        private enum PackResult
        {
            
            PackSuccess,//打包成功
            PackFail//打包失败
        }

       
        /// <summary>
        /// 检查并获取设置文件, 若不存在会创建一个
        /// </summary>
        /// <returns></returns>
        public static PackerSetting CheckSeetingData()
        {
            PackerSetting packerSetting;
            string parentPath = "Assets";
            if (Instance.SettingFolder)
            {
                parentPath = AssetDatabase.GetAssetPath(Instance.SettingFolder);
            }

            packerSetting = AssetDatabase.LoadAssetAtPath<PackerSetting>(parentPath + "/PackerSetting.asset");
            if (!packerSetting)
            {
                PackerSetting setting = CreateInstance<PackerSetting>();
                AssetDatabase.CreateAsset(setting, parentPath + "/PackerSetting.asset");
            }

            packerSetting = AssetDatabase.LoadAssetAtPath<PackerSetting>(parentPath + "/PackerSetting.asset");
            return packerSetting;
        }

        /// <summary>
        /// Asset右键菜单打包指定目录
        /// </summary>
        /// <param name="cmd"></param>
        [MenuItem("Assets/PackSpriteSheet", false, 200)]
        public static void PackThisFolder(MenuCommand cmd)
        {
            PackerSetting packerSetting = CheckSeetingData();
            if (packerSetting.OneKeyDst == null || packerSetting.OneKeySrc == null)
            {
                PackerWindow.OpenWin();
                return;
            }

            string srcPath = AssetDatabase.GetAssetPath(packerSetting.OneKeySrc);
            string dstPath = AssetDatabase.GetAssetPath(packerSetting.OneKeyDst);
            List<string> packPathList = new List<string>();
            foreach (string guid in Selection.assetGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (path.Contains(srcPath))
                {
                    packPathList.Add(path);
                }
                OneKeyPack(srcPath, packPathList.ToArray(), dstPath, packerSetting.Gap,
                    packerSetting.SheetMaxSize);
            }
        }
        /// <summary>
        /// 获取父路径(Assets相对路径)
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetParentRelativePath(string path)
        {
            Regex r = new Regex("/[^/]+$");
            return r.Replace(path, "");
        }

        /// <summary>
        /// 一键打包
        /// </summary>
        /// <param name="srcRoot">配置的资源根目录</param>
        /// <param name="src">当前要打包的路径</param>
        /// <param name="dst">输出目录</param>
        /// <param name="gap">纹理间隔</param>
        /// <param name="maxSize">图集最大尺寸限制</param>
        public static void OneKeyPack(string srcRoot, string[] src, string dst, int gap, Vector2 maxSize)
        {
            string[] texGUIDL = AssetDatabase.FindAssets("t:texture", src);
            Dictionary<string, List<Texture2D>> folderTexDic = new Dictionary<string, List<Texture2D>>();
            foreach (string guid in texGUIDL)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string parent = GetParentRelativePath(path);
                parent = parent.Replace(srcRoot, "");
                if (!folderTexDic.ContainsKey(parent))
                {
                    folderTexDic[parent] = new List<Texture2D>();
                }

                Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (t && folderTexDic[parent].IndexOf(t) == -1)
                {

                    folderTexDic[parent].Add(t);
                }
            }

            int progress = 1;
            List<string> successL = new List<string>();
            List<string> failList = new List<string>();
            foreach (KeyValuePair<string, List<Texture2D>> pair in folderTexDic)
            {
                EditorUtility.DisplayProgressBar("拼图中...",
                    "当前处理目录: " + pair.Key + "(" + progress + "/" + folderTexDic.Count + ")",
                    progress * 1.0f / folderTexDic.Count);

                if (pair.Value.Count > 0)
                {
                    PackResult packR = PackOneFolder(pair, dst, gap, maxSize);
                    if (packR == PackResult.PackFail)
                    {
                        failList.Add(pair.Key);
                    }
                    else
                    {
                        successL.Add(pair.Key);
                    }
                }

                progress++;
            }

            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("提示", "拼图完成\n" + "成功:" + successL.Count + "    失败:" + failList.Count + "\n 失败信息请看控制台输出", "确认");
            foreach (string s in failList)
            {
                Debug.Log("拼图" + s + " 失败, 生成图集过大");
            }
        }
        /// <summary>
        /// 打包一个目录
        /// </summary>
        /// <param name="folderInfo">目录信息, 相对路径 + 纹理列表</param>
        /// <param name="dst">输出目录</param>
        /// <param name="gap">纹理间隔</param>
        /// <param name="maxSize">最大尺寸限制</param>
        /// <returns></returns>
        private static PackResult PackOneFolder(KeyValuePair<string, List<Texture2D>> folderInfo, string dst, int gap,
            Vector2 maxSize)
        {
            List<SpriteInfo> spL = TexL2SpInfoL(folderInfo.Value, gap);
            string savePath = dst + folderInfo.Key;
          
            PackResult packR = CraeteSheetTexture(spL, savePath, maxSize);
            if (packR == PackResult.PackFail)
            {
                return packR;
            }
            var otherInfo = CreateLutTexture(spL, savePath);
            CrateDataPack(otherInfo, savePath);
            return packR;
        }
        /// <summary>
        /// 创建数据包
        /// </summary>
        /// <param name="otherInfo">其他动画片段信息</param>
        /// <param name="savePath">输出路径</param>
        private static void CrateDataPack(KeyValuePair<List<Vector2>, List<string>> otherInfo, string savePath)
        {
            string folderName = GetFolderName(savePath);
            bool newCraete = false;
            DataPack dp = AssetDatabase.LoadAssetAtPath<DataPack>(savePath + "/" + folderName + "_DataPack.asset");
            if (dp == null)
            {
                dp = DataPack.Create();
                newCraete = true;
            }


            dp.SetTex(AssetDatabase.LoadAssetAtPath<Texture2D>(savePath + "/" + folderName + "_Sheet.png"),
                AssetDatabase.LoadAssetAtPath<Texture2D>(savePath + "/" + folderName + "_Lut.asset"));
            dp.ClipName = otherInfo.Value;
            dp.ClipStartIdx = otherInfo.Key;
            if (newCraete)
            {
                AssetDatabase.CreateAsset(dp, savePath + "/" + folderName + "_DataPack.asset");
                AssetDatabase.Refresh();
            }
            else
            {
                EditorUtility.SetDirty(dp);
                AssetDatabase.SaveAssets();
            }
        }
        /// <summary>
        /// 创建查询纹理
        /// </summary>
        /// <param name="spL">精灵信息列表</param>
        /// <param name="savePath">输出路径</param>
        /// <returns></returns>
        private static KeyValuePair<List<Vector2>, List<string>> CreateLutTexture(List<SpriteInfo> spL, string savePath)
        {
            Dictionary<string, List<SpriteInfo>> dic = new Dictionary<string, List<SpriteInfo>>();

            foreach (SpriteInfo info in spL)
            {
                Sprite sp = info.Sp;
                if (sp)
                {
                    KeyValuePair<string, int> cfInfo = GetClipNameAndFrameIdx(sp.texture.name);
                    List<SpriteInfo> sL;
                    if (!dic.TryGetValue(cfInfo.Key, out sL))
                    {
                        sL = new List<SpriteInfo>();
                        dic[cfInfo.Key] = sL;
                    }

                    sL.Add(info);
                }
            }

            string[] keyL1 = dic.Keys.ToArray();
            List<string> keyL = new List<string>();
            foreach (string s in keyL1)
            {
                keyL.Add(s);
            }

            keyL.Sort();
            foreach (string key in keyL)
            {
                List<SpriteInfo> vL = dic[key];
                vL.Sort((a, b) =>
                {
                    KeyValuePair<string, int> aInfo = GetClipNameAndFrameIdx(a.Sp.texture.name);
                    KeyValuePair<string, int> bInfo = GetClipNameAndFrameIdx(b.Sp.texture.name);
                    if (aInfo.Value > bInfo.Value)
                    {
                        return 1;
                    }
                    else if (aInfo.Value == bInfo.Value)
                    {
                        return 0;
                    }

                    return -1;
                });
            }


            Vector2 dataTexSize = EstimateTexSize(spL.Count * 2);

            int frameCount = 0;
            Texture2D dataTex = GenerateRawTex((int)dataTexSize.x, (int)dataTexSize.y, TextureFormat.RGBAHalf, false);
            dataTex.filterMode = FilterMode.Point;
            Color[] color = dataTex.GetPixels();
            List<Vector2> startIdx = new List<Vector2>();
            List<string> nameList = new List<string>();
            foreach (string key in keyL)
            {
                List<SpriteInfo> vL = dic[key];
                List<SpriteInfo> l = vL;
                for (int i = 0; i < l.Count; i++)
                {
                    int idx = (frameCount + i) * 2;

                    Vector4 uvRange = l[i].OutterUV;
                    Vector4 vRange = l[i].VertexRange;
                    color[idx] = uvRange;
                    color[idx + 1] = vRange;
                }

                nameList.Add(key);
                startIdx.Add(new Vector2(frameCount, l.Count));
                frameCount += l.Count;
            }

            dataTex.SetPixels(color);
            dataTex.Apply();
            string folderName = GetFolderName(savePath);
            AssetDatabase.CreateAsset(dataTex, savePath + "/" + folderName + "_Lut.asset");
            return new KeyValuePair<List<Vector2>, List<string>>(startIdx, nameList);
        }
        /// <summary>
        /// 获取帧的动画剪辑名称以及帧索引
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static KeyValuePair<string, int> GetClipNameAndFrameIdx(string name)
        {
            Regex r = new Regex("_[0-9]+$");
            Match m = r.Match(name);

            if (m.Success)
            {
                int idx = m.Index;
                string clipName = name.Substring(0, idx);
                string frameIdxStr = name.Substring(idx + 1);
                int frameIdx = Int32.Parse(frameIdxStr);
                return new KeyValuePair<string, int>(clipName, frameIdx);
            }

            return new KeyValuePair<string, int>("", -1);
        }
        /// <summary>
        /// 生成一张空纹理
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="f"></param>
        /// <param name="mip"></param>
        /// <returns></returns>
        private static Texture2D GenerateRawTex(int width, int height, TextureFormat f = TextureFormat.RGBA32,
            bool mip = false)
        {
            Texture2D tex = new Texture2D(width, height, f, mip);
            if (f == TextureFormat.RGBA32 || f == TextureFormat.ARGB32 || f == TextureFormat.RGB24 ||
                f == TextureFormat.Alpha8)
            {
                Color[] cs = tex.GetPixels();
                for (int i = 0; i < cs.Length; i++)
                {
                    cs[i] = Color.clear;
                }

                tex.SetPixels(cs);
                tex.Apply();
            }

            tex.alphaIsTransparency = true;

            return tex;
        }
        /// <summary>
        /// 生成图集
        /// </summary>
        /// <param name="spL"></param>
        /// <param name="savePath"></param>
        /// <param name="maxSize"></param>
        /// <returns></returns>
        private static PackResult CraeteSheetTexture(List<SpriteInfo> spL, string savePath, Vector2 maxSize)
        {
            Vector2 sheetSize = TryPack(spL);
            if (sheetSize.x > maxSize.x || sheetSize.y > maxSize.y)
            {
                return PackResult.PackFail;
            }
            CheckPath(savePath);
            Texture2D sheet = GenerateRawTex((int)sheetSize.x, (int)sheetSize.y);
            foreach (SpriteInfo spriteInfo in spL)
            {
                Texture2D tempTexture = GenerateRawTex(spriteInfo.Sp.texture.width, spriteInfo.Sp.texture.height,
                    spriteInfo.Sp.texture.format, spriteInfo.Sp.texture.mipmapCount > 1);
                tempTexture.hideFlags = HideFlags.DontSave;
                Graphics.CopyTexture(spriteInfo.Sp.texture, tempTexture);
                //AssetDatabase.CreateAsset(tempTexture, "Assets/PackResult/" + spriteInfo.Sp.texture.name + ".asset");
                Color[] srcColors = tempTexture.GetPixels((int)spriteInfo.ImgRect.x,
                    (int)spriteInfo.ImgRect.y,
                    (int)spriteInfo.ImgRect.width,
                    (int)spriteInfo.ImgRect.height, 0);
                DestroyImmediate(tempTexture);
                Vector4 block = new Vector4(spriteInfo.PackRect.x + spriteInfo.Gap,
                    spriteInfo.PackRect.y + spriteInfo.Gap,
                    spriteInfo.PackRect.width - 2 * spriteInfo.Gap, spriteInfo.PackRect.height - 2 * spriteInfo.Gap);
                sheet.SetPixels((int)block.x, (int)block.y, (int)block.z, (int)block.w, srcColors);
                sheet.Apply();
                spriteInfo.OutterUV = new Vector4(block.x / sheet.width, block.y / sheet.height,
                    (block.x + block.z) / sheet.width, (block.y + block.w) / sheet.height);
            }

            byte[] bL = sheet.EncodeToPNG();
            CheckPath(savePath);
            string folderName = GetFolderName(savePath);
            File.WriteAllBytes(Path.GetFullPath(savePath) + "/" + folderName + "_Sheet.png", bL);
            AssetDatabase.Refresh();
            return PackResult.PackSuccess;
        }
        //检查路径
        private static void CheckPath(string savePath)
        {
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
                AssetDatabase.Refresh();
            }
        }
        //获取文件夹名称
        private static string GetFolderName(string path)
        {
            Regex r = new Regex("/[^/]+$");
            Match m = r.Match(path);
            return m.Groups[0].Value.Substring(1);
        }
        //拼接计算
        private static Vector2 TryPack(List<SpriteInfo> spL)
        {
            float totalArea = 0;
            foreach (SpriteInfo info in spL)
            {
                totalArea += info.Area;
            }

            Vector2 size = EstimateTexSize(totalArea);

            MaxRectsBinPack mbp = new MaxRectsBinPack((int)size.x, (int)size.y, false);
            bool sw = false;
            bool retry = false;
            while (true)
            {
                retry = false;
                foreach (var spinfo in spL)
                {
                    Rect r = mbp.Insert((int)spinfo.Rect.width, (int)spinfo.Rect.height,
                        MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestAreaFit);
                    if (r.x <= 0 && r.y <= 0 && r.width <= 0 && r.height <= 0)
                    {
                        retry = true;
                        if (sw)
                        {
                            size.x *= 2;
                            sw = false;
                            break;
                        }

                        size.y *= 2;
                        sw = true;
                        break;
                    }

                    spinfo.PackRect = r;
                }

                if (retry)
                {
                    mbp.Init((int)size.x, (int)size.y, false);
                }
                else
                {
                    break;
                }
            }

            return size;
        }
        //估算纹理大小
        private static Vector2 EstimateTexSize(float pixCount)
        {
            Vector2 size = Vector2.one * 2;
            while (true)
            {
                if (size.x * size.y >= pixCount)
                {
                    break;
                }

                size.x *= 2;
                if (size.x * size.y >= pixCount)
                {
                    break;
                }

                size.y *= 2;
            }

            return size;
        }
        //纹理转精灵信息
        private static List<SpriteInfo> TexL2SpInfoL(List<Texture2D> texL, int gap)
        {
            List<SpriteInfo> retL = new List<SpriteInfo>();
            foreach (Texture2D tex in texL)
            {
                Sprite sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100);
                retL.Add(new SpriteInfo(sp, gap));
            }

            retL.Sort((a, b) =>
            {
                if (a.Area < b.Area)
                {
                    return -1;
                }
                else if (a.Area == b.Area)
                {
                    return 0;
                }

                return 1;
            });
            return retL;
        }
        //精灵信息类
        public class SpriteInfo
        {
            public Sprite Sp;
            public Vector4 OutterUV;
            public Vector4 VertexRange;
            public Rect Rect;
            public Rect PackRect;
            public Rect ImgRect;
            public float Area;
            public int Gap;

            public SpriteInfo(Sprite _sp, int gap)
            {
                Sp = _sp;
                OutterUV = DataUtility.GetOuterUV(Sp);
                VertexRange = GetVertexRange(Sp);
                var padding = DataUtility.GetPadding(Sp);
                Rect = new Rect(0, 0, (int)(Sp.rect.width - padding.x - padding.z) + 2 * gap,
                    (int)(Sp.rect.height - padding.y - padding.w + 2 * gap));
                ImgRect = new Rect(padding.x, padding.y, Rect.width - 2 * gap, Rect.height - 2 * gap);
                Area = Rect.width * Rect.height;
                Gap = gap;
            }

            private Vector4 GetVertexRange(Sprite sp)
            {
                var padding = DataUtility.GetPadding(sp);
                var size = new Vector2(sp.rect.width, sp.rect.height);

                int spriteW = Mathf.RoundToInt(size.x);
                int spriteH = Mathf.RoundToInt(size.y);
                return new Vector4(
                    padding.x / spriteW,
                    padding.y / spriteH,
                    (spriteW - padding.z) / spriteW,
                    (spriteH - padding.w) / spriteH);
            }
        }
    }
}