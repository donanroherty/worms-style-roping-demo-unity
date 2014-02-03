#ifndef MARMOSET_SIMPLE_TERRAIN_CGINC
#define MARMOSET_SIMPLE_TERRAIN_CGINC

sampler2D _Control;
sampler2D _Splat0,_Splat1,_Splat2,_Splat3;
fixed4		_Color;

half4 		ExposureIBL;
half2		UniformOcclusion;
#ifdef MARMO_DIFFUSE_IBL
samplerCUBE _DiffCubeIBL;
#endif
#ifdef MARMO_SKY_ROTATION
float4x4	SkyMatrix;
#endif

struct Input {
	float2 uv_Control : TEXCOORD0;
	float2 uv_Splat0 : TEXCOORD1;
	float2 uv_Splat1 : TEXCOORD2;
	float2 uv_Splat2 : TEXCOORD3;
	float2 uv_Splat3 : TEXCOORD4;
	float3 worldNormal;
};

void MarmosetSimpleSurf (Input IN, inout SurfaceOutput OUT) {
	half4 splat_control = tex2D (_Control, IN.uv_Control);
	half3 diff;	
	diff  = splat_control.r * tex2D (_Splat0, IN.uv_Splat0).rgb;
	diff += splat_control.g * tex2D (_Splat1, IN.uv_Splat1).rgb;
	diff += splat_control.b * tex2D (_Splat2, IN.uv_Splat2).rgb;
	diff += splat_control.a * tex2D (_Splat3, IN.uv_Splat3).rgb;
	diff *= _Color.rgb;
	diff *= ExposureIBL.w; //camera exposure is built into OUT.Albedo	
	OUT.Albedo = diff;
	OUT.Alpha = 0.0;
	
	
	#ifdef MARMO_DIFFUSE_IBL
		ExposureIBL.xy *= UniformOcclusion.xy;
		float3 N = IN.worldNormal;
		#ifdef MARMO_SKY_ROTATION
			N = mulVec3(SkyMatrix,N); //per-fragment matrix multiply, expensive
		#endif
		half3 diffIBL = diffCubeLookup(_DiffCubeIBL, N);
		OUT.Emission = OUT.Albedo.rgb * diffIBL * ExposureIBL.x;
	#else
		OUT.Emission = half3(0.0,0.0,0.0);
	#endif
}

#endif