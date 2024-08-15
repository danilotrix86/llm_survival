//This shader is based on this free and open source tutorial: https://roystan.net/articles/grass-shader.html
//This is my own take on it, but it's a lot inspired by the tutorial, credits for this specific grass shader goes to Roystan

Shader "FX/Grass"
{
    Properties
    {
		[Header(Shading)]
        _TopColor("Top Color", Color) = (1,1,1,1)
		_BottomColor("Bottom Color", Color) = (1,1,1,1)
		_TranslucentGain("Translucent Gain", Range(0,1)) = 0.5
		//_Scale("Scale", Float) = 1
		
		[Header(Grass Shape)]
		_BladeWidth("Blade Width", Float) = 0.05
		_BladeWidthRandom("Blade Width Random", Float) = 0.02
		_BladeHeight("Blade Height", Float) = 0.5
		_BladeHeightRandom("Blade Height Random", Float) = 0.3
		_BendRotationRandom("Bend Rotation Random", Range(0, 1)) = 0.2
		_BladeForward("Blade Forward Amount", Float) = 0.38
		_BladeCurve("Blade Curvature Amount", Range(1, 4)) = 2

		[Header(Grass Density)]
		_TessellationUniform("Grass Density", Range(1, 64)) = 1

		[Header(Wind)]
		_WindStrength("Wind Strength", Float) = 1
		_WindFrequency("Wind Frequency", Vector) = (0.05, 0.05, 0, 0)
		_WindDistortionMap("Wind Distortion Map", 2D) = "white" {}
		
		[Header(Fallback)]
		_Color("Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
		
    }

	CGINCLUDE
	#include "UnityCG.cginc"
	#include "Autolight.cginc"

	#define BLADE_SEGMENTS 3

	float _BladeHeight;
	float _BladeHeightRandom;	
	float _BladeWidth;
	float _BladeWidthRandom;
	float _BendRotationRandom;
	float _BladeForward;
	float _BladeCurve;
	float _TessellationUniform;

	sampler2D _WindDistortionMap;
	float4 _WindDistortionMap_ST;
	float2 _WindFrequency;
	float _WindStrength;

	// Simple noise function, sourced from http://answers.unity.com/answers/624136/view.html
	// Extended discussion on this function can be found at the following link:
	// https://forum.unity.com/threads/am-i-over-complicating-this-random-function.454887/#post-2949326
	// Returns a number in the 0...1 range.
	float rand(float3 co)
	{
		return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 53.539))) * 43758.5453);
	}

	// Construct a rotation matrix that rotates around the provided axis, sourced from:
	// https://gist.github.com/keijiro/ee439d5e7388f3aafc5296005c8c3f33
	float3x3 AngleAxis3x3(float angle, float3 axis)
	{
		float c, s;
		sincos(angle, s, c);

		float t = 1 - c;
		float x = axis.x;
		float y = axis.y;
		float z = axis.z;

		return float3x3(
			t * x * x + c, t * x * y - s * z, t * x * z + s * y,
			t * x * y + s * z, t * y * y + c, t * y * z - s * x,
			t * x * z - s * y, t * y * z + s * x, t * z * z + c
			);
	}

	struct vertexInput
	{
		float4 vertex : POSITION;
		float3 normal : NORMAL;
		float4 tangent : TANGENT;
	};

	struct vertexOutput
	{
		float4 vertex : SV_POSITION;
		float3 normal : NORMAL;
		float4 tangent : TANGENT;
	};

	struct TessellationFactors 
	{
		float edge[3] : SV_TessFactor;
		float inside : SV_InsideTessFactor;
	};

	struct geometryOutput
	{
		float4 pos : SV_POSITION;
		float2 uv : TEXCOORD0;
		float3 normal : NORMAL;
		unityShadowCoord4 _ShadowCoord : TEXCOORD1;
	};

	geometryOutput VertexOutput(float3 pos, float2 uv, float3 normal)
	{
		geometryOutput o;
		o.pos = UnityObjectToClipPos(pos);
		o.uv = uv;
		o.normal = UnityObjectToWorldNormal(normal);
		o._ShadowCoord = ComputeScreenPos(o.pos);

		#if UNITY_PASS_SHADOWCASTER
			// Applying the bias prevents artifacts from appearing on the surface.
			o.pos = UnityApplyLinearShadowBias(o.pos);
		#endif

		return o;
	}

	geometryOutput GenerateGrassVertex(float3 vertexPosition, float width, float height, float forward, float2 uv, float3x3 transformMatrix)
	{
		float3 tangentPoint = float3(width, forward, height);
		float3 tangentNormal = normalize(float3(0, -1, forward));
		float3 localNormal = mul(transformMatrix, tangentNormal);
		float3 localPosition = vertexPosition + mul(transformMatrix, tangentPoint);
		return VertexOutput(localPosition, uv, localNormal);
	}

	TessellationFactors patchConstantFunction (InputPatch<vertexInput, 3> patch)
	{
		TessellationFactors f;
		float tesselValue =  _TessellationUniform;
		f.edge[0] = tesselValue;
		f.edge[1] = tesselValue;
		f.edge[2] = tesselValue;
		f.inside = tesselValue;
		return f;
	}

	vertexInput vert(vertexInput v)
	{
		return v;
	}

	vertexOutput tessVert(vertexInput v)
	{
		vertexOutput o;
		// Note that the vertex is NOT transformed to clip
		// space here; this is done in the grass geometry shader.
		o.vertex = v.vertex;
		o.normal = v.normal;
		o.tangent = v.tangent;
		return o;
	}

	[UNITY_domain("tri")]
	[UNITY_outputcontrolpoints(3)]
	[UNITY_outputtopology("triangle_cw")]
	[UNITY_partitioning("integer")]
	[UNITY_patchconstantfunc("patchConstantFunction")]
	vertexInput hull (InputPatch<vertexInput, 3> patch, uint id : SV_OutputControlPointID)
	{
		return patch[id];
	}

	[UNITY_domain("tri")]
	vertexOutput domain(TessellationFactors factors, OutputPatch<vertexInput, 3> patch, float3 barycentricCoordinates : SV_DomainLocation)
	{
		vertexInput v;

		#define MY_DOMAIN_PROGRAM_INTERPOLATE(fieldName) v.fieldName = \
			patch[0].fieldName * barycentricCoordinates.x + \
			patch[1].fieldName * barycentricCoordinates.y + \
			patch[2].fieldName * barycentricCoordinates.z;

		MY_DOMAIN_PROGRAM_INTERPOLATE(vertex)
		MY_DOMAIN_PROGRAM_INTERPOLATE(normal)
		MY_DOMAIN_PROGRAM_INTERPOLATE(tangent)

		return tessVert(v);
	}

	[maxvertexcount(BLADE_SEGMENTS * 2 + 1)]
	void geo(triangle vertexOutput IN[3] : SV_POSITION, inout TriangleStream<geometryOutput> triStream)
	{
		float3 pos = IN[0].vertex;
		geometryOutput o;

		float3 vNormal = IN[0].normal;
		float4 vTangent = IN[0].tangent;
		float3 vBinormal = cross(vNormal, vTangent) * vTangent.w;

		float3x3 tangentToLocal = float3x3(
			vTangent.x, vBinormal.x, vNormal.x,
			vTangent.y, vBinormal.y, vNormal.y,
			vTangent.z, vBinormal.z, vNormal.z
		);

		float2 windSample = float2(0, 1);
		if(_WindStrength > 0.01){
			float2 wind_uv = pos.xz * _WindDistortionMap_ST.xy + _WindDistortionMap_ST.zw + _WindFrequency * _Time.y;
			windSample = (tex2Dlod(_WindDistortionMap, float4(wind_uv, 0, 0)).xy * 2 - 1) * _WindStrength;
		}
		float3 wind = normalize(float3(windSample.x,  windSample.y, 0));
		float3x3 windRotation = AngleAxis3x3(UNITY_PI * windSample, wind);

		float3x3 facingRotationMatrix = AngleAxis3x3(rand(pos) * UNITY_TWO_PI, float3(0, 0, 1));
		float3x3 bendRotationMatrix = AngleAxis3x3(rand(pos.zzx) * _BendRotationRandom * UNITY_PI * 0.5, float3(-1, 0, 0));
		float3x3 transformationMatrix = mul(mul(mul(tangentToLocal, windRotation), facingRotationMatrix), bendRotationMatrix);
		float3x3 transformationMatrixFacing = mul(tangentToLocal, facingRotationMatrix);

		float height = (rand(pos.zyx) * 2 - 1) * _BladeHeightRandom + _BladeHeight;
		float width = (rand(pos.xzy) * 2 - 1) * _BladeWidthRandom + _BladeWidth;
		float forward = rand(pos.yyz) * _BladeForward;

		for (int i = 0; i < BLADE_SEGMENTS; i++)
		{
			float t = i / (float)BLADE_SEGMENTS;
			float segmentHeight = height * t;
			float segmentWidth = width * (1 - t);
			float segmentForward = pow(t, _BladeCurve) * forward;
			float3x3 transformMatrix = i == 0 ? transformationMatrixFacing : transformationMatrix;
			triStream.Append(GenerateGrassVertex(pos, segmentWidth, segmentHeight, segmentForward, float2(0, t), transformMatrix));
			triStream.Append(GenerateGrassVertex(pos, -segmentWidth, segmentHeight, segmentForward, float2(1, t), transformMatrix));
		}
		triStream.Append(GenerateGrassVertex(pos, 0, height, forward, float2(0.5, 1), transformationMatrix));

		//triStream.Append(GenerateGrassVertex(pos, width, 0, float2(0, 0), transformationMatrixFacing));
		//triStream.Append(GenerateGrassVertex(pos, -width, 0, float2(1, 0), transformationMatrixFacing));
		//triStream.Append(GenerateGrassVertex(pos, 0, height, float2(0.5, 1), transformationMatrix));
	}
	ENDCG

    SubShader
    {
		Cull Off

        Pass
        {
			Tags
			{
				"RenderType" = "Opaque"
				"LightMode" = "ForwardBase"
			}

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma geometry geo
			#pragma hull hull
			#pragma domain domain
			#pragma target 4.6
			#pragma multi_compile_fwdbase

            
			#include "Lighting.cginc"

			float4 _TopColor;
			float4 _BottomColor;
			float _TranslucentGain;

			float4 frag (geometryOutput i, fixed facing : VFACE) : SV_Target
            {	
				//return lerp(_BottomColor, _TopColor, i.uv.y);

				float3 normal = facing > 0 ? i.normal : -i.normal;
				//return float4(normal * 0.5 + 0.5, 1);

				float shadow = SHADOW_ATTENUATION(i);
				float NdotL = saturate(saturate(dot(normal, _WorldSpaceLightPos0)) + _TranslucentGain) * shadow;

				float3 ambient = ShadeSH9(float4(normal, 1));
				float4 lightIntensity = NdotL * _LightColor0 + float4(ambient, 1);
				float4 col = lerp(_BottomColor * lightIntensity, _TopColor * lightIntensity, i.uv.y);

				return col;
            }
            ENDCG
        }

		Pass
		{
			Tags
			{
				"LightMode" = "ShadowCaster"
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geo
			#pragma fragment frag
			#pragma hull hull
			#pragma domain domain
			#pragma target 4.6
			#pragma multi_compile_shadowcaster

			float4 frag(geometryOutput i) : SV_Target
			{
				SHADOW_CASTER_FRAGMENT(i)
			}

			ENDCG
		}
    }

	Fallback "FX/GrassMobile"
}