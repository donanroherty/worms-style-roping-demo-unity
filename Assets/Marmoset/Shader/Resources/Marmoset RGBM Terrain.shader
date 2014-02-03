// Marmoset Skyshop
// Copyright 2013 Marmoset LLC
// http://marmoset.co

Shader "Hidden/Marmoset/RGBM Terrain" {
	Properties {
		_Color   	("Diffuse Color", Color) = (1,1,1,1)
		_SpecColor 	("Specular Color", Color) = (1,1,1,1)
		
		_DetailWeight ("DetailWeight", Range(0.0,1.0)) = 1.0
		_FadeNear	("Fade Start", Float) = 500.0
		_FadeRange	("Fade Range", Float) = 100.0
		
		_Tiling0 	("Layer 0 Tiling (xy) Offset (zw)", Vector) = (100,100,0,0)
		_Tiling1 	("Layer 1 Tiling (xy) Offset (zw)", Vector) = (100,100,0,0)
		_Tiling2 	("Layer 2 Tiling (xy) Offset (zw)", Vector) = (100,100,0,0)
		_Tiling3 	("Layer 3 Tiling (xy) Offset (zw)", Vector) = (100,100,0,0)
		
		_SpecInt 	("Master Specular Intensity", Float) = 1.0
		_SpecInt0	("Intensity 0",Range(0.0,1.0)) = 1.0
		_SpecInt1	("Intensity 1",Range(0.0,1.0)) = 1.0
		_SpecInt2	("Intensity 2",Range(0.0,1.0)) = 1.0
		_SpecInt3	("Intensity 3",Range(0.0,1.0)) = 1.0
		
		_Shininess 	("Master Specular Sharpness", Range(2.0,8.0)) = 4.0
		_Gloss0		("Sharpness 0",Range(0.0,1.0)) = 1.0
		_Gloss1		("Sharpness 1",Range(0.0,1.0)) = 1.0
		_Gloss2		("Sharpness 2",Range(0.0,1.0)) = 1.0
		_Gloss3		("Sharpness 3",Range(0.0,1.0)) = 1.0
			
		_SpecFresnel("Specular Fresnel", Range(0.0,1.0)) = 0.0
			
		_DiffFresnel("Master Diffuse Fresnel", Float) = 0.0
		_Fresnel0	("Diffuse Fresnel 0",Range(0.0,1.0)) = 0.0
		_Fresnel1	("Diffuse Fresnel 1",Range(0.0,1.0)) = 0.0
		_Fresnel2	("Diffuse Fresnel 2",Range(0.0,1.0)) = 0.0
		_Fresnel3	("Diffuse Fresnel 3",Range(0.0,1.0)) = 0.0
		
		_Fresnel4	("Secondary Fresnel 4",Range(0.0,1.0)) = 0.0
		_Fresnel5	("Secondary Fresnel 5",Range(0.0,1.0)) = 0.0
		_Fresnel6	("Secondary Fresnel 6",Range(0.0,1.0)) = 0.0
		_Fresnel7	("Secondary Fresnel 7",Range(0.0,1.0)) = 0.0
			
		_BaseTex ("Base Diffuse (RGB) AO (A)", 2D) = "white" {}
		_BumpMap ("Base Normalmap (RGB)", 2D) = "bump" {}
		
		// set by terrain engine (and must be exposed this way)
		_Control ("Splatmap (RGBA)", 2D) = "red" {}
		_Splat0 ("Layer 0 (RGB) Spec (A)", 2D) = "white" {}
		_Splat1 ("Layer 1 (RGB) Spec (A)", 2D) = "white" {}
		_Splat2 ("Layer 2 (RGB) Spec (A)", 2D) = "white" {}
		_Splat3 ("Layer 3 (RGB) Spec (A)", 2D) = "white" {}
		_Normal0 ("Normal 0", 2D) = "bump" {}
		_Normal1 ("Normal 1", 2D) = "bump" {}
		_Normal2 ("Normal 2", 2D) = "bump" {}
		_Normal3 ("Normal 3", 2D) = "bump" {}
		
		
		//slots for custom lighting cubemaps
		_DiffCubeIBL ("Custom Diffuse Cube", Cube) = "black" {}
		_SpecCubeIBL ("Custom Specular Cube", Cube) = "black" {}
	}
	
	SubShader {
		Tags {
			"SplatCount" = "4"
			"Queue" = "Geometry-100"
			"RenderType" = "MarmoTerrainSpecular"
		}
		CGPROGRAM
		#ifdef SHADER_API_OPENGL	
			#pragma glsl
		#endif
		//NOTE: BlinnPhong instead of MarmosetDirect is fine here, MarmosetDirect is only needed for RGB specular
		#pragma surface MarmosetTerrainSurf BlinnPhong vertex:MarmosetTerrainVert fullforwardshadows finalcolor:replacement
		#pragma target 3.0
		#pragma exclude_renderers d3d11_9x
		//gamma-correct sampling permutations
		#pragma multi_compile MARMO_LINEAR MARMO_GAMMA
		
		#define MARMO_HQ
		#define MARMO_DIFFUSE_DIRECT
		#define MARMO_SPECULAR_DIRECT
		#define MARMO_DIFFUSE_IBL
		#define MARMO_SPECULAR_IBL
		#define MARMO_MIP_GLOSS
		#define MARMO_NORMALMAP
		#define MARMO_SKY_ROTATION
		#define MARMO_DIFFUSE_FRESNEL
		#define MARMO_FIRST_PASS

		#include "../MarmosetCore.cginc"
		#include "../Terrain/MarmosetTerrain.cginc"

		float4 replacement(Input IN, SurfaceOutput OUT, inout half4 result) {
			result = HDRtoRGBM(result);
			return result;
		}
		
		ENDCG  
	}
	
	SubShader {
		Tags {
			"SplatCount" = "4"
			"Queue" = "Geometry-100"
			"RenderType" = "MarmoTerrainDiffuse"
		}
		CGPROGRAM
		#ifdef SHADER_API_OPENGL	
			#pragma glsl
		#endif
		//NOTE: BlinnPhong instead of MarmosetDirect is fine here, MarmosetDirect is only needed for RGB specular
		#pragma surface MarmosetTerrainSurf BlinnPhong vertex:MarmosetTerrainVert fullforwardshadows addshadow finalcolor:replacement
		#pragma target 3.0
		#pragma exclude_renderers d3d11_9x
		//gamma-correct sampling permutations
		#pragma multi_compile MARMO_LINEAR MARMO_GAMMA
		
		#define MARMO_HQ
		#define MARMO_DIFFUSE_DIRECT
		//#define MARMO_SPECULAR_DIRECT
		#define MARMO_DIFFUSE_IBL
		//#define MARMO_SPECULAR_IBL
		//#define MARMO_MIP_GLOSS
		#define MARMO_NORMALMAP
		#define MARMO_SKY_ROTATION
		#define MARMO_DIFFUSE_FRESNEL
		#define MARMO_FIRST_PASS

		#include "../MarmosetCore.cginc"
		#include "../Terrain/MarmosetTerrain.cginc"
		
		float4 replacement(Input IN, SurfaceOutput OUT, inout half4 result) {
			result = HDRtoRGBM(result);
			return result;
		}

		ENDCG  
	}

	// Fallback to Unity terrain
	Fallback "Hidden/Marmoset/RGBM Simple Terrain"
}
