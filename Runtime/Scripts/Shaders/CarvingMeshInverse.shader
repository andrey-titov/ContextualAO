Shader "Custom/CarvingMeshInverse"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Pass
        {
            Name "ClearZbuffer"

            ZTest Always

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 normal : NORMAL;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = v.normal;
                return o;
            }

            float4 frag(v2f i, out float depth : SV_Depth) : SV_Target
            {
                depth = 1.0; // clear depth
                return float4(0, 0, 0, 0);
            }
            ENDHLSL
        }
        Pass
        {
            Name "RenderWithInvertedZtest"

            ZTest Greater

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 normal : NORMAL;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = mul(unity_ObjectToWorld, float4(v.normal.xyz, 0.f));
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 packedNormal = (normalize(i.normal) + float3(1, 1, 1)) * 0.5;
                return float4(packedNormal.xyz, i.vertex.z);
            }
            ENDHLSL
        }
    }
}
