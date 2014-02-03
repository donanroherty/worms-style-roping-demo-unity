// Marmoset Skyshop
// Copyright 2013 Marmoset LLC
// http://marmoset.co

Shader "Hidden/Marmoset/RGBM Grass" {
	//This shader is a selectable version of the replacement shader. It mimics Unity's diffuse-only terrain shader, adding an IBL component.
	Properties {
		_WavingTint ("Fade Color", Color) = (.7,.6,.5, 0)
		_MainTex ("Base (RGB) Alpha (A)", 2D) = "white" {}
		_WaveAndDistance ("Wave and distance", Vector) = (12, 3.6, 1, 1)
		_Cutoff ("Cutoff", float) = 0.5
		_DiffCubeIBL ("Custom Diffuse Cube", Cube) = "black" {}
	}

	CGINCLUDE
		#include "UnityCG.cginc"
		#include "TerrainEngine.cginc"
		
		struct v2f {
			float4 pos : POSITION;
			fixed4 color : COLOR;
			float4 uv : TEXCOORD0;
		};
		v2f BillboardVert (appdata_full v) {
			v2f o;
			WavingGrassBillboardVert (v);
			o.color = v.color;
			o.color.rgb *= ShadeVertexLights (v.vertex, v.normal);
			o.pos = mul (UNITY_MATRIX_MVP, v.vertex);	
			o.uv = v.texcoord;
			return o;
		}
	ENDCG
	
	SubShader {
		Tags {
			"Queue" = "Geometry+200"
			"IgnoreProjector"="True"
			"RenderType"="Grass"
		}
		Cull Off
		LOD 200
		//ColorMask RGB
			
		CGPROGRAM
		#pragma surface MarmosetGrassSurf Lambert vertex:WavingGrassVert addshadow finalcolor:replacement
		#pragma exclude_renderers flash
		#pragma target 3.0
		#pragma multi_compile MARMO_LINEAR MARMO_GAMMA
			
		#define MARMO_ALPHA_CLIP
		#define MARMO_SKY_ROTATION
		#define MARMO_DIFFUSE_IBL
		
		#include "TerrainEngine.cginc"
		#include "../MarmosetCore.cginc"
		#include "../Terrain/MarmosetGrass.cginc"
		
		float4 replacement(Input IN, SurfaceOutput OUT, inout half4 result) {
			result = HDRtoRGBM(result);
			return result;
		}
		ENDCG
	}
	
	SubShader {
		Tags {
			"Queue" = "Geometry+200"
			"IgnoreProjector"="True"
			"RenderType"="GrassBillboard"
		}
		Cull Off
		LOD 200
		//ColorMask RGB
				
		CGPROGRAM
			#pragma surface MarmosetGrassSurf Lambert vertex:WavingGrassBillboardVert addshadow finalcolor:replacement
			#pragma exclude_renderers flash
			#pragma glsl_no_auto_normalization
			#pragma target 3.0
			#pragma multi_compile MARMO_LINEAR MARMO_GAMMA
			
			#define MARMO_ALPHA_CLIP
			#define MARMO_SKY_ROTATION
			#define MARMO_DIFFUSE_IBL
			#include "../MarmosetCore.cginc"
			#include "../Terrain/MarmosetGrass.cginc"
			
			float4 replacement(Input IN, SurfaceOutput OUT, inout half4 result) {
				result = HDRtoRGBM(result);
				return result;
			}
		ENDCG
	}

	// Fallback to Diffuse
	FallBack "Marmoset/Diffuse IBL"
}
