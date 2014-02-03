// Marmoset Skyshop
// Copyright 2013 Marmoset LLC
// http://marmoset.co
Shader "Hidden/Marmoset/Terrain/Simple Distant IBL" {
	Properties {
		_Color   ("Diffuse Color", Color) = (1,1,1,1)
		_MainTex ("Diffuse(RGB) Alpha(A)", 2D) = "white" {}
		//slots for custom lighting cubemaps
		_DiffCubeIBL ("Custom Diffuse Cube", Cube) = "black" {}
	}
	
	SubShader {
		Tags {
			"Queue"="Geometry"
			"RenderType"="Opaque"
		}
		LOD 200
		//diffuse LOD 200
		//diffuse-spec LOD 250
		//bumped-diffuse, spec 350
		//bumped-spec 400
		
		//mac stuff
		CGPROGRAM
		#pragma glsl
		#pragma target 3.0
		#pragma surface TerrainBaseSurf Lambert
		//gamma-correct sampling permutations
		#pragma multi_compile MARMO_LINEAR MARMO_GAMMA
		
		#define MARMO_HQ
		#define MARMO_SKY_ROTATION
		#define MARMO_DIFFUSE_IBL
		
		#include "../MarmosetCore.cginc"
		
		sampler2D 	_MainTex;
		half4 		ExposureIBL;
		half2		UniformOcclusion;
	
		samplerCUBE _DiffCubeIBL;
		float4		_Color;
		
		#ifdef MARMO_SKY_ROTATION
		float4x4	SkyMatrix;
		#endif
		
		struct Input {
			float2 uv_MainTex;
			float3 worldNormal;
			INTERNAL_DATA
		};

		void TerrainBaseSurf(Input IN, inout SurfaceOutput OUT) {
			//DIFFUSE
			half4 diff = tex2D( _MainTex, IN.uv_MainTex );		//Unity's composite of all splats
			diff *= _Color;
			//camera exposure is built into OUT.Albedo
			diff.rgb *= ExposureIBL.w;
			OUT.Albedo = diff.rgb;
			OUT.Alpha = 0.0;
			
			//DIFFUSE IBL
			#ifdef MARMO_DIFFUSE_IBL
				ExposureIBL.xy *= UniformOcclusion.xy;
				float3 N = IN.worldNormal; //N is in world-space
				#ifdef MARMO_SKY_ROTATION
					N = mulVec3(SkyMatrix,N); //per-fragment matrix multiply, expensive
				#endif
				half3 diffIBL = diffCubeLookup(_DiffCubeIBL, N);
				OUT.Emission += diffIBL * diff.rgb * ExposureIBL.x;
			#else
				OUT.Emission = half3(0.0,0.0,0.0);
			#endif
		}

		ENDCG
	}
	
	FallBack "Marmoset/Diffuse IBL"
}
