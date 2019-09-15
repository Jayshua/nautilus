Shader "Custom/DiffuseFadeVertexColor" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Transparent-1" }
		LOD 200
		
		CGPROGRAM

		// Diffuse lighting model
		#pragma surface surf Lambert fullforwardshadows decal:blend

		sampler2D _MainTex;

		struct Input {
			float4 color : COLOR;
		};

		fixed4 _Color;

		void surf (Input IN, inout SurfaceOutput o) {
			// Albedo comes from a texture tinted by color
			fixed4 c = IN.color * _Color;
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
