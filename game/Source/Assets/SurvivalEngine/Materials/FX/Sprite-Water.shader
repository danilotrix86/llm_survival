// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Sprites/Water"
{
	Properties
	{
		_MainTex("Sprite Texture", 2D) = "white" {}
		[NoScaleOffset] _FlowMap("Flow (RG)", 2D) = "black" {}
		_Color("Tint", Color) = (1,1,1,1)
		[MaterialToggle] PixelSnap("Pixel snap", Float) = 0
		[HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
		[HideInInspector] _Flip("Flip", Vector) = (1,1,1,1)
		[PerRendererData] _AlphaTex("External Alpha", 2D) = "white" {}
		[PerRendererData] _EnableExternalAlpha("Enable External Alpha", Float) = 0
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
		[Toggle(Z_WRITE)] _ZWrite ("Z Write", Float) = 0

		_BorderColor("Border color", Color) = (1, 1, 1, 1)
		_BorderWidth("Border width", Range(0, 1)) = 0
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PreviewType" = "Plane"
			"CanUseSpriteAtlas" = "True"
		}

		Cull Off
		Lighting Off
		ZWrite [_ZWrite]
		ZTest [_ZTest]
		Blend One OneMinusSrcAlpha

		CGPROGRAM
#pragma surface surf Lambert vertex:vert nofog keepalpha noinstancing
#pragma shader_feature _ Z_WRITE
#pragma multi_compile _ PIXELSNAP_ON
#pragma multi_compile _ ETC1_EXTERNAL_ALPHA
#include "UnitySprites.cginc"

		sampler2D _FlowMap;

		fixed4 _BorderColor;
		fixed _BorderWidth;

		struct Input
		{
			float2 uv_MainTex;
			fixed4 color;
		};

		void vert(inout appdata_full v, out Input o)
		{
			v.vertex.xy *= _Flip.xy;

	#if defined(PIXELSNAP_ON)
			v.vertex = UnityPixelSnap(v.vertex);
	#endif

			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.color = v.color * _Color * _RendererColor;
		}

		float2 FlowUV(float2 uv, float2 flowVector, float time) {
			float progress = cos(time/2.0) / 30.0;
			return uv - flowVector * progress;
		}

		void surf(Input IN, inout SurfaceOutput o)
		{
			float2 flowVector = tex2D(_FlowMap, IN.uv_MainTex).rg * 2 - 1;
			float2 uv = FlowUV(IN.uv_MainTex, flowVector, _Time.y);
			fixed4 c = SampleSpriteTexture(uv) * IN.color;
			#if defined(Z_WRITE)
				clip(c.a - 0.04); //SKIP ZWrite for alpha
			#endif

			o.Albedo = c.rgb * c.a;
			o.Alpha = c.a;
		}
	ENDCG
	}

	Fallback "Transparent/VertexLit"
}
