// Marmoset Skyshop
// Copyright 2013 Marmoset LLC
// http://marmoset.co

#ifndef MARMOSET_SURF_CGINC
#define MARMOSET_SURF_CGINC
void MarmosetVert (inout appdata_full v, out Input o) {
	UNITY_INITIALIZE_OUTPUT(Input,o);
	#ifdef MARMO_OCCLUSION
		o.texcoord1 = v.texcoord1.xy;
	#endif
	#ifdef MARMO_VERTEX_COLOR
		o.color = v.color;
	#endif
	#if MARMO_BOX_PROJECTION
		o.worldPos = mul(_Object2World, v.vertex);
	#endif
}

void MarmosetSurf(Input IN, inout MarmosetOutput OUT) {
	#define uv_diff IN.uv_MainTex
	#define uv_spec IN.uv_MainTex
	#define uv_bump IN.uv_MainTex
	#define uv_glow IN.uv_MainTex
	#define uv_occ  IN.texcoord1

	ExposureIBL.xy *= UniformOcclusion.xy;
	#if LIGHTMAP_ON
		ExposureIBL.xy *= ExposureLM;
	#endif
	half4 baseColor = _Color;
	#ifdef MARMO_VERTEX_COLOR
		baseColor *= IN.color;
	#endif

	#ifdef MARMO_DIFFUSE_SPECULAR_COMBINED
		half4 diffspec = half4(1.0,1.0,1.0,1.0);
	#endif
		
	//DIFFUSE
	#if defined(MARMO_DIFFUSE_DIRECT) || defined(MARMO_DIFFUSE_IBL)
		half4 diff = tex2D( _MainTex, uv_diff );
		#ifdef MARMO_DIFFUSE_SPECULAR_COMBINED
			diffspec = diff.aaaa;
		#endif
		diff *= baseColor;
		//camera exposure is built into OUT.Albedo
		diff.rgb *= ExposureIBL.w;
		OUT.Albedo = diff.rgb;
		OUT.Alpha = diff.a;
		#ifdef MARMO_PREMULT_ALPHA
			OUT.Albedo *= diff.a;
		#endif
	#else
		OUT.Albedo = baseColor.rgb;
		OUT.Alpha = 1.0;
	#endif
	
	//AMBIENT OCC
	#if defined(MARMO_VERTEX_OCCLUSION) || defined(MARMO_OCCLUSION)
		half4 occ = half4(1.0,1.0,1.0,1.0);
		#ifdef MARMO_OCCLUSION
			occ = tex2D(_OccTex, uv_occ);
		#endif
		#ifdef MARMO_VERTEX_OCCLUSION
			occ.rg *= IN.color.rg;
		#endif
		occ = lerp(half4(1.0,1.0,1.0,1.0),occ, _OccStrength);
		//TODO: occlude lightprobe SH by diffuse AO
		ExposureIBL.xy *= occ.rg;
	#endif
	
	//NORMALS	
	#ifdef MARMO_NORMALMAP
		float3 localN = UnpackNormal(tex2D(_BumpMap,uv_bump));
		//localN and viewDir are in tangent-space
		#ifdef MARMO_HQ
			localN = normalize(localN);
		#endif
		OUT.Normal = localN;
		float3 worldN = WorldNormalVector(IN,localN);
	#else
		float3 worldN = IN.worldNormal;
		#ifdef MARMO_HQ
			worldN = normalize(worldN);
		#endif
		#if !defined(MARMO_NORMALMAP) && defined(UNITY_PASS_PREPASSFINAL)
			float3 localN = float3(0.0,0.0,1.0);
			//localN and viewDir are in tangent-space
		#else
			float3 localN = worldN;
			//localN and viewDir are in world-space
		#endif
	#endif
	
	//SPECULAR
	#if defined(MARMO_SPECULAR_DIRECT) || defined(MARMO_SPECULAR_IBL)
		#ifdef MARMO_DIFFUSE_SPECULAR_COMBINED
			half4 spec = diffspec;
		#else
			half4 spec = tex2D(_SpecTex, uv_spec);
		#endif
		float3 localE = IN.viewDir;
		#ifdef MARMO_HQ
			localE = normalize(localE);
			half fresnel = splineFresnel(localN, localE, _SpecInt, _Fresnel);
		#else
			half fresnel = fastFresnel(localN, localE, _SpecInt, _Fresnel);		
		#endif
			
		//camera exposure is built into OUT.Specular
		spec.rgb *= _SpecColor.rgb;

		//filter the light that reaches diffuse reflection by specular intensity		
		#ifdef MARMO_SPECULAR_FILTER
			//Light reaching diffuse is filtered by 1-specColor*specIntensity
			half3 specFilter = half3(1.0,1.0,1.0) - spec.rgb * _SpecInt;
			
			//If the material exhibits strong fresnel, bias the filter some.
			specFilter += _Fresnel.xxx*0.5;
			
			//don't let it get t crazy, clamp 0-1 and apply
			OUT.Albedo *= saturate(specFilter);
		#endif
		
		spec.rgb *= fresnel * ExposureIBL.w;
		half glossLod = glossLOD(spec.a, _Shininess);		
		#ifdef MARMO_SPECULAR_DIRECT
			OUT.SpecularRGB = spec.rgb;
			OUT.Specular = glossExponent(glossLod);
			//conserve energy by dividing out specular integral		
			OUT.SpecularRGB *= specEnergyScalar(OUT.Specular);
			OUT.Specular *= 0.00390625; // 1/256
		#endif
	#endif
	
	#if MARMO_BOX_PROJECTION
		float3 worldP = IN.worldPos;
	#else
		float3 worldP = float3(0.0,0.0,0.0);
	#endif
	
	//SPECULAR IBL
	#ifdef MARMO_SPECULAR_IBL
		float3 skyR = WorldReflectionVector(IN, localN);
		skyR = skyProject(SkyMatrix, InvSkyMatrix, _SkySize, worldP, skyR);
		#ifdef MARMO_MIP_GLOSS
			half3 specIBL = glossCubeLookup(_SpecCubeIBL, skyR, glossLod);
		#else
			half3 specIBL =  specCubeLookup(_SpecCubeIBL, skyR)*spec.a;
		#endif
		OUT.Emission += specIBL.rgb * spec.rgb * ExposureIBL.y;
	#endif
	
	//DIFFUSE IBL
	#ifdef MARMO_DIFFUSE_IBL
		float3 skyN = worldN;
		skyN = skyRotate(SkyMatrix, skyN);
		//skyN = skyProject(SkyMatrix, InvSkyMatrix, 2.0*_SkySize, worldP, skyN);
		#if MARMO_SPHERICAL_HARMONICS
			//spherical harmonics
			skyN = normalize(skyN);
			float3 band0, band1, band2;
			SHLookup(skyN,band0,band1,band2);
			half3 diffIBL = lightingSH(band0, band1, band2) * _SHScale;
		#else
			half3 diffIBL = diffCubeLookup(_DiffCubeIBL, skyN);
		#endif
		OUT.Emission += diffIBL * OUT.Albedo.rgb * ExposureIBL.x;
	#endif
	
	//GLOW
	#ifdef MARMO_GLOW
		half4 glow = tex2D(_Illum, uv_glow);
		glow.rgb *= _GlowColor.rgb;
		glow.rgb *= _GlowStrength;
		glow.rgb *= ExposureIBL.w;
		glow.a *= _EmissionLM;
		//NOTE: camera exposure is already in albedo from above
		glow.rgb += OUT.Albedo * glow.a;
		OUT.Emission += glow.rgb;
	#endif
	#ifndef MARMO_ALPHA
		OUT.Alpha = 1.0;
	#endif
}

#endif