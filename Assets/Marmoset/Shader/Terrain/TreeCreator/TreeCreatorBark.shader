Shader "Marmoset/Nature/Tree Creator Bark" {
Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_Shininess ("Shininess", Range (0.01, 1)) = 0.078125
	_MainTex ("Base (RGB) Alpha (A)", 2D) = "white" {}
	_BumpMap ("Normalmap", 2D) = "bump" {}
	_GlossMap ("Gloss (A)", 2D) = "black" {}
	
	// These are here only to provide default values
	_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
	_SpecInt ("Specular Intensity", Float) = 1.0
	_Fresnel ("Fresnel Falloff", Range(0.0,1.0)) = 0.0
	_Scale ("Scale", Vector) = (1,1,1,1)
	_SquashAmount ("Squash", Float) = 1
}

SubShader { 
	Tags { "RenderType"="TreeBark" }
	LOD 200
		
	CGPROGRAM
	#pragma surface surf BlinnPhong vertex:TreeVertBark addshadow nolightmap
	#pragma exclude_renderers flash 
	#pragma glsl
	#pragma glsl_no_auto_normalization

	#pragma multi_compile MARMO_GAMMA MARMO_LINEAR
	#pragma target 3.0

	#define MARMO_SKY_ROTATION
	#define MARMO_NORMALMAP
	//#define MARMO_SPECULAR_DIRECT

	#include "../../MarmosetCore.cginc"
	#include "Tree.cginc"
	#include "TreeBark.cginc"

	ENDCG
}

Dependency "OptimizedShader" = "Hidden/Marmoset/Nature/Tree Creator Bark Optimized"
FallBack "Diffuse"
}
