Shader "Marmoset/Vertex Lit/Diffuse IBL" {
	Properties {
		_Color   ("Diffuse Color", Color) = (1,1,1,1)
		_MainTex ("Diffuse(RGB) Spec. Mask(A)", 2D) = "white" {  }
				
		_DiffCubeIBL ("Custom Diffuse Cube", Cube) = "black" {}
		_SpecCubeIBL ("Custom Specular Cube", Cube) = "black" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		Pass {
			Tags { "LightMode"="ForwardBase" }
			CGPROGRAM
			//Tags { "LightMode"="Vertex" }	works with lightmaps but no SH vars are defined yet.
			//Tags { "LightMode"="ForwardBase" } SH but no lightmaps
			
			#pragma vertex MarmosetVert
			#pragma fragment MarmosetFrag
			#pragma fragmentoption ARB_precision_hint_fastest
			//gamma-correct sampling permutations
			#pragma multi_compile MARMO_LINEAR MARMO_GAMMA
					
			#define MARMO_HQ
			#define MARMO_DIFFUSE_IBL
			//#define MARMO_SPECULAR_IBL
			#define MARMO_SKY_ROTATION
			//#define MARMO_MIP_GLOSS
			#define MARMO_VERTEX_SH
			#define MARMO_VERTEX_DIRECT
			#define MARMO_FORWARDBASE
			#define MARMO_VERTEX_COLOR
			//#define MARMO_VERTEX_OCCLUSION
			
			#include "HLSLSupport.cginc"
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			
			#include "../MarmosetCore.cginc"
			#include "../MarmosetVertex.cginc"
			
			ENDCG 
		}
	}
	
	Fallback "VertexLit"
}
