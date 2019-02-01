using System.Collections.Generic;
using UnityEngine;

namespace RaphaelBelmont.FrameAnimation
{
    public class DataPack : ScriptableObject
    {
        [SerializeField] public Texture2D Sheet;
        [SerializeField] public Texture2D DataTex;
        [SerializeField] public List<string> ClipName = new List<string>();
        [SerializeField] public List<Vector2> ClipStartIdx = new List<Vector2>();
        [SerializeField] public Vector2 DataTexSize = Vector2.one;

        public void SetTex(Texture2D sheet, Texture2D dataTex)
        {
            Sheet = sheet;
            DataTex = dataTex;
            DataTexSize = new Vector2(dataTex.width, dataTex.height);

        }

        public static DataPack Create()
        {
            DataPack i = CreateInstance<DataPack>();
            return i;
        }
    }
}