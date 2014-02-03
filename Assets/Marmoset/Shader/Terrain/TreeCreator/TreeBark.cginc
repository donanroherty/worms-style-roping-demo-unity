#ifndef MARMO_TREE_BARK_CGINC
#define MARMO_TREE_BARK_CGINC

half4 		ExposureIBL;
samplerCUBE _DiffCubeIBL;
samplerCUBE _SpecCubeIBL;
#ifdef MARMO_SKY_ROTATION
float4x4	SkyMatrix;
#endif

sampler2D _MainTex;
sampler2D _BumpMap;
sampler2D _GlossMap;
half _Shininess;
half _SpecInt;
half _Fresnel;

struct Input {
	float2 uv_MainTex;
	float3 worldNormal;
	#ifdef MARMO_SPECULAR_DIRECT
		float3 viewDir;
		float3 worldRefl;
	#endif
	fixed4 color : COLOR;
	INTERNAL_DATA
};

void surf (Input IN, inout SurfaceOutput o) {
	fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
	o.Albedo = c.rgb * _Color.rgb * IN.color.a;
	o.Alpha = c.a;
	
	o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
	float3 N = WorldNormalVector(IN, o.Normal);
	#ifdef MARMO_SKY_ROTATION
		N = mulVec3(SkyMatrix,N);
	#endif
	
	#ifdef MARMO_SPECULAR_DIRECT
		o.Gloss = tex2D(_GlossMap, IN.uv_MainTex).a;
		o.Specular = _Shininess;
		o.Gloss *= specEnergyScalar(o.Specular*128); 
		float3 E = IN.viewDir; //E is in whatever space N is
		E = normalize(E);
		half fresnel = splineFresnel(o.Normal, E, _SpecInt, _Fresnel);
		_SpecInt *= fresnel;
		o.Gloss *= _SpecInt;
		
		#ifdef MARMO_NORMALMAP
			float3 R = WorldReflectionVector(IN, o.Normal);
		#else 
			float3 R = IN.worldRefl;
		#endif
		#ifdef MARMO_SKY_ROTATION
			R = mulVec3(SkyMatrix,R);
		#endif
		float lod = glossLOD(o.Gloss, _Shininess * 6.0 + 2.0);
		o.Emission += glossCubeLookup(_SpecCubeIBL, R, lod) * ExposureIBL.y * _SpecInt;
	#endif
	
	o.Albedo *= ExposureIBL.w;
	o.Emission += diffCubeLookup(_DiffCubeIBL, N) * o.Albedo * ExposureIBL.x;
}

#endif