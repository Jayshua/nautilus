Shader "Custom/DiffSpecFadeVertexColor" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
		_Shininess ("Shininess", Range (0.01, 1)) = 0.078125
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Transparent-1" }
		LOD 200
		
		CGPROGRAM

		// Diffuse lighting model
		#pragma surface surf BlinnPhong fullforwardshadows decal:blend

		sampler2D _MainTex;

		struct Input {
			float4 color : COLOR;
		};

		fixed4 _Color;
		half _Shininess;

		void surf (Input IN, inout SurfaceOutput o) {
			// Albedo comes from a texture tinted by color
			fixed4 c = IN.color * _Color;
			o.Albedo = c.rgb;
			o.Gloss = IN.color.a;
			o.Specular = _Shininess;
			o.Alpha = _Color.a;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
