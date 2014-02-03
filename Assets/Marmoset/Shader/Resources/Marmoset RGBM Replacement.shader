// Marmoset Skyshop
// Copyright 2013 Marmoset LLC
// http://marmoset.co

Shader "Hidden/Marmoset/RGBM Replacement" {
	Properties {
		_Color   ("Diffuse Color", Color) = (1,1,1,1)
		_SpecColor ("Specular Color", Color) = (1,1,1,1)
		_SpecInt ("Specular Intensity", Float) = 0.0
		_Shininess ("Specular Sharpness", Range(2.0,8.0)) = 4.0
		_Fresnel ("Fresnel Strength", Range(0.0,1.0)) = 0.0
		_Cutoff ("Alpha Cutoff", Range (0,1)) = 1.0
		_OccStrength("Occlusion Strength", Range(0.0,1.0)) = 1.0
		_MainTex ("Diffuse(RGB) Alpha(A)", 2D) = "white" {}
		_SpecTex ("Specular(RGB) Gloss(A)", 2D) = "white" {}
		_BumpMap ("Normalmap", 2D) 	= "bump" {}
		_GlowColor ("Glow Color", Color) = (0,0,0,0)
		_GlowStrength("Glow Strength", Float) = 0.0
		_EmissionLM ("Diffuse Emission Strength", Float) = 0.0
		_Illum ("Glow(RGB) Diffuse Emission(A)", 2D) = "white" {}
		_OccTex	 ("Occlusion Diff(R) Spec(G)", 2D) = "white" {}
		
		//slots for custom lighting cubemaps
		_DiffCubeIBL ("Custom Diffuse Cube", Cube) = "black" {}
		_SpecCubeIBL ("Custom Specular Cube", Cube) = "black" {}
	}
	
	//handled rendertypes: Opaque, TransparentCutout, MarmoVertexColor, MarmoVertexOcc
	//OPAQUE
	SubShader {
		Tags {
			"Queue"="Geometry"
			"RenderType"="Opaque"
		}
		LOD 400
		
		//mac stuff
		CGPROGRAM
		#ifdef SHADER_API_OPENGL
			#pragma glsl
		#endif
				
		#pragma target 3.0
		#pragma exclude_renderers d3d11_9x
		#pragma surface MarmosetSurf MarmosetDirect fullforwardshadows vertex:MarmosetVert finalcolor:replacement
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
		#define MARMO_GLOW
		#define MARMO_OCCLUSION
		//#define MARMO_ALPHA
		//#define MARMO_PREMULT_ALPHA
		//#define MARMO_VERTEX_OCCLUSION
		//#define MARMO_VERTEX_COLOR
		//#define MARMO_SPECULAR_FILTER
						
		#include "../MarmosetInput.cginc"
		#include "../MarmosetCore.cginc"
		#include "../MarmosetDirect.cginc"
		#include "../MarmosetSurf.cginc"
		
		float _Cutoff;
		float4 replacement(Input IN, MarmosetOutput OUT, inout half4 result) {
			result = HDRtoRGBM(result);
			return result;
		}
		ENDCG
	}
	//TRANS-CUTOUT
	SubShader {
		Tags {
			"Queue"="Geometry"
			"RenderType"="TransparentCutout"
		}
		LOD 400
		
		//mac stuff
		CGPROGRAM
		#ifdef SHADER_API_OPENGL
			#pragma glsl
		#endif
				
		#pragma target 3.0
		#pragma exclude_renderers d3d11_9x
		#pragma surface MarmosetSurf MarmosetDirect fullforwardshadows vertex:MarmosetVert finalcolor:replacement
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
		#define MARMO_GLOW
		#define MARMO_OCCLUSION
		#define MARMO_ALPHA
		//#define MARMO_PREMULT_ALPHA
		//#define MARMO_VERTEX_OCCLUSION
		//#define MARMO_VERTEX_COLOR
		//#define MARMO_SPECULAR_FILTER
						
		#include "../MarmosetInput.cginc"
		#include "../MarmosetCore.cginc"
		#include "../MarmosetDirect.cginc"
		#include "../MarmosetSurf.cginc"
		
		float _Cutoff;
		float4 replacement(Input IN, MarmosetOutput OUT, inout half4 result) {
			if(result.a < _Cutoff) discard;
			result = HDRtoRGBM(result);
			return result;
		}
		ENDCG
	}
	//VERTEX COLOR
	SubShader {
		Tags {
			"Queue"="Geometry"
			"RenderType"="MarmoVertexColor"
		}
		LOD 400
		
		//mac stuff
		CGPROGRAM
		#ifdef SHADER_API_OPENGL
			#pragma glsl
		#endif
				
		#pragma target 3.0
		#pragma exclude_renderers d3d11_9x
		#pragma surface MarmosetSurf MarmosetDirect fullforwardshadows vertex:MarmosetVert finalcolor:replacement
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
		#define MARMO_GLOW
		//#define MARMO_OCCLUSION
		//#define MARMO_ALPHA
		//#define MARMO_PREMULT_ALPHA
		//#define MARMO_VERTEX_OCCLUSION
		#define MARMO_VERTEX_COLOR
		//#define MARMO_SPECULAR_FILTER
						
		#include "../MarmosetInput.cginc"
		#include "../MarmosetCore.cginc"
		#include "../MarmosetDirect.cginc"
		#include "../MarmosetSurf.cginc"
		
		float _Cutoff;
		float4 replacement(Input IN, MarmosetOutput OUT, inout half4 result) {
			result = HDRtoRGBM(result);
			return result;
		}
		ENDCG
	}
	//VERTEX OCC
	SubShader {
		Tags {
			"Queue"="Geometry"
			"RenderType"="MarmoVertexOcc"
		}
		LOD 400
		
		//mac stuff
		CGPROGRAM
		#ifdef SHADER_API_OPENGL
			#pragma glsl
		#endif
				
		#pragma target 3.0
		#pragma exclude_renderers d3d11_9x
		#pragma surface MarmosetSurf MarmosetDirect fullforwardshadows vertex:MarmosetVert finalcolor:replacement
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
		#define MARMO_GLOW
		//#define MARMO_OCCLUSION
		//#define MARMO_ALPHA
		//#define MARMO_PREMULT_ALPHA
		#define MARMO_VERTEX_OCCLUSION
		//#define MARMO_VERTEX_COLOR
		//#define MARMO_SPECULAR_FILTER
						
		#include "../MarmosetInput.cginc"
		#include "../MarmosetCore.cginc"
		#include "../MarmosetDirect.cginc"
		#include "../MarmosetSurf.cginc"
		
		float _Cutoff;
		float4 replacement(Input IN, MarmosetOutput OUT, inout half4 result) {
			result = HDRtoRGBM(result);
			return result;
		}
		ENDCG
	}
	
	//HACK: Tree leaves are rendered using the vertex occlusion shader for now
	//TREE LEAVES 
	SubShader {
		Tags {
			"Queue"="Geometry"
			"RenderType"="TreeLeaf"
		}
		LOD 400
		
		//mac stuff
		CGPROGRAM
		#ifdef SHADER_API_OPENGL
			#pragma glsl
		#endif
				
		#pragma target 3.0
		#pragma exclude_renderers d3d11_9x
		#pragma surface MarmosetSurf MarmosetDirect fullforwardshadows vertex:MarmosetVert finalcolor:replacement
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
		#define MARMO_GLOW
		//#define MARMO_OCCLUSION
		#define MARMO_ALPHA
		//#define MARMO_PREMULT_ALPHA
		#define MARMO_VERTEX_OCCLUSION
		//#define MARMO_VERTEX_COLOR
		//#define MARMO_SPECULAR_FILTER
						
		#include "../MarmosetInput.cginc"
		#include "../MarmosetCore.cginc"
		#include "../MarmosetDirect.cginc"
		#include "../MarmosetSurf.cginc"
		
		float _Cutoff;
		float4 replacement(Input IN, MarmosetOutput OUT, inout half4 result) {
			if( result.a < _Cutoff ) discard;
			result = HDRtoRGBM(result);
			return result;
		}
		ENDCG
	}
	//fallback to ensure shadows get rendered
	FallBack "Hidden/Marmoset/RGBM Terrain"
}
