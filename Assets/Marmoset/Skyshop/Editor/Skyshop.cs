// M/armoset Skyshop
// Copyright 2013 Marmoset LLC
// http://marmoset.co

using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Collections;

[Serializable]
public class Skyshop : EditorWindow {
	private mset.ProgressState 	ps;

	private Vector2		uiScroll = new Vector2(0,0);

	[SerializeField] private float		uiExposure =	1.0f;
	[SerializeField] private bool		uiMipChain =	true;
	[SerializeField] private int 		uiExponent =	256;
	[SerializeField] private bool		uiReflectionInSIM = true;

	[SerializeField] private int		uiConvoSize = 32;
	[SerializeField] private mset.TexSize uiCubeSize =  mset.TexSize._256;

	[SerializeField] private bool		uiShowPreview = false;
	[SerializeField] private bool		uiBasicOptions = true;
	[SerializeField] private bool		uiAdvancedOptions = false;
	[SerializeField] private bool		uiGIOptions = false;
	[SerializeField] private bool		uiRefOptions = true;

	[SerializeField] private mset.Quality uiConvoQuality = mset.Quality.medium;
	[SerializeField] private bool		uiResponsiveUI = false;
	[SerializeField] private bool		uiGammaCompress = true;

	private Texture2D	uiConvoPreview = null;
	private mset.Sky	uiSelectedSky = null;

	[SerializeField]
	private mset.CubemapGUI	inSKY = null;
	[SerializeField]
	private mset.CubemapGUI	outSKY = null;
	[SerializeField]
	private mset.CubemapGUI	outDIM = null;
	[SerializeField]
	private mset.CubemapGUI	outSIM = null;

	[SerializeField] private int[]		outExponents;
	[SerializeField] private int[]		outMipSizes;

	[SerializeField] private ulong 		stepsPerFrame = 1024*32;
	private Texture2D	marmosetLogo = null;
	private Texture2D	skyshopLogo = null;

	private Rect		progressRect = new Rect(0,0,0,0);
	[SerializeField] private bool		uiHideDuringCompute = true;
	[SerializeField] private bool		repaintOften = false;

	[MenuItem("Window/Skyshop")]
	public static void ShowWindow() {
		EditorWindow.GetWindow(typeof(Skyshop));
	}
	[MenuItem ("GameObject/Create Other/Skyshop Sky")]
    public static mset.Sky addSky() {
		GameObject go = new GameObject("Sky");
		go.AddComponent<mset.Sky>();
		go.name = "Sky";
		Selection.activeGameObject = go;
		mset.Sky sky = go.GetComponent<mset.Sky>();
		mset.Util.RegisterCreatedObjectUndo(go, "Add Sky");
		return sky;
	}

	public void OnEnable() {
		//Debug.LogWarning("SkyEditor refreshed " + DateTime.Now);
		//Debug.LogWarning("Version of the runtime: " + Application.unityVersion);

		marmosetLogo = Resources.Load("marmosetLogo") as Texture2D;
		skyshopLogo =  Resources.Load("skyshopLogo") as Texture2D;

		if (inSKY == null) inSKY = mset.CubemapGUI.create(mset.CubemapGUI.Type.INPUT, false);
		if (outSIM == null) outSIM = mset.CubemapGUI.create(mset.CubemapGUI.Type.SIM, false);
		if (outDIM == null) outDIM = mset.CubemapGUI.create(mset.CubemapGUI.Type.DIM, false);
		if (outSKY == null) outSKY = mset.CubemapGUI.create(mset.CubemapGUI.Type.SKY, false);

		//link to input GUI (mostly for probe to send itself to input)
		outSKY.inputGUI = inSKY;
		outDIM.inputGUI = inSKY;
		outSIM.inputGUI = inSKY;

		outSKY.reloadReference();
		outDIM.reloadReference();
		outSIM.reloadReference();

		inSKY.updateBuffers();
		inSKY.updatePreview();
		/*
		outSKY.updateBuffers();
		outDIM.updateBuffers();
		outSIM.updateBuffers();

		outSKY.updatePreview();
		outDIM.updatePreview();
		outSIM.updatePreview();
		*/
		ps.init();
	}
	private void drawMarmosetLogo(float x, float y, float size) {
		if( marmosetLogo != null ) {
			Rect r = new Rect(x,y,size,size);
			UnityEngine.GUI.DrawTexture(r, marmosetLogo, ScaleMode.StretchToFill, true);
		}
	}
	private void drawSkyshopLogo(float x, float y, float size) {
		if( skyshopLogo != null ) {
			Rect r = new Rect(x,y,size,size/2);
			UnityEngine.GUI.DrawTexture(r, skyshopLogo, ScaleMode.StretchToFill, true);
		}
	}

	private void OnInspectorUpdate() {
	}

	private void Update() {
		/* Process convolution right in the update thread, this is faster but requires window focus >_<
		if( ps.isPlaying() ) {
			if( ps.curr == 0 ) { finishSKY(); }
			if( ps.done() ) {
				ps.repaintMetric.end();
				finishConvo();
				Repaint();
				ps.pause();
			} else {
				ps.repaintMetric.end();
				//execute a subset of convolution steps, take a break to repaint the gui, then continue convolution
				stepConvo();
				ps.pendingRepaint = false;
				ps.repaintMetric.begin();
			}
		} else {
			inSKY.update();
			outSKY.update();
			outDIM.update();
			outSIM.update();
			if( mset.Sky.activeSky ) mset.Sky.activeSky.Update();
		}*/
		//if( uiSelectedLight ) this.Repaint();
		if( repaintOften ) Repaint();
	}

	private void OnFocus() {
		//make sure selection is handled properly when we're coming back to skyshop
		OnSelectionChange();
		this.Repaint();
	}
	private void OnProjectChange() {
		mset.BeastConfig.RefreshConfig();
		this.Repaint();
	}

	//for the sake of the apply to selection buttons...
	private void OnSelectionChange() {
		mset.BeastConfig.RefreshConfig();
		uiSelectedSky = null;
		if( Selection.activeGameObject ) {
			uiSelectedSky = Selection.activeGameObject.GetComponent<mset.Sky>();
		}
		if( Selection.GetFiltered(typeof(Light),SelectionMode.Deep).Length > 0 ) {
			repaintOften = true;
		} else {
			repaintOften = false;
		}
		this.Repaint();
    }

	private UnityEngine.Object reObject = null;
	private int reCount = 100;
	private int reCounter = 0;
	public void selectTest() {
		reCount = EditorGUILayout.IntField("Reselection Count",reCount);
		if( GUILayout.Button("Test reselection") ) {
			reCounter = reCount*4;
			reObject = Selection.activeObject;
		}
		if( GUILayout.Button("Stop") ) {
			reCounter = 0;
			Selection.activeObject = reObject;
		}
		if( reCounter > 0 ) {
			if( (reCounter%4) < 2 )	Selection.activeObject = null;
			else 						Selection.activeObject = reObject;
			Rect r = GUILayoutUtility.GetRect(position.width-10,16);
			float progress = 1f - 0.25f*(float)reCounter/(float)reCount;
			EditorGUI.ProgressBar(r, progress, "Test Progress " + (reCounter/4) );
			reCounter--;

			if( reCounter == 0 ) Selection.activeObject = reObject;
			Repaint();
		}
	}

	///
	public void OnGUI() {
	    GUILayout.BeginArea(new Rect(0,0,position.width,position.height));
		uiScroll = EditorGUILayout.BeginScrollView(uiScroll, false, false,GUILayout.MinWidth(300),GUILayout.MaxWidth(position.width) );

		float rightPad = 23;
		float minWidth = 315;
		float sectionWidth = position.width - rightPad;
		float logoSize = 128;

		Rect logoRect = EditorGUILayout.BeginVertical(); {
			GUILayout.Space(32);
			drawSkyshopLogo(sectionWidth - logoSize, logoRect.y, logoSize);
		} EditorGUILayout.EndVertical();

		uiHideDuringCompute = false;//EditorGUILayout.Toggle("Hide UI During Compute",uiHideDuringCompute);
		if( !uiHideDuringCompute || !ps.isPlaying() ) {
			// INPUT REF
			EditorGUILayout.BeginVertical(); {
				inSKY.previewWidth = Mathf.Max(328,(int)sectionWidth-2);
				inSKY.drawGUI();
				outDIM.inputPath = outSIM.inputPath = outSKY.inputPath = inSKY.fullPath;
			} EditorGUILayout.EndVertical();

			// OUTPUT REF
			EditorGUILayout.BeginVertical("HelpBox",GUILayout.Width(sectionWidth), GUILayout.MinWidth(minWidth)); {
				uiRefOptions = EditorGUILayout.Foldout(uiRefOptions,"Output Cubemaps");
				if( uiRefOptions ) {
					outSKY.drawGUI(); EditorGUILayout.Space();
					outDIM.drawGUI(); EditorGUILayout.Space();
					outSIM.drawGUI(); EditorGUILayout.Space();

					EditorGUILayout.BeginHorizontal();{
						string newTip = "Create a new cubemap assets for each output slot and adds them to the project.";
						if (GUILayout.Button(new GUIContent("New All ", newTip), GUILayout.Width(70), GUILayout.Height(18))) {
							outSKY.newCube();
							outDIM.newCube();
							outSIM.newCube();
						}
						string findTip = "Search project for all existing output cubemaps by input panorama name.";
						if (GUILayout.Button(new GUIContent("Find All", findTip), GUILayout.Width(70), GUILayout.Height(18))) {
							outSKY.find();
							outDIM.find();
							outSIM.find();
						}
						string clearTip = "Deselect all target cubemaps from output slots.";
						if (GUILayout.Button(new GUIContent("Clear All", clearTip), GUILayout.Width(80), GUILayout.Height(18))) {
							outSKY.clear();
							outDIM.clear();
							outSIM.clear();
						}
						string reloadTip = "Reload all input and output slot textures and generate preview images for them.";
						EditorGUI.BeginDisabledGroup(outSKY.fullPath.Length == 0 && outDIM.fullPath.Length == 0 && outSIM.fullPath.Length == 0 && inSKY.fullPath.Length == 0);
						{
							if (GUILayout.Button(new GUIContent("Reload All", reloadTip), GUILayout.Width(85), GUILayout.Height(18))) {
								outSKY.reloadReference();
								outDIM.reloadReference();
								outSIM.reloadReference();
								inSKY.reloadReference();
							}
						}
						EditorGUI.EndDisabledGroup();
					} EditorGUILayout.EndHorizontal();

					EditorGUILayout.BeginHorizontal();{

						string editLabel = "";
						string editTip = "";
						mset.Sky editSky = null;

						if( uiSelectedSky ) {
							editSky = uiSelectedSky;
							editLabel = "Edit Selected";
							editTip = "Select cubemaps from the selected Sky as output targets.";
						} else {
							editSky = mset.Sky.activeSky;
							editLabel = "Edit Active";
							editTip = "Select cubemaps from the current viewport Sky as output targets.";
						}
						EditorGUI.BeginDisabledGroup(editSky == null);
						if( GUILayout.Button( new GUIContent(editLabel, editTip), GUILayout.Width(95), GUILayout.Height(32) ) ) {
							if( editSky ) {
								outSKY.HDR = editSky.hdrSky;
								outDIM.HDR = editSky.hdrDiff;
								outSIM.HDR = editSky.hdrSpec;
								outSKY.setReference( AssetDatabase.GetAssetPath(editSky.skyboxCube), false );
								outDIM.setReference( AssetDatabase.GetAssetPath(editSky.diffuseCube), false );
								outSIM.setReference( AssetDatabase.GetAssetPath(editSky.specularCube), true );
							}
						}
						EditorGUI.EndDisabledGroup();

						string applyTip = "Change the selected Sky object to use Skyshop's current output cubemaps.";
						EditorGUI.BeginDisabledGroup(uiSelectedSky == null);
						if( GUILayout.Button( new GUIContent("Apply to Selected", applyTip), GUILayout.Width(120), GUILayout.Height(32) ) ) {
							if( uiSelectedSky ) {
								mset.Util.RegisterUndo(uiSelectedSky, "Apply to Selected Sky");
								uiSelectedSky.diffuseCube =  outDIM.cube;
								uiSelectedSky.specularCube = outSIM.cube;
								uiSelectedSky.skyboxCube = outSKY.cube;
								uiSelectedSky.masterIntensity = 1f;
								uiSelectedSky.skyIntensity = 1f;
								uiSelectedSky.diffIntensity = 1f;
								uiSelectedSky.specIntensity = 1f;
								uiSelectedSky.hdrSky =  outSKY.HDR;
								uiSelectedSky.hdrSpec = outSIM.HDR;
								uiSelectedSky.hdrDiff = outDIM.HDR;

								mset.Sky currSky = mset.Sky.activeSky;
								uiSelectedSky.Apply(); //needed for refreshing exposures and such
								if( currSky ) currSky.Apply();
								SceneView.RepaintAll();
							}
						}
						EditorGUI.EndDisabledGroup();

						string addTip = "Create a new Sky object in the scene and assigns Skyshop's current output cubemaps to it.";
						if( GUILayout.Button( new GUIContent("Add to Scene", addTip), GUILayout.Width(95), GUILayout.Height(32) ) ){
							mset.Sky skyScript = addSky();
							if( skyScript ) {
								skyScript.diffuseCube = outDIM.cube;
								skyScript.specularCube = outSIM.cube;
								skyScript.skyboxCube = outSKY.cube;
								skyScript.masterIntensity = 1f;
								skyScript.skyIntensity = 1f;
								skyScript.diffIntensity = 1f;
								skyScript.specIntensity = 1f;
								skyScript.hdrSky =  outSKY.HDR;
								skyScript.hdrSpec = outSIM.HDR;
								skyScript.hdrDiff = outDIM.HDR;
								skyScript.Apply(); //Add to Scene
								SceneView.RepaintAll();
							}
						}
					}EditorGUILayout.EndHorizontal();
					EditorGUILayout.Space();
				}// end if uiRefOptions
			}EditorGUILayout.EndVertical();

			// BASIC
			string tipExposure = "A multiplier on all the pixels in the Input Panorama during computation. Use for uniform brightness adjustment of results.";
			string tipQuality =  "Changes some advanced options to balance between image quality and computation speed.";

			EditorGUILayout.BeginVertical("HelpBox",GUILayout.Width(sectionWidth), GUILayout.MinWidth(minWidth)); {
				uiBasicOptions = EditorGUILayout.Foldout(uiBasicOptions,"Basic Options");
				if( uiBasicOptions ) {
					float newExposure = EditorGUILayout.FloatField(new GUIContent("Baked Exposure","Baked Exposure -\n"+tipExposure), uiExposure, GUILayout.Width(300));
					newExposure = Mathf.Max(0.0f, newExposure);
					if( newExposure != uiExposure ) {
						mset.Util.RegisterUndo(this,"Change Exposure");
						uiExposure = newExposure;
					}

					mset.Quality newQuality = (mset.Quality)EditorGUILayout.EnumPopup(
						new GUIContent("Quality","Quality -\n"+tipQuality),
						(mset.Quality)uiConvoQuality,
						GUILayout.Width(300)
					);

					if( newQuality != uiConvoQuality ) {
						mset.Util.RegisterUndo(this,"Change Quality");
						uiConvoQuality = newQuality;
						switch(uiConvoQuality) {
							case mset.Quality.ultra: 	uiConvoSize =  64; break;
							case mset.Quality.high: 	uiConvoSize =  32; break;
							case mset.Quality.medium: 	uiConvoSize =  16; break;
							case mset.Quality.low: 		uiConvoSize =  8; break;
						}
					}
					/*
					uiCubeSize = (TexSize)EditorGUILayout.EnumPopup(
						new GUIContent("Output Size (cube)","Output Size -\n"+tipCubeSize),
						(CubeSize)uiCubeSize,
						GUILayout.Width(300)
					);*/

					EditorGUILayout.Space();
				}// end if uiBasicOptions
			}EditorGUILayout.EndVertical();

			// PRO MOVES
			string tipConvoSize = "Resolution the input panorama is downsampled to for convolution, must be power of 2.\n\nWarning: High resolutions can lead to VERY long computation times!";
			string tipMipChain = "If enabled, different specular gloss exponents are computed and stored in each mipmap level of the Specular Output cube.\n\nThis must be enabled for Gloss Maps to function in Marmoset Shaders.";
			string tipExponent = "Gloss exponent used in computing the Specular Output cubemap. Value must be a power of 2, lower values result in a blurrier cubemap. Only available when \"Build Mip Chains\" is disabled.";
			string tipExponents = "Displays a list of the specular gloss exponents used in the various mip levels of the Specular Output cube.";
			string tipResponsiveUI = "Enable if Unity is too unresponsive during computation. Will slow overall computation time.";
			string tipReflection = "Highest gloss level in the specular mip chain is a polished mirror reflection pulled from the input panorama itself.";

			EditorGUILayout.BeginVertical("HelpBox",GUILayout.Width(sectionWidth), GUILayout.MinWidth(minWidth));
			{
				uiAdvancedOptions = EditorGUILayout.Foldout(uiAdvancedOptions,"Advanced Options");
				if( uiAdvancedOptions ) {
					int newConvoSize = EditorGUILayout.IntField(
						new GUIContent("Convolution Size", "Convolution Size -\n" + tipConvoSize),
						uiConvoSize,
						GUILayout.Width(300)
					);

					if( newConvoSize < 2 ) newConvoSize = 2;
					newConvoSize += newConvoSize % 2;

					if( newConvoSize != uiConvoSize ) {
						mset.Util.RegisterUndo(this,"Change Convolution Size");
						uiConvoSize = newConvoSize;
						uiConvoQuality = mset.Quality.custom;
						switch( uiConvoSize ) {
							case 8:  uiConvoQuality = mset.Quality.low; break;
							case 16: uiConvoQuality = mset.Quality.medium; break;
							case 32: uiConvoQuality = mset.Quality.high; break;
							case 64: uiConvoQuality = mset.Quality.ultra; break;
						};
					}
					EditorGUILayout.Space();
					mset.CubemapGUI.drawStaticGUI();

					bool newMipChain = EditorGUILayout.Toggle(new GUIContent("Build Specular Mip Chain","Specular Mip Chains -\n" + tipMipChain),uiMipChain);
					if( newMipChain != uiMipChain ) {
						mset.Util.RegisterUndo(this,"Toggle Specular Mip Chain");
						uiMipChain = newMipChain;
					}
					if(uiMipChain) {
						bool newRefInSIM = EditorGUILayout.Toggle(new GUIContent("Highest Mip is Reflection","Highest Mip is Reflection -\n" + tipReflection),uiReflectionInSIM);
						if( newRefInSIM != uiReflectionInSIM ) {
							mset.Util.RegisterUndo(this,"Toggle Mip Chain Reflection");
							uiReflectionInSIM = newRefInSIM;
						}
						EditorGUI.BeginDisabledGroup(true);
						string mipString;
						if( uiReflectionInSIM ) mipString = "mirror, 128, 64, 32, 16...";
						else 					mipString = "256, 128, 64, 32, 16, 8...";
						EditorGUILayout.TextField(new GUIContent("Specular Exponents","Specular Exponents -\n"+tipExponents), mipString, GUILayout.Width(300));
						EditorGUI.EndDisabledGroup();
					} else {
						EditorGUI.BeginDisabledGroup(true);
						EditorGUILayout.Toggle(new GUIContent("Highest Mip is Reflection","Highest Mip is Reflection -\n" + tipReflection),false);
						EditorGUI.EndDisabledGroup();
						int newExponent = EditorGUILayout.IntField(
							new GUIContent("Specular Exponent","Specular Exponent -\n" + tipExponent),
							uiExponent,
							GUILayout.Width(300)
						);
						newExponent = Mathf.Max(1, newExponent);
						if( newExponent != uiExponent ) {
							mset.Util.RegisterUndo(this, "Change Specular Exponent");
							uiExponent = newExponent;
						}
					}
					EditorGUILayout.Space();

					//TODO: Will anyone ever want this? Marmoset shaders need gamma compression as does sRGB sampling.
					uiGammaCompress = true;
					/*
					uiGammaCompress = EditorGUILayout.Toggle(new GUIContent(
						"Gamma-Compress RGBM",
						"Gamma-Compress RGBM -\nIf enabled, a gamma of 1/2.2 is applied to HDR data before it is encoded as RGBM. This adds dynamic range but also shader complexity. Leave enabled for Marmoset shaders."),
						uiGammaCompress);
					*/

					uiResponsiveUI = stepsPerFrame <= 1024*16;
					uiResponsiveUI = EditorGUILayout.Toggle(new GUIContent("Keep UI Responsive","Keep UI Responsive -\n"+tipResponsiveUI), uiResponsiveUI);
					ulong newStepsPerFrame = stepsPerFrame;
					if( uiResponsiveUI ) newStepsPerFrame = 1024*16;
					else 				 newStepsPerFrame = 1024*256;

					if( newStepsPerFrame != stepsPerFrame ) {
						mset.Util.RegisterUndo(this,"Toggle Responsive UI");
						stepsPerFrame = newStepsPerFrame;
					}
					EditorGUILayout.Space();


					if( GUILayout.Button("Reset to Default", GUILayout.Width(120)) ) {
						uiConvoSize = Mathf.Min(uiConvoSize, 16);
						uiMipChain = true;
						uiReflectionInSIM = true;
						uiResponsiveUI = false;
						uiGammaCompress = true;
					}
					EditorGUILayout.Space();

				}
			}EditorGUILayout.EndVertical();

			EditorGUILayout.Space();
			EditorGUILayout.Space();
		}

		//GENERATE
		bool generate = false;
		bool cancel = false;
		EditorGUILayout.BeginHorizontal(); {
			if( ps.isPlaying() ) {
				cancel = GUILayout.Button("Abort", GUILayout.Width(130), GUILayout.Height(50));
			} else {
				bool valid = true;
				if( inSKY.input == null ) {
					valid = false;
				}
				if( outSKY.cube == null && outDIM.cube == null && outSIM.cube == null ) {
					valid = false;
				}
				EditorGUI.BeginDisabledGroup(!valid);
					generate = GUILayout.Button("Compute", GUILayout.Width(130), GUILayout.Height(50));
				EditorGUI.EndDisabledGroup();

				if( !valid ) {
					EditorGUILayout.BeginVertical();
					if( inSKY.input == null ) {
						EditorGUILayout.HelpBox("Input Panorama is missing.", MessageType.Error);
					}
					if( outSKY.cube == null && outDIM.cube == null && outSIM.cube == null ) {
						EditorGUILayout.HelpBox("An Output Cubemap is needed.", MessageType.Error);
					}
					EditorGUILayout.EndVertical();
				}
			}
		}EditorGUILayout.EndHorizontal();
		if( cancel ) {
			ps.pause();
			Repaint();

			inSKY.locked =
			outSKY.locked =
			outDIM.locked =
			outSIM.locked = false;
			inSKY.updateBuffers();
		}

		progressRect = GUILayoutUtility.GetRect(sectionWidth - 4,16);
		progressRect.width = sectionWidth;
		progressRect.x = 4;
		progressRect.y += 2;

		//uiShowPreview = true;
		if( uiShowPreview ) {
			EditorGUILayout.LabelField("Convolution Preview");
			float previewWidth = position.width - rightPad;
			float previewHeight = previewWidth*0.5f;
			mset.Util.GUILayout.drawTexture( 4, 0, previewWidth, previewHeight, "", uiConvoPreview, false);
		}

		if( generate ) {
			startConvo();
			ps.repaintMetric.begin();
		}

		if( ps.isPlaying() ) {
			if( ps.curr == 0 ) { finishSKY(); }
			if( ps.done() ) {
				ps.repaintMetric.end();
				finishConvo();
				Repaint();
				ps.pause();
			} else {
				ps.repaintMetric.end();
				//execute a subset of convolution steps, take a break to repaint the gui, then continue convolution
				stepConvo();
				EditorGUI.ProgressBar(progressRect, ps.progress(), "Convolution Progress " + Mathf.Floor(100f * ps.progress()) + "%");
				Repaint();
				ps.pendingRepaint = false;
				ps.repaintMetric.begin();
			}
		}

		if( ps.isPlaying() && !ps.done() ) {
			EditorGUI.ProgressBar(progressRect, ps.progress(), "Convolution Progress " + Mathf.Floor(100f * ps.progress()) + "%");
		}

		//DEBUG OPTIONS
		/*
		EditorGUILayout.BeginVertical("HelpBox", GUILayout.Width(sectionWidth), GUILayout.MinWidth(minWidth)); {
			uiPerfReport = EditorGUILayout.Foldout(uiPerfReport,"Debug");
			if( uiPerfReport ) {
				uiShowPreview = EditorGUILayout.Toggle("Show Preview", uiShowPreview, GUILayout.Height(16));
				EditorGUILayout.Space();

				string report = "Performance Report\n";
				report += ps.totalMetric.getString("Total",0);
				report += ps.initMetric.getString("Init",1);
				report += ps.blockMetric.getString("Coroutine Step",1);
				report += ps.passWriteMetric.getString("Cube Write",2);
				report += ps.repaintMetric.getString("Repaint", 1);
				report += ps.finishMetric.getString("Finalize", 1);
				EditorGUILayout.SelectableLabel(report, "HelpBox", GUILayout.Height(360));
				EditorGUILayout.Space();
				selectTest();
			}
		}EditorGUILayout.EndVertical();
		*/

		EditorGUILayout.BeginVertical("HelpBox",GUILayout.Width(sectionWidth), GUILayout.MinWidth(minWidth));
		{
			uiGIOptions = EditorGUILayout.Foldout(uiGIOptions,"Beast Global Illum Options");
			if( uiGIOptions ) {
				mset.BeastConfig.DrawGUI();
				EditorGUILayout.Space();
			}
		}EditorGUILayout.EndVertical();


		EditorGUILayout.EndScrollView();
		GUILayout.EndArea();

		//GUIUtility.ExitGUI();
	}

	private void startConvo() {
		ps.initMetric.reset();
		ps.totalMetric.reset();
		ps.totalMetric.begin();
		ps.initMetric.begin();

		int convSize = uiConvoSize;
		int cubeSize = 1<<(int)uiCubeSize;

		int outExponentCount = 0;
		if(outDIM.cube) outExponentCount++;
		if(outSIM.cube) {
			if(uiMipChain)	outExponentCount += (int)uiCubeSize;
			else 			outExponentCount++;
		}

		outMipSizes =  new int[outExponentCount];
		outExponents = new int[outExponentCount];

		if(outDIM.cube) {
			outMipSizes[outExponentCount-1] = cubeSize;
			outExponents[outExponentCount-1] = 1;
		}
		if(outSIM.cube) {
			if( uiMipChain ) {
				int maxExp = 256;
				int maxSize = cubeSize;
				//SIM exponents
				for( int i = 0; i<outExponentCount-1; ++i ) {
					outExponents[i] = maxExp >> i;
					outMipSizes[i] = maxSize >> i;
				}
			} else {
				outMipSizes[0] = cubeSize;
				outExponents[0] = uiExponent;
			}
		}
		ps.reset();
		ps.exponentCount = (ulong)outExponentCount;
		ps.gammaCompress = uiGammaCompress;

		if(inSKY.input) {
			this.uiShowPreview = false;
			if(outSIM.cube) {
				ps.buildMipChain = uiMipChain;
				ps.reflectionInSIM = uiReflectionInSIM && uiMipChain;
			} else {
				ps.buildMipChain = false;
				ps.reflectionInSIM = false;
			}

			//NOTE: ps.IN is now a direct reference to the input sky. This is ok because we lock the CubemapGUIs
			//ps.IN.filterMode = CubeBuffer.FilterMode.BILINEAR;
			ps.IN = inSKY.buffers[0];
			ps.IN.applyExposure(uiExposure);

			ps.CONVO.resize(convSize);
			ps.IN.resampleToBuffer(ref ps.CONVO);
			ps.CONVO.filterMode = mset.CubemapGUI.sFilterMode;

			gatherLights( ref ps.CONVO );
			ulong opcount = ps.lightCount * ps.lightCount * ps.exponentCount;
			ps.setOpCount(stepsPerFrame,opcount);

			inSKY.locked =
			outSKY.locked =
			outDIM.locked =
			outSIM.locked = true;

			ps.play();
		}
		ps.initMetric.end();
	}

	private void gatherLights(ref mset.CubeBuffer cube) {
		ps.lights = new Color[cube.pixels.Length];
		mset.CubeBuffer.pixelCopy(ref ps.lights, 0, cube.pixels, 0, cube.pixels.Length);

		ps.lightCount = (ulong)ps.lights.LongLength;
		ulong faceSize = (ulong)cube.faceSize;
		ps.lightDirs = new mset.DirLookup[ps.lightCount];
		Vector3 vec = new Vector3();

		for( ulong face = 0; face<6; ++face )
		for( ulong y=0; y<faceSize; ++y )
		for( ulong x=0; x<faceSize; ++x ) {
			ulong i = face*faceSize*faceSize + y*faceSize + x;
			mset.Util.invCubeLookup(ref vec, ref ps.lightDirs[i].weight, face, x, y, faceSize);
			ps.lights[i] *= ps.lightDirs[i].weight;
			ps.lightDirs[i].weight = 1f;
			ps.lightDirs[i].x = vec.x;
			ps.lightDirs[i].y = vec.y;
			ps.lightDirs[i].z = vec.z;
		}
	}

	// steps convolution like a coroutine
	// returns true if more pixels are to be processed on IN, false if last pixel was processed
	private void stepConvo() {
		ps.blockMetric.begin();

		ulong isize = ps.lightCount;
		ulong osize = ps.lightCount;
		float invLightCount = 1f/(float)ps.lightCount;

		ulong passSize = isize*osize;
		ulong endOfPass = passSize-1;

		//re-initialize iterator state
		ulong expPass = ps.curr / passSize;
		ulong ixyoxy =  ps.curr % passSize;
		ulong ixy = ixyoxy / osize;
		ulong oxy = ixyoxy % osize;
		expPass = ps.exponentCount - expPass - 1;

		/*
		ulong passItr = ps.curr / passSize;					//iterates over every exponent pass
		ulong inoutItr = ps.curr % passSize; 				//iterates over every in-out pairing
		ulong inItr =  inoutItr / osize;					//iterates over every input pixel
		ulong outItr = inoutItr % osize;					//iterates over every output pixel
		passItr = (ulong)outExponentCount - passItr - 1;	//flip pass iterators to do high exponents last
		*/
		// iterate over all 5 nested for loops of expPass { iy { ix { oy { ox } } } } linearly and extract pixel coordinates from i
		for( ; ps.curr<ps.total; ) {
			ixyoxy = ps.curr % passSize; // needed every frame to check for end of pass
			oxy = ps.curr % osize;

			if( oxy == 0 ) { // next input pixel
				ixy = ixyoxy / osize;
				if( ixyoxy == 0 ) {	//start an exponent pass
					expPass = ps.curr / passSize;
					expPass = ps.exponentCount - expPass - 1;

					//if we're putting the reflection into the highest mip level, skip the first exponent pass and do this instead
					if( expPass == 0 && ps.reflectionInSIM ) {
						uploadReflection(0,outMipSizes[0]);
						ps.skip(ps.lightCount * ps.lightCount);
						if( ps.needsRepaint() ) break;
						else continue;
					}
					ps.exponent = outExponents[expPass];
					ps.exponentFunc = mset.QPow.closestPowFunc(ps.exponent);
					ps.exposure = specNormalizer(ps.exponent) * invLightCount;

					mset.Util.clearTo(ref ps.CONVO.pixels, Color.black);
				}
				ps.inLookup = ps.lightDirs[ixy];
				ps.LColor = ps.lights[ixy];
			}
			//NOTE: this assumes input and output dimensions match!
			ps.outLookup = ps.lightDirs[oxy];
			float dp =	ps.inLookup.x*ps.outLookup.x +
						ps.inLookup.y*ps.outLookup.y +
						ps.inLookup.z*ps.outLookup.z;

			if( dp > 0f ) {
				dp = ps.exponentFunc(dp);
				//compute two pixels with the same dot product
				//NOTE: this only works if input dimensions match output dimensions!
				ps.CONVO.pixels[oxy] += ps.exposure * dp * ps.LColor;
				if( oxy != ixy ) {
					ps.CONVO.pixels[ixy] += ps.exposure * dp * ps.lights[oxy];
				}
			}

			if( ixyoxy == endOfPass ) { //end of exponent pass
				ps.passWriteMetric.begin();
				if( uiShowPreview ) {
					uiConvoPreview.SetPixels(ps.CONVO.pixels);
					uiConvoPreview.Apply();
				}
				if( ps.exponent == 1 ) {
					// DIM
					finishDIM();
				} else {
					// SIM
					if( ps.buildMipChain ) {
						int mipSize = outMipSizes[expPass];
						uploadMipLevel((int)expPass,mipSize);
					} else {
						finishSIM();
					}
				}
				ps.passWriteMetric.end();
			}
			// modified j=i for loop. Since we compute two pixels with the same dot product above,
			// we can skip any repeating permutations of input/output pixels.
			if( oxy >= ixy ) ps.skip(osize - oxy);
			else 			 ps.next();

			if(ps.needsRepaint()) {
				break;
			}
		}
		ps.blockMetric.end();
	}

	//returns whether or not an output cubemap should be generated/encoded with a gamma-curve
	private bool needsGamma( ref mset.CubemapGUI cubegui ) {
		if( cubegui.HDR ) return ps.gammaCompress;	//toggle gamma curve by whether we want RGBM gamma compression on or off
		return true;								//LDR data should always be saved as sRGB
	}
	private bool needsLinearSampling( ref mset.CubemapGUI cubegui ) {
		if( cubegui.HDR ) return !ps.gammaCompress;	//non-gamma compression requires sRGB sampling bypassed
		return false;
	}

	private void uploadReflection(int mip, int mipSize) {
		ps.IN.resampleToCube(ref outSIM.cube, mip, outSIM.colorMode, needsGamma(ref outSIM));
	}

	private void uploadMipLevel(int mip, int mipSize) {
		ps.CONVO.resampleToCube(ref outSIM.cube, mip, outSIM.colorMode, needsGamma(ref outSIM));
	}

	private void finishSKY() {
		if( outSKY.cube && inSKY.input != outSKY.cube ) {
			ps.IN.resampleToCube(ref outSKY.cube, 0, outSKY.colorMode, needsGamma(ref outSKY));
			outSKY.cube.Apply(true);
			outSKY.locked = false;
			outSKY.setLinear(needsLinearSampling(ref outSKY));
			outSKY.setReference( AssetDatabase.GetAssetPath(outSKY.cube), false );
		}
	}

	private void finishDIM() {
		string cubePath = AssetDatabase.GetAssetPath(outDIM.cube);
		ps.CONVO.resampleToCube(ref outDIM.cube, 0, outDIM.colorMode, needsGamma(ref outDIM));
		outDIM.locked = false;
		outDIM.setReference(cubePath,false);
		outDIM.setLinear(needsLinearSampling(ref outDIM));
		outDIM.cube.Apply(true);
	}

	private void finishSIM() {
		ps.finishMetric.begin();
		string cubePath = AssetDatabase.GetAssetPath(outSIM.cube);

		//clear existing mips
		UnityEngine.Object[] mips = AssetDatabase.LoadAllAssetRepresentationsAtPath(cubePath);
		for(int i=0; i<mips.Length; ++i) {
			if(AssetDatabase.IsSubAsset(mips[i])) {
				UnityEngine.Object.DestroyImmediate(mips[i],true);
			}
		}
		if( !ps.buildMipChain ){
			ps.CONVO.resampleToCube(ref outSIM.cube, 0, outSIM.colorMode, needsGamma(ref outSIM));
			outSIM.cube.Apply(true);
		}

		AssetDatabase.Refresh();

		if( ps.buildMipChain ) {
			AssetDatabase.StartAssetEditing();
			int faceSize = outSIM.cube.width;
			// skip mip level 0, its in the cubemap itself
			faceSize = faceSize >> 1;
			for( int mip = 1; faceSize > 0; ++mip ) {
				// extract mipmap faces from a cubemap and add them as textures in the sub image
				Texture2D tex = new Texture2D(faceSize, faceSize*6,TextureFormat.ARGB32,false);
				tex.name = "mip"+mip;
				for( int face = 0; face<6; ++face ) {
					tex.SetPixels(0, face*faceSize, faceSize, faceSize, outSIM.cube.GetPixels((CubemapFace)face,mip));
				}
				tex.Apply();
				AssetDatabase.AddObjectToAsset(tex, cubePath);
				AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(tex));
				faceSize = faceSize >> 1;
			}
			AssetDatabase.StopAssetEditing();

			outSIM.cube.filterMode = FilterMode.Trilinear;
			outSIM.cube.Apply(false);
		}
		outSIM.locked = false;
		outSIM.setLinear(needsLinearSampling(ref outSIM));
		outSIM.setReference(cubePath,true);
		ps.finishMetric.end();
	}

	private void finishConvo() {
		if( ps.buildMipChain ) {
			finishSIM();
		}
		AssetDatabase.Refresh();

		ps.finishMetric.end();
		ps.totalMetric.end();
		SceneView.RepaintAll();

		ps.CONVO.clear();
		ps.reset();

		inSKY.locked =
		outSKY.locked =
		outDIM.locked =
		outSIM.locked = false;
		inSKY.updateBuffers();

		mset.SkyInspector.forceRefresh();

		System.GC.Collect();
	}

	public static float specNormalizer( int exponent ) {
		// divide specular light by integral of the exponent for 50% more win
		//http://www.farbrausch.de/~fg/articles/phong.pdf
		return (exponent + 2);
		//pre-106 normalization:
		//return Mathf.PI * (exponent + 2);
	}
};