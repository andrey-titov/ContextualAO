Shader "Custom/VolumeBoundaries"
{
	Properties
	{
		//_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		// No culling or depth
		//Cull Off ZWrite Off ZTest Always

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 color : COLOR0;
			};

			v2f vert(float3 vertex : POSITION, float4 color : COLOR0)
			{
				float4 pos = UnityObjectToClipPos(vertex);

				v2f o;
				o.vertex = pos;
				o.color = color;
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				return float4(i.color.xyz, i.vertex.z);
			}

			ENDHLSL
		}
	}
}
