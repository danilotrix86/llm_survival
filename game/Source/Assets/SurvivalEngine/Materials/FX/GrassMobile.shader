// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "FX/GrassMobile" {
Properties {
    _Color("Color", Color) = (1,1,1,1)
    _MainTex ("Base (RGB)", 2D) = "white" {}
}
SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 150
    Cull Off

CGPROGRAM
#pragma surface surf Lambert noforwardadd

sampler2D _MainTex;
float4 _Color;

struct Input {
    float2 uv_MainTex;
};

void surf (Input IN, inout SurfaceOutput o) {
    fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
    o.Albedo = c.rgb * _Color.rgb;
    o.Alpha = c.a * _Color.a;
}
ENDCG
}

Fallback "Mobile/VertexLit"
}
