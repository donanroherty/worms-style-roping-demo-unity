Shader "Hidden/Marmoset/Nature/Tree Creator Bark Optimized" {
Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_MainTex ("Base (RGB) Alpha (A)", 2D) = "white" {}
	_BumpSpecMap ("Normalmap (GA) Spec (R)", 2D) = "bump" {}
	_TranslucencyMap ("Trans (RGB) Gloss(A)", 2D) = "white" {}
	
	// These are here only to provide default values
	_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
	_Scale ("Scale", Vector) = (1,1,1,1)
	_SquashAmount ("Squash", Float) = 1
}

SubShader { 
	Tags { "RenderType"="TreeBark" }
	LOD 200
	
CGPROGRAM
#pragma surface surf Lambert vertex:TreeVertBark addshadow nolightmap
#pragma exclude_renderers flash
#pragma glsl_no_auto_normalization
#pragma multi_compile MARMO_GAMMA MARMO_LINEAR
#pragma target 3.0

#include "Tree.cginc"
#include "../../MarmosetCore.cginc"


#define MARMO_SKY_ROTATION
#define MARMO_NORMALMAP
//#define MARMO_SPECULAR_DIRECT
// no specular, it looks more or less terrible.

half4 		ExposureIBL;
samplerCUBE _DiffCubeIBL;
samplerCUBE _SpecCubeIBL;
#ifdef MARMO_SKY_ROTATION
float4x4	SkyMatrix;
#endif

sampler2D _MainTex;
sampler2D _BumpSpecMap;
sampler2D _TranslucencyMap;
float _ShadowOffset; //ignored. needed by editor

struct Input {
	float2 uv_MainTex;
	#ifdef MARMO_NORMALMAP
		float3 worldNormal;
	#endif
	fixed4 color : COLOR;
	INTERNAL_DATA
};

void surf (Input IN, inout SurfaceOutput o) {
	fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
	o.Albedo = c.rgb * _Color.rgb;
	
	#if defined(MARMO_NORMALMAP) || defined(MARMO_SPECULAR_DIRECT)
		half4 norspc = tex2D (_BumpSpecMap, IN.uv_MainTex);
		#ifdef MARMO_NORMALMAP
			o.Normal = UnpackNormalDXT5nm(norspc);
		#endif
	#endif
	
	#ifdef MARMO_SPECULAR_DIRECT
		o.Specular = norspc.r;
		fixed4 trngls = tex2D (_TranslucencyMap, IN.uv_MainTex);
		o.Gloss = trngls.a * _Color.r;
		o.Gloss *= ExposureIBL.w;
		o.Gloss *=  specEnergyScalar(o.Specular*128);
	#endif
	o.Alpha = c.a;
	
	#ifdef MARMO_NORMALMAP
		float3 N = WorldNormalVector(IN, o.Normal);
	#else
		float3 N = o.Normal;
	#endif
	#ifdef MARMO_SKY_ROTATION
		N = mulVec3(SkyMatrix, N);
	#endif
	
	o.Albedo *= ExposureIBL.w;
	o.Emission = diffCubeLookup(_DiffCubeIBL, N) * o.Albedo * ExposureIBL.x * IN.color.a;
}
ENDCG
}

SubShader {
	Tags { "RenderType"="TreeBark" }
	Pass {		
		Material {
			Diffuse (1,1,1,1)
			Ambient (1,1,1,1)
		} 
		Lighting On
		SetTexture [_MainTex] {
			Combine texture * primary DOUBLE, texture * primary
		}
		SetTexture [_MainTex] {
			ConstantColor [_Color]
			Combine previous * constant, previous
		} 
	}
}

Dependency "BillboardShader" = "Hidden/Marmoset/Nature/Tree Creator Bark Rendertex"
}
