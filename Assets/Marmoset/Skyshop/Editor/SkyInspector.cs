// Marmoset Skyshop
// Copyright 2013 Marmoset LLC
// http://marmoset.co

using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using mset;

namespace mset {
	[CustomEditor(typeof(Sky))]
	public class SkyInspector : Editor {
		// Singleton cubemap references because Inspectors get
		// allocated like candy and this speeds up selection
		// exponentially.

		//Don't serialize singletons
		[NonSerialized] private static mset.CubemapGUI _refSKY = null;
		[NonSerialized] private static mset.CubemapGUI _refDIM = null;
		[NonSerialized] private static mset.CubemapGUI _refSIM = null;
		private static mset.CubemapGUI refSKY {
			get {
				if (_refSKY == null) {
					_refSKY = mset.CubemapGUI.create(mset.CubemapGUI.Type.SKY, true);
				} return _refSKY;
			}
		}
		private static mset.CubemapGUI refDIM {
			get {

				if (_refDIM == null) {
					_refDIM = mset.CubemapGUI.create(mset.CubemapGUI.Type.DIM, true);
				}
				return _refDIM;
			}
		}
		private static mset.CubemapGUI refSIM {
			get {
				if (_refSIM == null) {
					_refSIM = mset.CubemapGUI.create(mset.CubemapGUI.Type.SIM, true);
				}
				return _refSIM;
			}
		}
				
		[SerializeField] private float camExposure = 1f;
		[SerializeField] private float masterIntensity = 1f;
		[SerializeField] private float skyIntensity = 1f;
		[SerializeField] private float diffIntensity = 1f;
		[SerializeField] private float specIntensity = 1f;
		
		[SerializeField] private float diffIntensityLM = 1f;
		[SerializeField] private float specIntensityLM = 1f;

		private static bool forceDirty = false;
		public  static void forceRefresh() { forceDirty = true; }

		public void OnEnable() {
			mset.Sky sky = target as mset.Sky;
			
			camExposure = sky.camExposure;
			masterIntensity = sky.masterIntensity;
			skyIntensity =  sky.skyIntensity;
			diffIntensity = sky.diffIntensity;
			specIntensity = sky.specIntensity;
			diffIntensityLM = sky.diffIntensityLM;
			specIntensityLM = sky.specIntensityLM;
			
			refSKY.HDR = sky.hdrSky;
			refSIM.HDR = sky.hdrSpec;
			refDIM.HDR = sky.hdrDiff;
			
			//forceDirty = true;
		}
		
		public void OnDisable() {
		}
		
		public void OnDestroy() {
			System.GC.Collect();
		}
		
		private static bool skyToGUI(ref Cubemap skyCube, ref bool skyHDR, mset.CubemapGUI cubeGUI, bool updatePreview) {
			bool dirty = false;
			bool dirtyGUI = false;
			
			//sky -> cubeGUI
			dirtyGUI |= cubeGUI.HDR != skyHDR;
			cubeGUI.HDR = skyHDR;
			
			if(cubeGUI.cube != skyCube) {
				if(skyCube) {
					string path = AssetDatabase.GetAssetPath(skyCube);
					cubeGUI.setReference(path, cubeGUI.mipmapped);
				} else {
					cubeGUI.clear();
				}
				//dirty = true;
			}
			if( dirtyGUI && updatePreview ) {
				cubeGUI.updatePreview();
			}
			return dirty;
		}

		private static bool GUIToSky(ref Cubemap skyCube, ref bool skyHDR, mset.CubemapGUI cubeGUI) {
			//cubeGUI -> sky
			bool prevHDR = cubeGUI.HDR;
			Cubemap prevCube = cubeGUI.cube;
			cubeGUI.drawGUI();
			
			skyCube = cubeGUI.cube;
			skyHDR = cubeGUI.HDR;
			
			bool dirty = false;
			//return true if the cubeGUI gui changed any parameters
			dirty |= prevHDR != cubeGUI.HDR;
			dirty |= prevCube != cubeGUI.cube;	
			return dirty;
		}
		
		public static void detectColorSpace(ref mset.Sky sky) {
			sky.linearSpace = PlayerSettings.colorSpace == ColorSpace.Linear;
			#if UNITY_IPHONE || UNITY_ANDROID
				sky.linearSpace = false; // no sRGB on mobile
			#endif
		}

		public static void generateSH(ref mset.Sky sky) {
			if( sky.SH == null ) sky.SH = new mset.SHEncoding();

			skyToGUI(ref sky.skyboxCube, ref sky.hdrSky, refSKY, false);
			skyToGUI(ref sky.diffuseCube, ref sky.hdrDiff, refDIM, false);
			skyToGUI(ref sky.specularCube, ref sky.hdrSpec, refSIM, false);
			
			if( refSIM.cube != null ) {
				refSIM.projectToSH(ref sky.SH);
				mset.SHUtil.convolve(ref sky.SH);
			}
			else if( refDIM.cube != null ) {
				refDIM.projectToSH(ref sky.SH);
			}
			else if( refSKY.cube != null ) {
				refSKY.projectToSH(ref sky.SH);
				mset.SHUtil.convolve(ref sky.SH);
			}

			if( sky.SH != null ) {
				sky.SH.copyToBuffer();
			}
		}

		public override void OnInspectorGUI() {
			bool dirty = false;		//flag for changed sky parameters
			bool dirtyRef = false;	//flag for changed sky cubemap references (causes SH recompute and preview refresh)

			mset.Sky sky = target as mset.Sky;
			
			//sync GUI from sky
			camExposure = sky.camExposure;
			masterIntensity = sky.masterIntensity;
			skyIntensity = sky.skyIntensity;
			diffIntensity = sky.diffIntensity;
			specIntensity = sky.specIntensity;
			diffIntensityLM = sky.diffIntensityLM;
			specIntensityLM = sky.specIntensityLM;
			
			//sync and sync from CubeGUIs
			dirtyRef |= skyToGUI(ref sky.skyboxCube, ref sky.hdrSky, refSKY, true);
			dirtyRef |= GUIToSky(ref sky.skyboxCube, ref sky.hdrSky, refSKY);
			
			dirtyRef |= skyToGUI(ref sky.diffuseCube, ref sky.hdrDiff, refDIM, true);
			dirtyRef |= GUIToSky(ref sky.diffuseCube, ref sky.hdrDiff, refDIM);
			
			dirtyRef |= skyToGUI(ref sky.specularCube, ref sky.hdrSpec, refSIM, true);
			dirtyRef |= GUIToSky(ref sky.specularCube, ref sky.hdrSpec, refSIM);
			
			GUIStyle buttonStyle = new GUIStyle("Button");
			buttonStyle.padding.top = buttonStyle.padding.bottom = 0;
			buttonStyle.padding.left = buttonStyle.padding.right = 0;

			EditorGUILayout.Space();
			EditorGUILayout.Space();
			
			bool prevApply = sky.autoApply;
			bool autoApply = EditorGUILayout.Toggle(new GUIContent("Auto-Apply", "If enabled, this sky will be applied automatically upon level load, or whenever selected in the editor viewport."), sky.autoApply);
			if( autoApply != prevApply ) {
				mset.Util.RegisterUndo(sky, "Auto-Apply Toggle");
				sky.autoApply = autoApply;
			}
			EditorGUI.BeginDisabledGroup(sky.autoApply); {
				if( GUILayout.Button("Apply to Viewport", GUILayout.Width(164)) ) {
					sky.Apply();
				}
			} EditorGUI.EndDisabledGroup();

			EditorGUILayout.Space();
			EditorGUILayout.Space();

			bool showSkybox = EditorGUILayout.Toggle(new GUIContent("Show Skybox","Toggles rendering the background image"), sky.showSkybox);
			if( showSkybox != sky.showSkybox ){
				mset.Util.RegisterUndo(sky,"Skybox Toggle");
				sky.showSkybox = showSkybox;			
				// if we're toggling skyboxes off, clear the render settings material
				if( !sky.showSkybox ) RenderSettings.skybox = null;
				dirty = true;
			}
			
			bool detect = EditorGUILayout.Toggle(new GUIContent("Auto-Detect Color Space","If enabled, attempts to detect the project's gamma correction setting and enables/disables the Linear Space option accordingly"), sky.autoDetectColorSpace);
			if( detect != sky.autoDetectColorSpace ) {
				mset.Util.RegisterUndo(sky, "Color-Space Detection Change");
				sky.autoDetectColorSpace = detect;
			}
			
			bool prevLinear = sky.linearSpace;
			if( sky.autoDetectColorSpace ) {
				detectColorSpace(ref sky);
			}
			EditorGUI.BeginDisabledGroup(sky.autoDetectColorSpace);
				bool userLinearSpace = EditorGUILayout.Toggle(new GUIContent("Linear Space","Enable if gamma correction is enabled for this project (Edit -> Project Settings -> Player -> Color Space: Linear)"), sky.linearSpace);
				if( userLinearSpace != sky.linearSpace ) {
					mset.Util.RegisterUndo(sky, "Color-Space Change");
					sky.linearSpace = userLinearSpace;
				}
			EditorGUI.EndDisabledGroup();
			if( prevLinear != sky.linearSpace ){
			//	dirty = true;
			}
			
			bool prevDim = sky.hasDimensions;
			bool hasDim = EditorGUILayout.Toggle(new GUIContent("Has Dimensions (beta)", "Use transform scale as the box projection dimensions of this sky. Only affects box-projected shaders for now."), sky.hasDimensions);
			if( hasDim != prevDim ) {
				mset.Util.RegisterUndo(sky, "Has Dimensions Toggle");
				sky.hasDimensions = hasDim;
			}
					
			EditorGUILayout.Space();
			EditorGUILayout.Space();
			EditorGUILayout.Space();
			
			//sync sky from GUI
			EditorGUILayout.LabelField(new GUIContent("Master Intensity","Multiplier on the Sky, Diffuse, and Specular cube intensities"));
			masterIntensity = EditorGUILayout.Slider(masterIntensity, 0f, 10f);
			if(sky.masterIntensity != masterIntensity) {
				mset.Util.RegisterUndo(sky,"Intensity Change");
				sky.masterIntensity = masterIntensity;
			}
			
			EditorGUILayout.LabelField(new GUIContent("Skybox Intensity", "Brightness of the skybox"));
			skyIntensity = EditorGUILayout.Slider(skyIntensity, 0f, 10f);
			if(sky.skyIntensity != skyIntensity) {
				mset.Util.RegisterUndo(sky,"Intensity Change");
				sky.skyIntensity = skyIntensity;
			}
			
			EditorGUILayout.LabelField(new GUIContent("Diffuse Intensity", "Multiplier on the diffuse light put out by this sky"));
			diffIntensity = EditorGUILayout.Slider(diffIntensity, 0f, 10f);
			if(sky.diffIntensity != diffIntensity) {
				mset.Util.RegisterUndo(sky,"Intensity Change");
				sky.diffIntensity = diffIntensity;
			}
			
			EditorGUILayout.LabelField(new GUIContent("Specular Intensity", "Multiplier on the specular light put out by this sky"));
			specIntensity = EditorGUILayout.Slider(specIntensity, 0f, 10f);
			if(sky.specIntensity != specIntensity) {
				mset.Util.RegisterUndo(sky,"Intensity Change");
				sky.specIntensity = specIntensity;
				dirty = true;
			}
			
			EditorGUILayout.Space();
			EditorGUILayout.Space();
			EditorGUILayout.Space();
			
			EditorGUILayout.LabelField(new GUIContent("Camera Exposure","Multiplier on all light coming into the camera, including IBL, direct light, and glow maps"));
			camExposure = EditorGUILayout.Slider(camExposure, 0f, 10f);
			if(sky.camExposure != camExposure) {
				mset.Util.RegisterUndo(sky,"Exposure Change");
				sky.camExposure = camExposure;
			}
			
			EditorGUILayout.Space();
			EditorGUILayout.Space();
			
			EditorGUILayout.LabelField(new GUIContent("Lightmapped Diffuse Multiplier", "Multiplier on the diffuse intensity for lightmapped surfaces"));
			diffIntensityLM = EditorGUILayout.Slider(diffIntensityLM, 0f, 1f);
			if(sky.diffIntensityLM != diffIntensityLM) {
				mset.Util.RegisterUndo(sky,"Multiplier Change");
				sky.diffIntensityLM = diffIntensityLM;
			}
			
			EditorGUILayout.LabelField(new GUIContent("Lightmapped Specular Multiplier", "Multiplier on the specular intensity for lightmapped surfaces"));
			specIntensityLM = EditorGUILayout.Slider(specIntensityLM, 0f, 1f);
			if(sky.specIntensityLM != specIntensityLM) {
				mset.Util.RegisterUndo(sky,"Multiplier Change");
				sky.specIntensityLM = specIntensityLM;
			}

			dirty |= GUI.changed;
			dirtyRef |= sky.SH == null;

			if( forceDirty ) {
				refSKY.reloadReference();
				refDIM.reloadReference();
				refSIM.reloadReference();

				dirtyRef = true;
				forceDirty = false;
				dirty = true;
				Repaint();
			}
					
			//guess input path
			if( dirtyRef ) {				
				string inPath = refSKY.fullPath;
				if( inPath.Length == 0 ) inPath = refDIM.fullPath;
				if( inPath.Length == 0 ) inPath = refSIM.fullPath;
				if( inPath.Length > 0 ) {
					int uscore = inPath.LastIndexOf("_");
					if( uscore > -1 ) {
						inPath = inPath.Substring(0,uscore);
					} else {
						inPath = Path.GetDirectoryName(inPath) + "/" + Path.GetFileNameWithoutExtension(inPath);
					}
					refSKY.inputPath = 
					refDIM.inputPath = 
					refSIM.inputPath = inPath;
				} else {
					refSKY.inputPath = 
					refDIM.inputPath = 
					refSIM.inputPath = "";
				}

				generateSH(ref sky);
				dirty = true;
			}

			//if the active sky is not whats selected, see whats up with that and try rebinding (catches re-activating disabled skies)
			if( mset.Sky.activeSky != sky ) dirty = true;
			
			if( !Application.isPlaying ) {

				if(sky.autoApply || sky == Sky.activeSky) {
					//if any of the cubemaps have changed, refresh the viewport
					if( dirty ) {
						sky.Apply(); //SkyInspector dirty
						SceneView.RepaintAll();
					} else {
						sky.ApplySkyTransform();
					}
				}
			}
		}
	};
}