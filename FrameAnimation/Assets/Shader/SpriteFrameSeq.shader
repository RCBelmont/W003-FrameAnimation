Shader "Effect/SpriteFrameSeq"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" { }
        _AlphaTex ("Texture", 2D) = "white" { }
        _Color ("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 100
        ZWrite On
        Cull off
        //Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            
            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            
            #include "UnityCG.cginc"
            inline float4 JzyxBillboardTrans(float4 vertex, int axis)
            {
                if (axis == 0)
                {
                    return UnityObjectToClipPos(vertex);
                }
                half3 wPos = mul(unity_ObjectToWorld, vertex);
                half2 fixAxis = axis == 1? half2(0, 1): half2(1, 0);
                half3 up = normalize(mul(UNITY_MATRIX_V, mul(unity_ObjectToWorld, half4(fixAxis.xy, 0, 0))).xyz);
                half3 right = normalize(cross(up, half3(0, 0, 1)));
                half3 forward = normalize(cross(up, right));
                half4 or = float4(UnityObjectToViewPos(half4(0.0, 0.0, 0.0, 1.0)), 1);
                half4 orX = float4(UnityObjectToViewPos(half4(1.0, 0.0, 0.0, 1.0)), 1);
                half scaleX = length(unity_WorldToObject[0].xyz);
                half scaleY = length(unity_WorldToObject[1].xyz);
                
                half4 pos = float4(vertex.x * right / scaleX + vertex.y * up / scaleX + vertex.z * forward, 1);
                return mul(UNITY_MATRIX_P, or + pos);
            }
            struct appdata
            {
                float4 vertex: POSITION;
                float2 uv: TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float2 uv: TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex: SV_POSITION;
                float4 test: COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            sampler2D _MainTex;
            sampler2D _AlphaTex;
            float4 _MainTex_ST;
            
            float2 _dataTexInfo;
            float _NowTime;
            
            UNITY_INSTANCING_BUFFER_START(Props)
            
            UNITY_DEFINE_INSTANCED_PROP(float4, _ClipInfo)
            UNITY_DEFINE_INSTANCED_PROP(float, _StartTime)
            UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
            UNITY_INSTANCING_BUFFER_END(Props)
            
            half2 GetUV(float idx)
            {
                float row = idx / _dataTexInfo.x;
                float column = idx % _dataTexInfo.x;
                return half2(column / _dataTexInfo.x, row / _dataTexInfo.y);
            }
            
            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                
                v2f o;
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                float2 clipIdx = UNITY_ACCESS_INSTANCED_PROP(Props, _ClipInfo);
                float startTime = UNITY_ACCESS_INSTANCED_PROP(Props, _StartTime);
                float totalFrame = floor((_NowTime - startTime) * 15);
                float actFrame = totalFrame % 8;
                float idx = (clipIdx.x + actFrame) * 2;
                //idx = (3) * 2;
                half2 uv1 = GetUV(idx);
                half2 uv2 = GetUV(idx + 1);
                float4 data1 = tex2Dlod(_AlphaTex, half4(uv1, 0, 0));
                float4 data2 = tex2Dlod(_AlphaTex, half4(uv2, 0, 0));
                
                o.test = data1;
                
                
                
                half4 frameUV = data1; //UNITY_ACCESS_INSTANCED_PROP(Props, _FrameUV);
                half4 vRange = data2;//UNITY_ACCESS_INSTANCED_PROP(Props, _VRange);
                
                v.vertex.xy = v.vertex.xy + 0.5;
                v.vertex = lerp(float4(vRange.x, vRange.y, v.vertex.z, v.vertex.w), float4(vRange.z, vRange.w, v.vertex.z, v.vertex.w), v.vertex);
                v.vertex.xy = v.vertex.xy - 0.5;
                
                o.vertex = JzyxBillboardTrans(v.vertex, 1);;
                o.uv = v.uv;
                o.uv = lerp(frameUV.xy, frameUV.zw, o.uv.xy);
                
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }
            
            fixed4 frag(v2f i): SV_Target
            {
                // sample the texture
                UNITY_SETUP_INSTANCE_ID(i);
                fixed4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                fixed4 col = tex2D(_MainTex, i.uv) * color;
                //fixed4 cola = tex2D(_AlphaTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                //return i.test;
                clip(col.a - 0.5);
                return half4(col.rgb, col.a);
            }
            ENDCG
            
        }
    }
}
