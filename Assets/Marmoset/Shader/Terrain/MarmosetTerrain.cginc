#ifndef MARMOSET_TERRAIN_CGINC
#define MARMOSET_TERRAIN_CGINC

sampler2D _BaseTex;
sampler2D _Control;
sampler2D _Splat0,_Splat1,_Splat2,_Splat3;

#ifdef MARMO_NORMALMAP
sampler2D _BumpMap;
sampler2D _Normal0,_Normal1,_Normal2,_Normal3;
#endif

float4		_Tiling0;
float4		_Tiling1;
float4		_Tiling2;
float4		_Tiling3;

fixed4		_Color;
half		_BaseWeight;
half		_DetailWeight;
half4 		ExposureIBL;
half2		UniformOcclusion;

#ifdef MARMO_DIFFUSE_IBL
samplerCUBE _DiffCubeIBL;
#endif
#ifdef MARMO_SPECULAR_IBL
samplerCUBE _SpecCubeIBL;
#endif
#ifdef MARMO_SKY_ROTATION
float4x4	SkyMatrix;
#endif
half	 	_FadeNear;
half		_FadeRange; //TODO: invFadeRange

half		_SpecInt;
half		_SpecInt0;
half		_SpecInt1;
half		_SpecInt2;
half		_SpecInt3;

half		_SpecFresnel;
half		_Shininess;	//master gloss
half		_Gloss0;
half		_Gloss1;
half		_Gloss2;
half		_Gloss3;

half		_DiffFresnel; //master fresnel
half		_Fresnel0;
half		_Fresnel1;
half		_Fresnel2;
half		_Fresnel3;

half		_Fresnel4;
half		_Fresnel5;
half		_Fresnel6;
half		_Fresnel7;

struct Input {
	float3 texcoord : TEXCOORD0;
	float3 worldNormal;
	#if defined(MARMO_DIFFUSE_FRESNEL) || defined(MARMO_SPECULAR_IBL)
		float3 viewDir;
	#endif
	INTERNAL_DATA
};

void MarmosetTerrainVert (inout appdata_full v, out Input o) {
	UNITY_INITIALIZE_OUTPUT(Input,o);
	o.texcoord.xy = v.texcoord;	
#ifdef MARMO_NORMALMAP
	v.tangent.xyz = cross(v.normal, float3(0.0,0.0,1.0));
	v.tangent.w = -1.0;
#endif
#if defined(MARMO_DIFFUSE_FRESNEL) || defined(MARMO_SPECULAR_IBL)
	float3 vpos = mul(UNITY_MATRIX_MV, v.vertex).xyz;
	o.texcoord.z = length(vpos);
#endif
}

void MarmosetTerrainSurf (Input IN, inout SurfaceOutput OUT) {
	ExposureIBL.xy *= UniformOcclusion.xy;
	
	float2 uv_Control = IN.texcoord;	
	float2 uv_Splat0 = _Tiling0.xy*uv_Control + _Tiling0.zw;
	float2 uv_Splat1 = _Tiling1.xy*uv_Control + _Tiling1.zw;
	float2 uv_Splat2 = _Tiling2.xy*uv_Control + _Tiling2.zw;
	float2 uv_Splat3 = _Tiling3.xy*uv_Control + _Tiling3.zw;
	
	half4 splat_control = tex2D (_Control, uv_Control);
	fixed splatSum = dot(splat_control, fixed4(1,1,1,1));
	
	fixed4 diffBase;
	diffBase = tex2D (_BaseTex, uv_Control);
	
	fixed4 diff;	
	diff  = splat_control.r * tex2D (_Splat0, uv_Splat0);
	diff += splat_control.g * tex2D (_Splat1, uv_Splat1);
	diff += splat_control.b * tex2D (_Splat2, uv_Splat2);
	diff += splat_control.a * tex2D (_Splat3, uv_Splat3);
	
	diff.rgb *= diffBase.rgb;
	diff.rgb *= _Color.rgb;
	diff.rgb *= ExposureIBL.w; //camera exposure is built into OUT.Albedo	
	OUT.Albedo = diff.rgb;
	OUT.Alpha = 0.0;
	
	//NORMAL
	float3 N;
	#ifdef MARMO_NORMALMAP
		fixed4 nrm;
		fixed4 nrmBase;
		nrmBase = tex2D (_BumpMap, uv_Control);
		nrm  = splat_control.r * tex2D (_Normal0, uv_Splat0);
		nrm += splat_control.g * tex2D (_Normal1, uv_Splat1);
		nrm += splat_control.b * tex2D (_Normal2, uv_Splat2);
		nrm += splat_control.a * tex2D (_Normal3, uv_Splat3);
		// Sum of our four splat weights might not sum up to 1, in
		// case of more than 4 total splat maps. Need to lerp towards
		// "flat normal" in that case.
		fixed4 flatNormal = fixed4(0.5,0.5,1,0.5); // this is "flat normal" in both DXT5nm and xyz*2-1 cases
	
		#ifdef MARMO_HQ
			//weight detail normalmap to flat right here
			nrm = lerp(flatNormal, nrm, splatSum*_DetailWeight);		
		#else
			nrm = lerp(flatNormal, nrm, splatSum);
		#endif	
		
		N = UnpackNormal(nrm);
		float3 baseN = UnpackNormal(nrmBase);
			
		#ifdef MARMO_HQ
			//put detail normalmap into base normalmap's tangent-space
			float3 T = normalize(cross(float3(0.0,1.0,0.0), baseN));
			float3 B = normalize(cross(baseN,T));
			N = T * N.x + B * N.y + baseN * N.z;
		#else
			//quick n' dirty blend of details onto base normalmap
			N = normalize(baseN + _DetailWeight*N);
		#endif
			
		OUT.Normal = N; //OUT.Normal is in tangent-space
	#else
		//N = OUT.Normal; //OUT.Normal is in world-space
	#endif

	N = WorldNormalVector(IN, OUT.Normal);
	#ifdef MARMO_SKY_ROTATION
		N = mulVec3(SkyMatrix,N); //per-fragment matrix multiply, expensive
	#endif	
	
	//SPECULAR & FRESNEL FADE
	#if defined(MARMO_DIFFUSE_FRESNEL) || defined(MARMO_SPECULAR_IBL)
		half fade = IN.texcoord.z;
		fade = (fade - _FadeNear) / _FadeRange;
		fade = 1.0-saturate(fade); //TODO: can we get rid of this 1-?
		float3 E = normalize(IN.viewDir.xyz);
	#endif
	
	//SPECULAR
	#ifdef MARMO_SPECULAR_IBL
		float3 R = reflect(E,N);
		#ifdef MARMO_SKY_ROTATION
			R = mulVec3(SkyMatrix,R); //per-fragment matrix multiply, expensive
		#endif
	
		half specBlur = dot(splat_control, half4(_Gloss0,_Gloss1,_Gloss2,_Gloss3));
		half specMask = dot(splat_control, half4(_SpecInt0,_SpecInt1,_SpecInt2,_SpecInt3));
		specMask *= diff.a * _SpecInt;// * splatSum;
		
		half glossLod = glossLOD(specBlur, _Shininess);
		OUT.Specular = glossExponent(glossLod);
		
		#ifdef MARMO_HQ
			half fresnel = splineFresnel(OUT.Normal, E, _SpecInt, _SpecFresnel);
		#else
			//omitting normalize makes things darker, generally
			half fresnel = fastFresnel(OUT.Normal, E, _SpecInt, _SpecFresnel);		
		#endif
		
		//camera exposure is built into OUT.Specular
		specMask *= fade * fresnel * ExposureIBL.w;		
		OUT.Gloss = specMask * specEnergyScalar(OUT.Specular); //divide specular integral out of direct lighting
		OUT.Specular *= 0.00390625; //divide specular exponent by 256
	
		#ifdef MARMO_MIP_GLOSS
			half3 specIBL = glossCubeLookup(_SpecCubeIBL, R, glossLod);
		#else
			half3 specIBL = specCubeLookup(_SpecCubeIBL, R);
		#endif
		OUT.Emission = _SpecColor.rgb * specIBL * ExposureIBL.y * specMask;
	#else
		OUT.Emission = half3(0.0,0.0,0.0);
	#endif
	
	//DIFFUSE
	#ifdef MARMO_DIFFUSE_IBL
		half3 diffIBL = diffCubeLookup(_DiffCubeIBL, N);
		#ifdef MARMO_DIFFUSE_FRESNEL
			#ifdef MARMO_FIRST_PASS
				half dfresnelMask = dot(splat_control, half4(_Fresnel0,_Fresnel1,_Fresnel2,_Fresnel3));
			#else
				half dfresnelMask = dot(splat_control, half4(_Fresnel4,_Fresnel5,_Fresnel6,_Fresnel7));
			#endif
			half dfresnel = saturate(dot(OUT.Normal, E));
			dfresnel = 1.0 - dfresnel;
			dfresnel = lerp(dfresnel*dfresnel*dfresnelMask, dfresnel, dfresnelMask);
			//HACK: modify albedo and direct lighting gets fresnel also
			OUT.Albedo.rgb *= 1.0 + dfresnel.xxx * fade * _DiffFresnel;
		#endif
		OUT.Emission += OUT.Albedo.rgb * diffIBL * ExposureIBL.x;
	#endif
	OUT.Emission *= diffBase.a; //AO
}

#endif