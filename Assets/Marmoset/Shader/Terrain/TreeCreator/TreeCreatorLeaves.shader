Shader "Marmoset/Nature/Tree Creator Leaves" {
Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_Shininess ("Shininess", Range (0.01, 1)) = 0.078125
	_MainTex ("Base (RGB) Alpha (A)", 2D) = "white" {}
	_BumpMap ("Normalmap", 2D) = "bump" {}
	_GlossMap ("Gloss (A)", 2D) = "black" {}
	_TranslucencyMap ("Translucency (A)", 2D) = "white" {}
	_ShadowOffset ("Shadow Offset (A)", 2D) = "black" {}
	
	// These are here only to provide default values
	_Cutoff ("Alpha cutoff", Range(0,1)) = 0.3
	_Scale ("Scale", Vector) = (1,1,1,1)
	_SquashAmount ("Squash", Float) = 1
}

SubShader { 
	Tags { "IgnoreProjector"="True" "RenderType"="TreeLeaf" }
	LOD 200
		
CGPROGRAM
#pragma surface surf TreeLeaf alphatest:_Cutoff vertex:TreeVertLeaf addshadow nolightmap
#pragma exclude_renderers flash
#pragma multi_compile MARMO_LINEAR MARMO_GAMMA

#pragma glsl_no_auto_normalization
#include "Tree.cginc"

#define MARMO_SKY_ROTATION
//#define MARMO_SPECULAR_DIRECT
// no specular, it looks more or less terrible.

#include "../../MarmosetCore.cginc"

sampler2D _MainTex;
sampler2D _BumpMap;
sampler2D _GlossMap;
sampler2D _TranslucencyMap;
half _Shininess;

half4 		ExposureIBL;
samplerCUBE _DiffCubeIBL;
#ifdef MARMO_SKY_ROTATION
float4x4	SkyMatrix;
#endif

struct Input {
	float2 uv_MainTex;
	fixed4 color : COLOR; // color.a = AO
};

void surf (Input IN, inout LeafSurfaceOutput o) {
	fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
	o.Albedo = c.rgb * _Color.rgb * IN.color.a;
	o.Translucency = tex2D(_TranslucencyMap, IN.uv_MainTex).rgb;
	o.Alpha = c.a;
	
	#ifdef MARMO_SPECULAR_DIRECT
		o.Specular = _Shininess;
		o.Gloss = tex2D(_GlossMap, IN.uv_MainTex).a;
		o.Gloss *= ExposureIBL.w;
		o.Gloss *=  specEnergyScalar(o.Specular*128);
	#else
		o.Specular = 0.0;
		o.Gloss = 0.0;
	#endif
	
	o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
	float3 N = o.Normal;
	#ifdef MARMO_SKY_ROTATION
		N = mulVec3(SkyMatrix,N);
	#endif
	o.Albedo *= ExposureIBL.w;
	o.Emission = diffCubeLookup(_DiffCubeIBL, N) * o.Albedo * ExposureIBL.x;
}
ENDCG
}

Dependency "OptimizedShader" = "Hidden/Marmoset/Nature/Tree Creator Leaves Optimized"
FallBack "Diffuse"
}
