Shader "Marmoset/Nature/Tree Soft Occlusion Bark" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,0)
		_MainTex ("Main Texture", 2D) = "white" {}
		_BaseLight ("Base Light", Range(0, 1)) = 0.35
		_AO ("Amb. Occlusion", Range(0, 10)) = 2.4
		
		// These are here only to provide default values
		_Scale ("Scale", Vector) = (1,1,1,1)
		_SquashAmount ("Squash", Float) = 1
		
		_DiffCubeIBL ("Custom Diffuse Cube", Cube) = "black" {}
	}
	
	SubShader {
		Tags {
			"IgnoreProjector"="True"
			"RenderType" = "TreeOpaque"
		}

		Pass {
			Lighting On
		
			CGPROGRAM
			#pragma vertex bark
			#pragma fragment frag
			#pragma glsl_no_auto_normalization
			#pragma multi_compile MARMO_GAMMA MARMO_LINEAR
			
			#define MARMO_SKY_ROTATION
			half4 		ExposureIBL;
			samplerCUBE _DiffCubeIBL;
			#ifdef MARMO_SKY_ROTATION
			float4x4	SkyMatrix;
			#endif
			
			#include "SH_Vertex.cginc"

			sampler2D _MainTex;
			
			fixed4 frag(v2f input) : COLOR
			{
				half4 albedo = tex2D( _MainTex, input.uv.xy);
				albedo.rgb *= ExposureIBL.w; //camera exposure
				
				half4 light = input.color;
				half4 ibl;
				ibl.rgb = diffCubeLookup(_DiffCubeIBL,input.normal.xyz);
				ibl.rgb *= ExposureIBL.x * saturate(input.normal.w); //diff exposure & occlusion
				ibl.rgb *= _Color.rgb;
				ibl.a = 0.0;
				return (light + ibl)*albedo;
			}
			ENDCG
		}
		
		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			
			Fog {Mode Off}
			ZWrite On ZTest LEqual Cull Off
			Offset 1, 1
	
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma glsl_no_auto_normalization
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile_shadowcaster
			#include "UnityCG.cginc"
			#include "TerrainEngine.cginc"
			
			struct v2f { 
				V2F_SHADOW_CASTER;
			};
			
			struct appdata {
			    float4 vertex : POSITION;
			    fixed4 color : COLOR;
			};
			v2f vert( appdata v )
			{
				v2f o;
				TerrainAnimateTree(v.vertex, v.color.w);
				TRANSFER_SHADOW_CASTER(o)
				return o;
			}
			
			float4 frag( v2f i ) : COLOR
			{
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG	
		}
	}
	
	SubShader {
		Tags {
			"IgnoreProjector"="True"
			"RenderType" = "TreeOpaque"
		}

		Pass {
			CGPROGRAM
			#pragma exclude_renderers shaderonly
			#pragma vertex bark
			#include "SH_Vertex.cginc"
			ENDCG
			
			Lighting On
						
			SetTexture [_MainTex] { combine primary * texture DOUBLE, constant }
		}
	}
	
	SubShader {
		Tags {
			"IgnoreProjector"="True"
			"RenderType" = "Opaque"
		}
		Pass {
			Tags { "LightMode" = "Vertex" }
			Lighting On
			Material {
				Diffuse [_Color]
				Ambient [_Color]
			}
			SetTexture [_MainTex] { combine primary * texture DOUBLE, constant }
		}		
	}
	
	Dependency "BillboardShader" = "Hidden/Marmoset/Nature/Tree Soft Occlusion Bark Rendertex"
	Fallback Off
}
