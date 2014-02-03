// Marmoset Skyshop
// Copyright 2013 Marmoset LLC
// http://marmoset.co

Shader "Marmoset/Beta/Skin IBL" {
	Properties {
		_Color   ("Diffuse Color", Color) = (1,1,1,1)
		_SpecColor ("Specular Color", Color) = (1,1,1,1)
		_SpecInt ("Specular Intensity", Float) = 1.0
		_Shininess ("Specular Sharpness", Range(2.0,8.0)) = 4.0
		_Fresnel ("Specular Fresnel", Range(0.0,1.0)) = 0.0		
		_MainTex ("Diffuse(RGB) Alpha(A)", 2D) = "white" {}
		_SpecTex ("Specular(RGB) Gloss(A)", 2D) = "white" {}
		_BumpMap ("Normalmap", 2D) 	= "bump" {}
		
		_SubdermisColor ("Subdermis Color", Color) = (1,1,1,1)
		_Subdermis ("Subdermis", Range(0.0,1.0)) = 1.0
		_SubdermisTex("Subdermis(RGB) Skin Mask(A)", 2D) = "white" {}

		_TranslucencyColor ("Translucency Color", Color) = (1,0.5,0.4,1)
		_Translucency ("Translucency", Range(0.0,1.0)) = 0.0
		_TranslucencySky ("Sky Translucency", Range(0.0,1.0)) = 0.0
		_TranslucencyMap ("Translucency Map", 2D) = "white" {}

		_FuzzColor	("Fuzz Color", Color) = (1,1,1,1)
		_Fuzz		("Fuzz", Range(0.0,1.0)) = 0.0
		_FuzzScatter ("Fuzz Scatter", Range(0.0,1.0)) = 1.0
		_FuzzOcc 	("Fuzz Occlusion", Range(0.5,1.0)) = 0.5
		
		_Aniso		("Anisotropy", Range(0.0,1.0)) = 0.0
		_AnisoDir	("Anisotropy Direction", Range(0.0,180.0)) = 0.0
		
		_Cutoff		("Alpha Cutoff", Range(0.0,1.0)) = 0.3
		
		//slots for custom lighting cubemaps
		_SpecCubeIBL ("Custom Specular Cube", Cube) = "black" {}
	}
	
	SubShader {
		Tags {
			"Queue"="AlphaTest"
			"IgnoreProjector"="True"
			"RenderType"="TransparentCutout"
		}
		LOD 400
		//diffuse LOD 200
		//diffuse-spec LOD 250
		//bumped-diffuse, spec 350
		//bumped-spec 400
		
		//mac stuff
		CGPROGRAM
		#ifdef SHADER_API_OPENGL
			#pragma glsl
		#endif
				
		#pragma target 3.0
		#pragma exclude_renderers d3d11_9x
		#pragma surface MarmosetSkinSurf MarmosetSkinDirect fullforwardshadows vertex:MarmosetSkinVert noambient alphatest:_Cutoff
		//gamma-correct sampling permutations
		#pragma multi_compile MARMO_LINEAR MARMO_GAMMA
		
		#define MARMO_HQ
		#define MARMO_SKY_ROTATION
		#define MARMO_DIFFUSE_IBL
		#define MARMO_SPECULAR_IBL
		#define MARMO_DIFFUSE_DIRECT
		#define MARMO_SPECULAR_DIRECT		
		#define MARMO_NORMALMAP
		#define MARMO_MIP_GLOSS
		
		#define MARMO_SUBDERMIS_MAP
		#define MARMO_TRANSLUCENCY_MAP
		#define MARMO_SPECULAR_ANISO
		
		#define MARMO_SKIN_IBL
		#define MARMO_SKIN_DIRECT
		#define MARMO_SPECULAR_FILTER
		//#define MARMO_DIFFUSE_SPECULAR_COMBINED
		#define MARMO_SPHERICAL_HARMONICS 1
		
		#define MARMO_ALPHA
		//#define MARMO_GLOW
		//#define MARMO_PREMULT_ALPHA
		//#define MARMO_OCCLUSION
		//#define MARMO_VERTEX_OCCLUSION
		//#define MARMO_VERTEX_COLOR		
						
		#include "MarmosetSkinInput.cginc"
		#include "../../MarmosetSH.cginc"
		#include "../../MarmosetCore.cginc"
		#include "MarmosetSkin.cginc"

		ENDCG
	}
	
	FallBack "Transparent/Cutout/Bumped Specular"
}
