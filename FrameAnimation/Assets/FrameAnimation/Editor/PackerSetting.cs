using UnityEngine;

namespace RaphaelBelmont.FrameAnimation
{
    public class PackerSetting : ScriptableObject
    {
        [SerializeField] public Object OneKeySrc;
        [SerializeField] public Object OneKeyDst;
        [SerializeField] public int Gap = 0;
        [SerializeField] public Vector2 SheetMaxSize = new Vector2(1024, 1024);

    }
}


