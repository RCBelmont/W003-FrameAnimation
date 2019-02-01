using System.Collections;
using System.Collections.Generic;
using RaphaelBelmont.FrameAnimation;
using UnityEngine;

public class DrawTest : MonoBehaviour
{
    public DataPack Pack;
    public Shader aa;
    public Mesh mesh;
    public Material mat1;


    public class DrawInfo
    {
        public DataPack Pack;
        public MaterialPropertyBlock Mb = new MaterialPropertyBlock();
        public List<Matrix4x4> Ml = new List<Matrix4x4>();
    }

    private DrawInfo _drawInfo;
    private Material mat;
    private List<DrawInfo> _dl;

    void Start()
    {
        mat = new Material(aa);
        //mat = new Material(Shader.Find("Unlit/TT"));
        mat.enableInstancing = true;
        _dl = new List<DrawInfo>();
        _drawInfo = new DrawInfo();
        _drawInfo.Pack = Pack;
        

        List<float> startTimeL = new List<float>();
        List<Vector4> clipInfoL = new List<Vector4>();
        List<Vector4> colorL = new List<Vector4>();

        for (int i = 0; i < 90; i++)
        {
            for (int j = 0; j < 90; j++)
            {
                _drawInfo.Ml.Add(Matrix4x4.TRS(new Vector3(i - 20 / 2.0f, 0, j), Quaternion.identity, Vector3.one * 8));
                //_drawInfo.Ml.Add(Matrix4x4.TRS(new Vector3(i - 20 / 2.0f, 0, j), Quaternion.identity, Vector3.one * 8));
                startTimeL.Add(0);
                colorL.Add(Color.white);
                clipInfoL.Add(Pack.ClipStartIdx[(j) % Pack.ClipName.Count]);
                if (_drawInfo.Ml.Count >= 500)
                {
                    _drawInfo.Mb.SetVector("_dataTexInfo", new Vector4(Pack.DataTexSize.x, Pack.DataTexSize.y, 0, 0));
                    _drawInfo.Mb.SetTexture("_MainTex", Pack.Sheet);
                    _drawInfo.Mb.SetTexture("_AlphaTex", Pack.DataTex);
                    _drawInfo.Mb.SetVectorArray("_ClipInfo", clipInfoL);
                    _drawInfo.Mb.SetVectorArray("_Color", colorL);
                    _drawInfo.Mb.SetFloatArray("_StartTime", startTimeL);
                    _dl.Add(_drawInfo);
                    _drawInfo = new DrawInfo();
                    startTimeL = new List<float>();
                    clipInfoL = new List<Vector4>();
                    colorL = new List<Vector4>();
                }
            }
        }

        if (_drawInfo.Ml.Count > 0)
        {
            _drawInfo.Mb.SetVector("_dataTexInfo", new Vector4(Pack.DataTexSize.x, Pack.DataTexSize.y, 0, 0));
            _drawInfo.Mb.SetTexture("_MainTex", Pack.Sheet);
            _drawInfo.Mb.SetTexture("_AlphaTex", Pack.DataTex);
            _drawInfo.Mb.SetVectorArray("_ClipInfo", clipInfoL);
            _drawInfo.Mb.SetVectorArray("_Color", colorL);
            _drawInfo.Mb.SetFloatArray("_StartTime", startTimeL);
            _dl.Add(_drawInfo);
        }
    }

    // Update is called once per frame
    void Update()
    {
        Shader.SetGlobalFloat("_NowTime", Time.time);
       
        foreach (DrawInfo drawInfo in _dl)
        {
           
            Graphics.DrawMeshInstanced(mesh, 0, mat, drawInfo.Ml, drawInfo.Mb);
        }
    }
}