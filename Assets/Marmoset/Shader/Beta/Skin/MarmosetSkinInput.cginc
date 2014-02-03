// Marmoset Skyshop
// Copyright 2013 Marmoset LLC
// http://marmoset.co

//#ifndef MARMOSET_INPUT_CGINC
//#define MARMOSET_INPUT_CGINC

sampler2D 	_MainTex;
half4 		ExposureIBL; //IBL intensities
half2		ExposureLM; //IBL intensities when lightmapping
half2		UniformOcclusion;

//NOTE: deprecated _ExposureIBL, it caused hlsl2glsl errors on Android

float4x4	SkyMatrix;
float4x4	InvSkyMatrix;

#if defined(MARMO_OCCLUSION) || defined(MARMO_VERTEX_OCCLUSION)
half		_OccStrength;
#endif

#ifdef MARMO_OCCLUSION
sampler2D	_OccTex;
#endif

#if defined(MARMO_DIFFUSE_DIRECT) || defined(MARMO_DIFFUSE_IBL)
float4		_Color;
#endif

#ifdef MARMO_DIFFUSE_IBL
samplerCUBE _DiffCubeIBL;
float 	_InvHDR;
#endif

#if defined(MARMO_SPECULAR_DIRECT) || defined(MARMO_SPECULAR_IBL)

#ifndef MARMO_DIFFUSE_SPECULAR_COMBINED
	sampler2D	_SpecTex;
#endif
//float4	_SpecColor; //defined by unity
float		_SpecInt;
float		_Shininess;
float		_Fresnel;
#endif

#ifdef MARMO_SPECULAR_IBL
samplerCUBE _SpecCubeIBL;
#endif

#ifdef MARMO_NORMALMAP
sampler2D 	_BumpMap;
#endif

#ifdef MARMO_GLOW
sampler2D	_Illum;
float4		_GlowColor;
float		_GlowStrength;
float		_EmissionLM;
#endif

float  _NormalSmoothing;

#ifdef MARMO_DETAIL
float  _DetailWeight;
float4 _DetailTiling;
sampler2D _DetailMap;
#endif

float _Subdermis;
float4 _SubdermisColor;
#ifdef MARMO_SUBDERMIS_MAP
sampler2D _SubdermisTex;
#endif

#ifdef MARMO_SPECULAR_ANISO
float _Aniso;
float _AnisoDir;
#endif

float _Translucency;
float _TranslucencySky;
float4 _TranslucencyColor;
#ifdef MARMO_TRANSLUCENCY_MAP
sampler2D _TranslucencyMap;
#endif

float   _Fuzz;	
float4	_FuzzColor;
float 	_FuzzScatter;
float 	_FuzzOcc;

struct Input {
	float2 uv_MainTex;
	#ifdef MARMO_OCCLUSION
		float2 texcoord1;
	#endif
	//#ifdef MARMO_NORMALMAP
		float3 worldNormal; //internal, required for the WorldNormalVector macro
	//#endif
	#if defined(MARMO_SPECULAR_DIRECT) || defined(MARMO_SPECULAR_IBL) || defined(MARMO_SKIN_IBL) || defined(MARMO_SKIN_DIRECT)
		float3 viewDir;
	#endif
	#ifdef MARMO_SPECULAR_IBL
		float3 worldRefl; //internal, required for the WorldReflVector macro
	#endif
	#if defined(MARMO_VERTEX_COLOR) || defined(MARMO_VERTEX_OCCLUSION)
		half4 color : COLOR;
	#endif
	INTERNAL_DATA
};

struct MarmosetSkinOutput {
	half3 Albedo;	//diffuse map RGB
	half Alpha;		//diffuse map A
	half3 Normal;	//world-space normal
	half3 Emission;	//contains IBL contribution
	half Specular;	//specular exponent (required by Unity)
	#ifdef MARMO_SPECULAR_DIRECT
		half3 SpecularRGB;	//specular mask
	#endif
	#if defined(MARMO_SKIN_DIRECT) || defined(MARMO_SKIN_IBL)
		half3 Subdermis;
		half3 Translucency;
		half  Fuzz;
	#endif
};
//#endif