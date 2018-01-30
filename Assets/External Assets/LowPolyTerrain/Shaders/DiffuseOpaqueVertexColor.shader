Shader "Custom/DiffuseOpaqueVertexColor" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Diffuse lighting model, and enable shadows on all light types
		#pragma surface surf Lambert fullforwardshadows

		sampler2D _MainTex;

		struct Input {
			float4 color : COLOR;
		};

		fixed4 _Color;

		void surf (Input IN, inout SurfaceOutput  o) {
			// Albedo comes from a texture tinted by color
			fixed4 c = IN.color * _Color;
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
