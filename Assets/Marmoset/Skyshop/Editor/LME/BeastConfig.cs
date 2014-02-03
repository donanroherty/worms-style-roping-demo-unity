// Copyright (c) 2012 Michael Stevenson
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in the
// Software without restriction, including without limitation the rights to use, copy,
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software,
// and to permit persons to whom the Software is furnished to do so, subject to the
// following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies
// or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE
// OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Collections; 
using UnityEditorInternal;

namespace mset
{
	//A subset of LMExtendedWindow that deals specifically with Environment and GI baking.
	//This class generates, edits, and reserializes BeastSettings.xml files and should
	//be able to share them with Lightmapping Extended.
	public class BeastConfig {
		private static mset_SerializedConfig _sc;
		private static mset_SerializedConfig sc {
			get {
				if(_sc == null)
					_sc = ScriptableObject.CreateInstance<mset_SerializedConfig>();
				return _sc;
			}
		}

		public static SerializedObject getSerializedConfig() {
			return new SerializedObject(sc);
		}
		
		public static void RefreshConfig() {
			if(!File.Exists(configFilePath)) {
				sc.config = null;
			} else {
				sc.config = mset.ILConfig.DeserializeFromPath(configFilePath);
			}
		}
		
		public static string configFilePath {
			get {
				if(string.IsNullOrEmpty(EditorApplication.currentScene))
					return "";
				string root = Path.GetDirectoryName(EditorApplication.currentScene);
				string dir = Path.GetFileNameWithoutExtension(EditorApplication.currentScene);
				string path = Path.Combine(root, dir);
				path = Path.Combine(path, "BeastSettings.xml");
				return path;
			}
		}
	
		public static void SaveConfig() {
			sc.config.SerializeToPath(configFilePath);
		}
		
		public static bool hasConfigFile() {
			string path = configFilePath;
			if(string.IsNullOrEmpty(path)) return false; // no open scene
			return File.Exists(path);
		}
		
		private static bool ConfigGUI() {
			string path = configFilePath;
			if(string.IsNullOrEmpty(path)) {
				EditorGUILayout.HelpBox("Open a scene file to edit its lightmapping settings.", MessageType.Info);
				return false;
			}
			if(!File.Exists(path)) {
				sc.config = null;
			}
			// Determine if config file exists
			bool haveConfigFile = false;
			if(sc.config == null) {
				if(File.Exists(path)) {
					sc.config = mset.ILConfig.DeserializeFromPath(path);
					haveConfigFile = true;
				}
			} else {
				haveConfigFile = true;
			}
			// Option to generate a config file
			if( !haveConfigFile ) {
				EditorGUILayout.Space();
				if(GUILayout.Button("Generate Beast settings file for current scene", GUILayout.Width(320), GUILayout.Height(40))) {
					//SetPresetToDefault();
					mset.ILConfig newConfig = new mset.ILConfig();
					var dir = Path.GetDirectoryName(configFilePath);
					if(!Directory.Exists(dir))
						Directory.CreateDirectory(dir);
					newConfig.SerializeToPath(configFilePath);
					sc.config = mset.ILConfig.DeserializeFromPath(path);
					AssetDatabase.Refresh();
					GUIUtility.ExitGUI();
				}
				return false;
			}
			return true;
		}
	
		public static void DrawGUI() {
			if( !ConfigGUI() ) return;
			SerializedObject serializedConfig = getSerializedConfig();
			SerializedProperty giEnvironment = 			serializedConfig.FindProperty("config.environmentSettings.giEnvironment");
			SerializedProperty giEnvironmentIntensity = serializedConfig.FindProperty("config.environmentSettings.giEnvironmentIntensity");
			SerializedProperty iblImageFile = 			serializedConfig.FindProperty("config.environmentSettings.iblImageFile");
			SerializedProperty iblSwapYZ = 				serializedConfig.FindProperty("config.environmentSettings.iblSwapYZ");
			SerializedProperty iblTurnDome = 			serializedConfig.FindProperty("config.environmentSettings.iblTurnDome");
			SerializedProperty iblGIEnvBlur = 			serializedConfig.FindProperty("config.environmentSettings.iblGIEnvBlur");
			SerializedProperty iblEmitLight = 			serializedConfig.FindProperty("config.environmentSettings.iblEmitLight");
			SerializedProperty iblSamples = 			serializedConfig.FindProperty("config.environmentSettings.iblSamples");
			SerializedProperty iblIntensity = 			serializedConfig.FindProperty("config.environmentSettings.iblIntensity");
			SerializedProperty iblEmitDiffuse = 		serializedConfig.FindProperty("config.environmentSettings.iblEmitDiffuse");
			SerializedProperty iblEmitSpecular = 		serializedConfig.FindProperty("config.environmentSettings.iblEmitSpecular");
			SerializedProperty iblSpecularBoost = 		serializedConfig.FindProperty("config.environmentSettings.iblSpecularBoost");
			SerializedProperty iblShadows = 			serializedConfig.FindProperty("config.environmentSettings.iblShadows");
			SerializedProperty iblBandingVsNoise = 		serializedConfig.FindProperty("config.environmentSettings.iblBandingVsNoise");
			
			EditorGUILayout.PropertyField(giEnvironment, new GUIContent("Environment Type", ""), GUILayout.Width(300));
			EditorGUI.BeginDisabledGroup(sc.config.environmentSettings.giEnvironment == mset.ILConfig.EnvironmentSettings.Environment.None);
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(giEnvironmentIntensity, new GUIContent("Intensity", ""), GUILayout.Width(300));
	
			if(sc.config.environmentSettings.giEnvironment == mset.ILConfig.EnvironmentSettings.Environment.SkyLight) {
				// FIXME add undo
				LMColorPicker("Sky Light Color", ref sc.config.environmentSettings.skyLightColor, "It is often a good idea to keep the color below 1.0 in intensity to avoid boosting by gamma correction. Boost the intensity instead with the giEnvironmentIntensity setting.");
			}
			else if(sc.config.environmentSettings.giEnvironment == mset.ILConfig.EnvironmentSettings.Environment.IBL) {
				GUILayout.Label("IBL Image", EditorStyles.boldLabel);
				EditorGUILayout.PrefixLabel(new GUIContent("Image Path",""));
				GUILayout.BeginHorizontal(); {
					EditorGUILayout.PropertyField(iblImageFile, new GUIContent("","The absolute image file path to use for IBL. Accepts hdr or OpenEXR format. The file should be long-lat. Use giEnvironmentIntensity to boost the intensity of the image."));
					GUILayout.Space(10);
				} GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
				{
					//GUILayout.FlexibleSpace();
					GUILayout.Space(10);
					if(!string.IsNullOrEmpty(sc.config.environmentSettings.iblImageFile)) {
						if(GUILayout.Button("Reveal", GUILayout.Width(55))) {
							EditorUtility.OpenWithDefaultApp(Path.GetDirectoryName(sc.config.environmentSettings.iblImageFile));
						}
						if(GUILayout.Button("Edit", GUILayout.Width(55))) {
							EditorUtility.OpenWithDefaultApp(sc.config.environmentSettings.iblImageFile);
						}
					}
					if(GUILayout.Button("Open...", GUILayout.Width(60))) {
						string file = EditorUtility.OpenFilePanel("Select EXR or HDR file", "", "");
						string ext = Path.GetExtension(file);
						if(!string.IsNullOrEmpty(file)) {
							if(ext == ".exr" || ext == ".hdr") {
								sc.config.environmentSettings.iblImageFile = file;
								iblImageFile.stringValue = file;
								GUI.changed = true;
								SaveConfig();
							} else {
								Debug.LogError("IBL image files must use the extension .exr or .hdr");
							}
						}
					}
				}
				GUILayout.EndHorizontal();
				EditorGUILayout.Space();
				EditorGUILayout.PropertyField(iblGIEnvBlur,
					new GUIContent("Image Blur", "Pre-blur the environment image for Global Illumination calculations. Can help to reduce noise and flicker in images rendered with Final Gather. May increase render time as it is blurred at render time. It is always cheaper to pre-blur the image itself in an external application before loading it into Beast."),
					GUILayout.Width(300)
				);
				
				EditorGUILayout.Space();
				EditorGUILayout.PropertyField(iblSwapYZ, new GUIContent("Dome Swap Y/Z", "Swap the Up Axis. Default value is false, meaning that Y is up."));			
				EditorGUILayout.Slider(	iblTurnDome,
					0,360,
					new GUIContent("Dome Rotation", "The sphere that the image is projected on can be rotated around the up axis. The amount of rotation is given in degrees."),
					GUILayout.Width(300)
				);
				GUILayout.BeginHorizontal();
				{
					GUILayout.Space(10);
					if(GUILayout.Button(new GUIContent("Align to Active Sky", "Matches Dome Rotation to the active Skyshop Sky. Note: Skyshop at 0 degrees is Beast at 90."), GUILayout.Width(135))) {
						float theta = 90;
						if(mset.Sky.activeSky) theta += mset.Sky.activeSky.transform.rotation.eulerAngles.y;
						iblTurnDome.floatValue = Mathf.Repeat(theta,360f);
						GUI.changed = true;
					}	
				}
				GUILayout.EndHorizontal();
				
				EditorGUILayout.Space();
				GUILayout.Label("IBL Light", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(
					iblEmitLight,
					new GUIContent("Emit Light", "Turns on the expensive IBL implementation. This will generate a number of(iblSamples) directional lights from the image."),
					GUILayout.Width(300)
				);
				if(sc.config.environmentSettings.iblEmitLight) EditorGUILayout.HelpBox("The scene will be lit by a number of directional lights with colors sampled from the IBL image. Very expensive.", MessageType.None);
				else EditorGUILayout.HelpBox("The scene will be lit with Global Illumination using the IBL image as a simple environment.", MessageType.None);
				
				EditorGUI.BeginDisabledGroup(!sc.config.environmentSettings.iblEmitLight);
				{
					EditorGUILayout.PropertyField(iblSamples, new GUIContent("Samples", "The number of samples to be taken from the image. This will affect how soft the shadows will be, as well as the general lighting. The higher number of samples, the better the shadows and lighting."), GUILayout.Width(300));
					EditorGUILayout.PropertyField(iblIntensity, new GUIContent("IBL Intensity", "Sets the intensity of the lighting."), GUILayout.Width(300));
					EditorGUILayout.PropertyField(iblEmitDiffuse, new GUIContent("Diffuse", "To remove diffuse lighting from IBL, set this to false. To get the diffuse lighting Final Gather could be used instead."), GUILayout.Width(300));
					EditorGUILayout.PropertyField(iblEmitSpecular, new GUIContent("Specular", "To remove specular highlights from IBL, set this to false."), GUILayout.Width(300));
					EditorGUI.indentLevel++;
					{
						if(!sc.config.environmentSettings.iblEmitSpecular)
							GUI.enabled = false;
						EditorGUILayout.PropertyField(iblSpecularBoost, new GUIContent("Specular Boost", "Further tweak the intensity by boosting the specular component."), GUILayout.Width(300));
						if(sc.config.environmentSettings.iblEmitLight)
							GUI.enabled = true;
					}
					EditorGUI.indentLevel--;
					EditorGUILayout.PropertyField(iblShadows, new GUIContent("Shadows", "Controls whether shadows should be created from IBL when this is used."), GUILayout.Width(300));
					{
						EditorGUI.indentLevel++;
						if(!sc.config.environmentSettings.iblShadows)
							GUI.enabled = false;
						EditorGUILayout.PropertyField(iblBandingVsNoise, new GUIContent("Shadow Noise", "Controls the appearance of the shadows, banded shadows look more aliased, but noisy shadows flicker more in animations."), GUILayout.Width(300));
						if(sc.config.environmentSettings.iblEmitLight)
							GUI.enabled = true;
					}
					EditorGUI.indentLevel--;
				}
				EditorGUI.EndDisabledGroup();
				EditorGUILayout.Space();
			}
			EditorGUI.indentLevel--;
			EditorGUI.EndDisabledGroup();
			
			if(GUI.changed) {
				serializedConfig.ApplyModifiedProperties();
				SaveConfig();
			}
		
			float width = 80;
			float height = 30;
			bool disabled = !InternalEditorUtility.HasPro();
			if( disabled ) {
				EditorGUILayout.HelpBox("Global Illumination baking is a Unity Pro feature :(", MessageType.Error);
			}
			EditorGUI.BeginDisabledGroup(disabled);
			{
				EditorGUILayout.Space();
				GUILayout.BeginHorizontal();
				{
					GUILayout.Space(10);
					EditorGUI.BeginDisabledGroup(Lightmapping.isRunning);
					if(GUILayout.Button("Clear", GUILayout.Width(width))) {
						if(EditorUtility.DisplayDialog("Clear Lightmapping Data", "Do you want to clear all lightmap and probe data from the scene?", "OK", "Cancel")) {
							Lightmapping.Clear();
						}
					}
					EditorGUI.EndDisabledGroup();
				}
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal(); {
					if(!Lightmapping.isRunning) {
						GUILayout.Space(10);
						bool scene = 	GUILayout.Button("Bake Scene", GUILayout.Height(height));
						bool selected = GUILayout.Button("Bake Selected", GUILayout.Height(height));
						bool probes =   GUILayout.Button("Bake Probes", GUILayout.Height(height));
							
						if((scene || selected || probes) && CheckSettingsIntegrity()) {
							if(scene) 			Lightmapping.BakeAsync();
							else if(selected)	Lightmapping.BakeSelectedAsync();
							else if(probes)		Lightmapping.BakeLightProbesOnlyAsync();
						}
						GUILayout.Space(10);
					} else {
						GUILayout.Space(10);
						if(GUILayout.Button("Cancel", GUILayout.Width(width), GUILayout.Height(height))) {
							Lightmapping.Cancel();
						}
					}
				} GUILayout.EndHorizontal();
			}
			EditorGUI.EndDisabledGroup();
		}
		
		private static bool CheckSettingsIntegrity() {
			if(sc.config.environmentSettings.giEnvironment == mset.ILConfig.EnvironmentSettings.Environment.IBL) {
				if(string.IsNullOrEmpty(sc.config.environmentSettings.iblImageFile)) {
					EditorUtility.DisplayDialog("Missing IBL image", "The lightmapping environment type is set to IBL, but no IBL image file is available. Either change the environment type or specify an HDR or EXR image file path.", "Ok");
					Debug.LogError("Lightmapping cancelled, environment type set to IBL but no IBL image file was specified.");
					return false;
				} else if(!File.Exists(sc.config.environmentSettings.iblImageFile)) {
					EditorUtility.DisplayDialog("Missing IBL image", "The lightmapping environment type is set to IBL, but there is no compatible image file at the specified path. Either change the environment type or specify an absolute path to an HDR or EXR image file.", "Ok");
					Debug.LogError("Lightmapping cancelled, environment type set to IBL but the absolute path to an IBL image is incorrect.");
					return false;
				}
			}
			return true;
		}
	
		private static void LMColorPicker(string name, ref ILConfig.LMColor color, string tooltip) {
			Color c = EditorGUILayout.ColorField(new GUIContent(name, tooltip), new Color(color.r, color.g, color.b, color.a), GUILayout.Width(220));
			color = new mset.ILConfig.LMColor(c.r, c.g, c.b, c.a);
		}
	}
}

