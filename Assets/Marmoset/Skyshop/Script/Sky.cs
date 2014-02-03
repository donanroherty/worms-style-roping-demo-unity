// Marmoset Skyshop
// Copyright 2013 Marmoset LLC
// http://marmoset.co
using UnityEngine;
using System.Collections;

namespace mset {
	[System.Serializable]
	public class Sky : MonoBehaviour {
		public static mset.Sky activeSky = null;
		public Cubemap diffuseCube = null;
		public Cubemap specularCube = null;
		public Cubemap skyboxCube = null;
		public float masterIntensity = 1f;
		public float skyIntensity = 1f;
		public float specIntensity = 1f;
		public float diffIntensity = 1f;
		public float camExposure = 1f;
		public float specIntensityLM = 1f;
		public float diffIntensityLM = 1f;
		public bool hdrSky = false;
		public bool hdrSpec = false;
		public bool hdrDiff = false;
		public bool showSkybox = true;
		public bool linearSpace = true;
		public bool autoDetectColorSpace = true; //for inspector use
		public bool hasDimensions = false;
		public bool autoApply = true;
		public mset.SHEncoding SH = null;
		private Matrix4x4 skyMatrix = Matrix4x4.identity;
		private Matrix4x4 invMatrix = Matrix4x4.identity;
		private Vector4 exposures = Vector4.one;
		private Vector2 exposuresLM = Vector2.one;
		private float exposureSH = 1f;

		//used internally for unbinding skies from individual renderers
		[SerializeField] private float skyID = 0;

		//Skybox material, allocated only if requested
		private Material _skyboxMaterial;

		private Material skyboxMaterial {
			get {
				if(_skyboxMaterial == null) {
					Shader shader = Shader.Find("Hidden/Marmoset/Skybox IBL");
					if(shader) {
						_skyboxMaterial = new Material(shader);
						_skyboxMaterial.name = "Internal IBL Skybox";
					} else {
						Debug.LogError("Failed to create IBL Skybox material. Missing shader?");
					}
				}
				return _skyboxMaterial;
			}
		}

		//A black cubemap texture only allocated if requested
		private Cubemap _blackCube;

		private Cubemap blackCube {
			get {
				if(_blackCube == null) {
					_blackCube = new Cubemap(16, TextureFormat.ARGB32, true);
					for(int f = 0; f < 6; ++f)
						for(int x = 0; x < 16; ++x)
							for(int y = 0; y < 16; ++y) {
								_blackCube.SetPixel((CubemapFace)f, x, y, Color.black);
							}
					_blackCube.Apply(true);
				}
				return _blackCube;
			}
		}

		//pulls the appropriate material reference from the renderer for sky binding
		private static Material getTargetMaterial(Renderer target) {
#if UNITY_EDITOR
			if( Application.isPlaying ) return target.material;
			return target.sharedMaterial;
#else
			return target.material;
#endif
		}

		// Public Interface //

		public void Apply() {
			Apply(null);
		}

		public void Apply(Renderer target) {
			// Binds IBL data, exposure, and a skybox texture globally or to a specific game object

			if(this.enabled && this.gameObject.activeInHierarchy) {
				//certain global parameters are only bound on a global basis
				if(target == null) {
					//turn off previously bound sky
					if(mset.Sky.activeSky != null) mset.Sky.activeSky.UnApply();
					mset.Sky.activeSky = this;
					ToggleChildLights(true);
					Shader.SetGlobalFloat("_SkyID", skyID);

					ApplySkybox();
					//toggle between linear-space (gamma-corrected) and gamma-space (uncorrected) shader permutations
					Shader.DisableKeyword("MARMO_GAMMA");
					Shader.DisableKeyword("MARMO_LINEAR");
					if(linearSpace) Shader.EnableKeyword("MARMO_LINEAR");
					else 			Shader.EnableKeyword("MARMO_GAMMA");

				//box projection requires Unity 4.3+
				#if !(UNITY_4_0 || UNITY_4_1 || UNITY_4_2)
					//enable box projection (on the shaders that support it) only if the bound sky has dimensions
					Shader.DisableKeyword("MARMO_BOX_PROJECTION_OFF");
					Shader.DisableKeyword("MARMO_BOX_PROJECTION");
					if(hasDimensions)	Shader.EnableKeyword("MARMO_BOX_PROJECTION");
					else 				Shader.EnableKeyword("MARMO_BOX_PROJECTION_OFF");
				#endif
				} else {				
					Material mat = getTargetMaterial(target);
					mat.SetFloat("_SkyID", skyID);
				//per-material keywords were introduced in Unity 4.3
				#if !(UNITY_4_0 || UNITY_4_1 || UNITY_4_2)
					//enable box projection only if the bound sky has dimensions
					mat.DisableKeyword("MARMO_BOX_PROJECTION_OFF");
					mat.DisableKeyword("MARMO_BOX_PROJECTION");
					if(hasDimensions)	mat.EnableKeyword("MARMO_BOX_PROJECTION");
					else 				mat.EnableKeyword("MARMO_BOX_PROJECTION_OFF");
				#endif
				}

				//upload the sky transform to the shader
				ApplySkyTransform(target);
				ApplyExposures(target);
				ApplyIBL(target);
			}
		}

		public void ApplySkyTransform() {
			ApplySkyTransform(null);
		}

		public void ApplySkyTransform(Renderer target) {
			// Binds only the sky transform and bounds globally or to a specific game object.
			// Faster than full Apply in that exposures are not recomputed, lights not toggled, etc.

			if(this.enabled && this.gameObject.activeInHierarchy) {
				UpdateSkyTransform();
				if(target == null) {
					Shader.SetGlobalMatrix("SkyMatrix", skyMatrix);
					Shader.SetGlobalMatrix("InvSkyMatrix", invMatrix);
					Shader.SetGlobalVector("_SkySize", 0.5f * transform.localScale);
				} else {
					Material mat = getTargetMaterial(target);
					mat.SetMatrix("SkyMatrix", skyMatrix);
					mat.SetMatrix("InvSkyMatrix", invMatrix);
					mat.SetVector("_SkySize", 0.5f * transform.localScale);
				}
			}
		}

		public static void SetUniformOcclusion(Renderer target, float diffuse, float specular) {
			//Sets a custom multiplier on the diffuse and specular intensities from the active Sky.			
			Vector4 occlusion = Vector4.one;
			occlusion.x = diffuse;
			occlusion.y = specular;
			getTargetMaterial(target).SetVector("UniformOcclusion", occlusion);
		}

		public void SetCustomExposure(Renderer target, float diffInt, float specInt, float skyInt, float camExpo) {
			Vector4 expo = Vector4.one;
			ComputeExposureVector(ref expo, diffInt, specInt, skyInt, camExpo);
			Material mat = getTargetMaterial(target);
			mat.SetVector("ExposureIBL", expo);
		}

		public void ToggleChildLights(bool enable) {
			//Enable/disable all lights that are child objects of this Sky
			//NOTE: this causes scene changes on sky selection, may not be desireable in the editor!
			Light[] lights = this.GetComponentsInChildren<Light>();
			for(int i = 0; i < lights.Length; ++i) {
				lights[i].enabled = enable;
			}
		}

		//Private Functions //

		private void UnApply() {
			UnApply(null);
		}

		private void UnApply(Renderer target) {
			ToggleChildLights(false);
			if(target) {
				Material mat = getTargetMaterial(target);
				// see if the current sky is still bound, if so, null IBL inputs and set the target to use global sky settings
				float currID = mat.GetFloat("_SkyID");
				if( currID > 0f && currID == this.skyID ) {
					if( Sky.activeSky ) {
						//NOTE: calling Apply() here causes infinite loop
						Sky.activeSky.ApplySkyTransform(target); 
						Sky.activeSky.ApplyExposures(target);
						Sky.activeSky.ApplyIBL(target);
					}
					//TODO: ideally you'd want to set SH and all shader params to null here to use Shader.Global ones
					mat.SetTexture("_DiffCubeIBL", null);
					mat.SetTexture("_SpecCubeIBL", null);
				}
			}
		}

		private void UpdateSkyTransform() {
			skyMatrix.SetTRS(transform.position, transform.rotation, Vector3.one);
			invMatrix = skyMatrix.inverse;
		}

		private void ComputeExposureVector(ref Vector4 result, float diffInt, float specInt, float skyInt, float camExpo) {
			//build exposure values for shader, HDR skies need the RGBM expansion constant 6.0 in there
			result.x = masterIntensity * diffInt;
			result.y = masterIntensity * specInt;
			result.z = masterIntensity * skyInt * camExpo; //exposure baked right into skybox exposure
			result.w = camExpo;

			//prepare exposure values for gamma correction
			float toLinear = 2.2f;
			float toSRGB = 1f / toLinear;

			float hdrScale = 6f;
			if(linearSpace) {
				//HDR scale needs to be applied in linear space
				hdrScale = Mathf.Pow(6f, toLinear);
			} else {
				//Exposure values are treated as being in linear space, but the shader is working in sRGB space.
				//Move exposure into sRGB as well before applying.
				result.x = Mathf.Pow(result.x, toSRGB);
				result.y = Mathf.Pow(result.y, toSRGB);
				result.z = Mathf.Pow(result.z, toSRGB);
				result.w = Mathf.Pow(result.w, toSRGB);
			}
			//RGBM cubemaps need a scalar added to their exposure
			if(hdrDiff)
				result.x *= hdrScale;
			if(hdrSpec)
				result.y *= hdrScale;
			if(hdrSky)
				result.z *= hdrScale;
		}

		private void UpdateExposures() {
			//TODO: simplify this in the shader so 1/hdr is not needed for SH
			float hdrScale = 6f;
			float toLinear = 2.2f;
			if(linearSpace) {
				hdrScale = Mathf.Pow(6f, toLinear);
			}			
			exposureSH = 1f / hdrScale;

			ComputeExposureVector(ref exposures, diffIntensity, specIntensity, skyIntensity, camExposure);
			exposuresLM.x = diffIntensityLM;
			exposuresLM.y = specIntensityLM;

		}

		private void ApplyExposures(Renderer target) {
			UpdateExposures();
			if(target == null) {
				Shader.SetGlobalVector("ExposureIBL", exposures);
				Shader.SetGlobalVector("ExposureLM", exposuresLM);
				//this is a hint for the Beast Lightmapper, rendering is unaffected
				Shader.SetGlobalFloat("_EmissionLM", 1f);
				Shader.SetGlobalVector("UniformOcclusion", Vector4.one);
				Shader.SetGlobalFloat("_SHScale", exposureSH);
			} else {
				Material mat = getTargetMaterial(target);
				mat.SetVector("ExposureIBL", exposures);
				mat.SetVector("ExposureLM", exposuresLM);
				mat.SetFloat("_SHScale", exposureSH);
			}
		}

		private void ApplyIBL(Renderer target) {
			if(target == null) {
				//bind cubemaps
				if(diffuseCube) Shader.SetGlobalTexture("_DiffCubeIBL", diffuseCube);
				else 			Shader.SetGlobalTexture("_DiffCubeIBL", blackCube);
				if(specularCube)Shader.SetGlobalTexture("_SpecCubeIBL", specularCube);
				else 			Shader.SetGlobalTexture("_SpecCubeIBL", blackCube);
				if(skyboxCube) 	Shader.SetGlobalTexture("_SkyCubeIBL", skyboxCube);
				else 			Shader.SetGlobalTexture("_SkyCubeIBL", blackCube);

				//bind spherical harmonics
				if(this.SH != null) {
					for(uint i = 0; i < 9; ++i) {
						Shader.SetGlobalVector("_SH" + i, this.SH.cBuffer[i]);
					}
				}
			} else {
				Material mat = getTargetMaterial(target);
				//bind cubemaps
				if(diffuseCube) mat.SetTexture("_DiffCubeIBL", diffuseCube);
				else 			mat.SetTexture("_DiffCubeIBL", blackCube);
				if(specularCube)mat.SetTexture("_SpecCubeIBL", specularCube);
				else 			mat.SetTexture("_SpecCubeIBL", blackCube);
				if(skyboxCube) 	mat.SetTexture("_SkyCubeIBL", skyboxCube);
				else 			mat.SetTexture("_SkyCubeIBL", blackCube);

				//bind spherical harmonics
				if(this.SH != null) {
					for(int i = 0; i < 9; ++i) {
						mat.SetVector("_SH" + i, this.SH.cBuffer[i]);
					}
				}
			}
		}

		private void ApplySkybox() {
			Shader.DisableKeyword("MARMO_RGBM");
			Shader.EnableKeyword("MARMO_RGBA");
			//NOTE: this causes scene changes on sky selection, may not be desireable in the editor!
			if(showSkybox) {
				if(RenderSettings.skybox != skyboxMaterial) {
					RenderSettings.skybox = skyboxMaterial;
				}
			} else {
				if(RenderSettings.skybox && RenderSettings.skybox.name == "Internal IBL Skybox") {
					RenderSettings.skybox = null;
				}
			}
		}

		// Run-Time //		
		private void Reset() {
			skyMatrix = invMatrix = Matrix4x4.identity;
			exposures = Vector4.one;
			exposuresLM = Vector2.one;
			exposureSH = 1f;

			diffuseCube = specularCube = skyboxCube = null;
			masterIntensity = skyIntensity = specIntensity = diffIntensity = 1f;
			hdrSky = hdrSpec = hdrDiff = false;
		}

		//on enable or activate
		private void OnEnable() {
			//finalize or allocate serialized properties here
			if(SH == null)
				SH = new mset.SHEncoding();
			SH.copyToBuffer();
		}

		private void OnLevelWasLoaded(int level) {
			UpdateExposures();
			UpdateSkyTransform();
			if(this.autoApply) Apply();
		}

		//on frame the script is activated, before update		
		static float skyIDCounter = 1f;
		private void Start() {
			skyID = skyIDCounter;
			skyIDCounter += 1f;
			UpdateExposures();
			UpdateSkyTransform();
			if(this.autoApply)  Apply();
#if UNITY_ANDROID
			// on mobile devices all gloss levels are discarded
			if( this.specularCube ) {
				this.specularCube.Apply(true);
			}
#endif
		}

		private void Update() {
			if(transform.hasChanged) {
				UpdateSkyTransform();
				//NOTE: there's no way to get updated transforms to targeted Apply renderers
				if(mset.Sky.activeSky == this) {
					ApplySkyTransform();
				}
			}
		}

		//script instance is destroyed
		private void OnDestroy() {
			UnityEngine.Object.DestroyImmediate(_skyboxMaterial, false);
			SH = null;
			_skyboxMaterial = null;
			_blackCube = null;
			diffuseCube = null;
			specularCube = null;
			skyboxCube = null;
		}

		// Editor Functions //

		private void OnDrawGizmos() {
			//The most reliable place to bind shader variables in the editor
			if(this.autoApply && mset.Sky.activeSky == null) {
				this.Apply();
			}
			Gizmos.DrawIcon(transform.position, "cubelight.tga", true);
			if(this.hasDimensions) {
				Color c = new Color(0.4f, 0.7f, 1f, 0.333f);
				Gizmos.matrix = transform.localToWorldMatrix;
				Gizmos.color = c;
				Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
			}
		}

		private void OnDrawGizmosSelected() {
			//Selected skies can be changed without Inspector input, apply here also
			if(this.autoApply) Apply();
			if(this.hasDimensions) {
				Color c = new Color(0.4f, 0.7f, 1f, 1f);
				Gizmos.matrix = transform.localToWorldMatrix;
				Gizmos.color = c;
				Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
			}
		}

		private void OnTriggerEnter(Collider other) {
			if(other.renderer) {
				this.Apply(other.renderer);
			}
		}
		
		private void OnTriggerExit(Collider other) {
			if(other.renderer && Sky.activeSky) {
				UnApply(other.renderer);
			}
		}
	}
}