// Marmoset Skyshop
// Copyright 2013 Marmoset LLC
// http://marmoset.co

#ifndef MARMOSET_VERTEX_CGINC
#define MARMOSET_VERTEX_CGINC

float4 		_MainTex_ST;
sampler2D	_MainTex;

#if defined(MARMO_OCCLUSION) || defined(MARMO_VERTEX_OCCLUSION)
float		_OccStrength;
#endif

#ifdef MARMO_OCCLUSION
sampler2D	_OccTex;
#endif

float4		_Color;
float		_SpecInt;
float		_Shininess;			
float		_Fresnel;

half4 		ExposureIBL;
half2		ExposureLM; //multiplier on exposures when lightmapping
half2		UniformOcclusion;

float4x4	SkyMatrix;
float4x4	InvSkyMatrix;
float3		_SkySize;

samplerCUBE _DiffCubeIBL;
samplerCUBE _SpecCubeIBL;
			
struct appdata_t {
	float4 vertex : POSITION;
	float3 normal : NORMAL;
	float3 texcoord : TEXCOORD0;
	float3 texcoord1 : TEXCOORD1;
	float4 color : COLOR;
};

struct v2f {
	float4 vertex : POSITION;
	half4 texcoord : TEXCOORD0;
	#if MARMO_SPHERICAL_HARMONICS
	#else
		half3 skyNormal : TEXCOORD1;
	#endif
	half4 lighting : TEXCOORD3;
	#ifdef MARMO_SPECULAR_IBL
		half3 skyRefl : TEXCOORD4;
	#endif
	#if defined(MARMO_VERTEX_COLOR) || defined(MARMO_VERTEX_OCCLUSION)
		half4 color : COLOR;
	#endif
};

inline float3 toSkySpace(float3 vec) {
	//TODO: box projection
	#ifdef MARMO_SKY_ROTATION
		return mulVec3(SkyMatrix, vec);
	#else
		return vec;
	#endif
}

inline float3 softLambert(float4 lightP, float3 P, float3 N) {
	float3 L = lightP.xyz - P*lightP.w;
	float lengthSq = dot(L, L);
	float atten = 1.0 / (1.0 + lengthSq * unity_LightAtten[0].z);
	L = normalize(L);
	float diff = dot(N, L)*0.5 + 0.5;
	diff *= diff * diff;
	diff *= atten;
	return diff.xxx;
}

v2f MarmosetVert(appdata_t v) {
	v2f o;				
	o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
	o.texcoord.xy = TRANSFORM_TEX(v.texcoord,_MainTex);
	#ifdef MARMO_OCCLUSION
		o.texcoord.zw = v.texcoord1;
	#else
		o.texcoord.zw = half2(0.0,0.0);
	#endif
	
	float3 worldP = mul(_Object2World, v.vertex).xyz;
	float3 worldN = mul((float3x3)_Object2World, v.normal * unity_Scale.w);
	float3 skyN = toSkySpace(worldN);
		
	o.lighting = float4(0.0,0.0,0.0,1.0);	
	o.lighting.rgb += UNITY_LIGHTMODEL_AMBIENT.xyz;
	#ifdef MARMO_SPECULAR_IBL
		float3 worldE = WorldSpaceViewDir( v.vertex );
		float3 worldR = reflect( -worldE, worldN );
		o.skyRefl = toSkySpace(worldR);
		o.lighting.a = fastFresnel(normalize(worldN), normalize(worldE), _SpecInt, _Fresnel);
	#endif
	
	#ifdef MARMO_VERTEX_DIRECT		
		#ifdef MARMO_FORWARDBASE
			o.lighting.rgb += softLambert(_WorldSpaceLightPos0, worldP, worldN) * _LightColor0.rgb;
		#else
			//Yep. It's different.
			o.lighting.rgb += softLambert(_WorldSpaceLightPos0, worldP, worldN) * unity_LightColor[0].rgb;
		#endif
	#endif
	o.lighting.rgb *= 2.0; //2x to match Unity
	#ifdef MARMO_VERTEX_SH
		half4 shN;
		shN.xyz = normalize(worldN);
		shN.w = 1.0;
		o.lighting.rgb += ShadeSH9(shN);
	#endif
	
	#if MARMO_SPHERICAL_HARMONICS
		//spherical harmonics
		skyN = normalize(skyN);
		float3 band0, band1, band2;
		SHLookup(skyN,band0,band1,band2);
		o.lighting.rgb += lightingSH(band0, band1, band2) * ExposureIBL.x;
	#else
		o.skyNormal = skyN;
	#endif
	
	#ifdef MARMO_VERTEX_COLOR
		o.color = v.color;
	#endif
	
	#ifdef MARMO_VERTEX_OCCLUSION
		o.color = lerp(half4(1.0,1.0,1.0,1.0), v.color, _OccStrength);
		#ifdef SHADER_API_D3D11
			//HACK: dx11 seems to swap the red and blue components, combine them to hack-fix AO anyway
			o.color.r *= o.color.b;
		#endif
	#endif
	return o;
}

half4 MarmosetFrag(v2f IN) : COLOR {
	half4 albedo = _Color;
	albedo *= tex2D(_MainTex, IN.texcoord.xy);	
	#ifdef MARMO_VERTEX_COLOR
		albedo.rgb *= IN.color.rgb;
	#endif
	
	ExposureIBL.xy *= UniformOcclusion;
	#if defined(MARMO_OCCLUSION) || defined(MARMO_VERTEX_OCCLUSION)
		half4 occ = half4(1.0,1.0,1.0,1.0);	
		#ifdef MARMO_OCCLUSION
			occ = tex2D(_OccTex, IN.texcoord.zw);
			occ = lerp(half4(1.0,1.0,1.0,1.0), occ, _OccStrength);
		#endif
		#ifdef MARMO_VERTEX_OCCLUSION
			occ *= IN.color;
		#endif		
		ExposureIBL.xy *= occ.rg;
		IN.lighting.rgb *= 0.5*occ.r + 0.5;
	#endif
	
	half3 ibl = half3(0.0,0.0,0.0);	
	//no cubemaps if lightmapping is off
	#ifdef MARMO_DIFFUSE_IBL
		#if MARMO_SPHERICAL_HARMONICS
		//this gets done per vertex now
		//	float3 skyN = normalize(IN.skyNormal);
		//	float3 band0, band1, band2;
		//	SHLookup(skyN,band0,band1,band2);
		//	half3 diff = lightingSH(band0, band1, band2);
		#else
			half3 diff = diffCubeLookup(_DiffCubeIBL, IN.skyNormal);			
			ibl += albedo.rgb * diff * ExposureIBL.x;
		#endif
	#endif
	
	#ifdef MARMO_SPECULAR_IBL
		half3 spec = specCubeLookup(_SpecCubeIBL, IN.skyRefl);
		albedo.a *= 0.125*_Shininess;
		albedo.a *= albedo.a;
		ibl += (_SpecColor.rgb * spec) * (albedo.a * ExposureIBL.y * IN.lighting.a);
	#endif
		
	half4 col;
	col.rgb = ibl + albedo.rgb * IN.lighting;
	col.rgb *= ExposureIBL.w;
	col.a = albedo.a;
	
	return col;
}

#endif