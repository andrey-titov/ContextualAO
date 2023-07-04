Shader "Custom/RaycastDisplay"
{
    Properties
    {
        _RaycastedImage("Volume Image", 2D) = "white" {}
        //screen_scale("Screen Scale", Float) = 1.0
    }
    SubShader
    {
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha, SrcAlpha DstAlpha
		Pass
		{
			ZTest Off
			ZWrite Off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			sampler2D _RaycastedImage;

			// VERTEX
			float4 vert(float3 vertex : POSITION) : SV_POSITION
			{
				return UnityObjectToClipPos(vertex);
			}

			// FRAGMENT
			float4 frag(/*v2f o, */ UNITY_VPOS_TYPE screenPos : VPOS) : SV_Target
			{
				float2 uv = float2(screenPos.x / _ScreenParams.x, screenPos.y / _ScreenParams.y);

				float4 sampled_color = tex2D(_RaycastedImage, uv);

				return sampled_color;
			}

			ENDHLSL
        }
    }
}
