Shader "Hidden/Marmoset/Terrain/Simple Terrain IBL AddPass" {
//This shader is a selectable version of the replacement shader. It mimics Unity's diffuse-only terrain shader, adding an IBL component.
Properties {
	_Control ("Splatmap (RGBA)", 2D) = "black" {}
	_Splat0 ("Layer 0 (R)", 2D) = "white" {}
	_Splat1 ("Layer 1 (G)", 2D) = "white" {}
	_Splat2 ("Layer 2 (B)", 2D) = "white" {}
	_Splat3 ("Layer 3 (A)", 2D) = "white" {}
}
	
SubShader {
	Tags {
		"SplatCount" = "4"
		"Queue" = "Geometry-99"
		"IgnoreProjector"="True"
		"RenderType" = "Opaque"
	}
	
CGPROGRAM
#pragma glsl
#pragma target 3.0
#pragma surface MarmosetSimpleSurf Lambert decal:add
#pragma multi_compile MARMO_LINEAR MARMO_GAMMA


#define MARMO_DIFFUSE_IBL
#define MARMO_SKY_ROTATION

#include "../MarmosetCore.cginc"
#include "MarmosetSimpleTerrain.cginc"

ENDCG  
}

Fallback off
}
