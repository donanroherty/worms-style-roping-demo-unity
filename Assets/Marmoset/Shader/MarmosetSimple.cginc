// Marmoset Skyshop
// Copyright 2013 Marmoset LLC
// http://marmoset.co

#ifndef MARMOSET_SIMPLE_CGINC
#define MARMOSET_SIMPLE_CGINC

#include "MarmosetInput.cginc"
#include "MarmosetCore.cginc"

void MarmosetDirect( Input IN, inout MarmosetOutput OUT ) {
	//DIFFUSE
	half4 tex = tex2D(_MainTex, uv_diff);
	tex.rgb *= _Color.rgb;
	OUT.Albedo = tex.rgb;
	OUT.Alpha = tex.a*_Color.a;
	OUT.Albedo *= OUT.Alpha; //premultiplied alpha


	//NORMALS
	float3 N = UnpackNormal(tex2D(_BumpMap,IN.uv_BumpMap));
	OUT.Normal = N;	
	//must happen while N is in not-worldspace
	float3 R = WorldReflectionVector(IN,N);
	float3 E = IN.viewDir;
	E = normalize(E);
	
	
	//SPECULAR
	half4 spec = tex2D(_SpecTex, uv_spec);
	half fresnel = splineFresnel(N, E, _SpecInt, _Fresnel);
	OUT.Specular = spec.rgb * _SpecColor.rgb * fresnel;
	OUT.Gloss = lerp(4.0, _Shininess, spec.a*spec.a);
	
	
	//DIFF IBL
	N = WorldNormalVector(IN, N);
	N = mulVec3(SkyMatrix,N);
	half3 DIFF = diffCubeLookup(N, _DiffCubeIBL, ExposureIBL.x);
	OUT.Emission = DIFF*OUT.Albedo;


	//SPEC IBL
	R = mulVec3(SkyMatrix,R);
	half3 SPEC = glossCubeLookup(R, _SpecCubeIBL, OUT.Gloss, ExposureIBL.y);
	OUT.Emission += SPEC*OUT.Specular;
	OUT.Specular *= specEnergyScalar(OUT.Gloss);
}

#endif