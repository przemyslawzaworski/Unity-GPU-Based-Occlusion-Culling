Shader "HardwareOcclusion"
{
	SubShader
	{
		Cull Off
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			CGPROGRAM
			#pragma vertex VSMain
			#pragma fragment PSMain
			#pragma target 5.0

			RWStructuredBuffer<float4> buffer : register(u1);
			int index, debug;

			void VSMain (inout float4 vertex : POSITION)
			{
				vertex = UnityObjectToClipPos(vertex);
			}

			[earlydepthstencil]
			float4 PSMain (float4 vertex : POSITION) : SV_TARGET
			{
				buffer[index] = vertex;
				return float4(0.0, 0.0, 1.0, 0.2 * debug);
			}
			ENDCG
		}
	}
}