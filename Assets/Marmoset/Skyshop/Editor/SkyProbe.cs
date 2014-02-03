// Marmoset Skyshop
// Copyright 2013 Marmoset LLC
// http://marmoset.co

using UnityEngine;
using UnityEditor;
using System;
using System.IO;
	
namespace mset {
	public class SkyProbe {
		public Cubemap cube = null;
		
		public void capture(Transform at, bool HDR) { capture(ref cube, at, HDR); }
		public void capture(ref Cubemap targetCube, Transform at, bool HDR) {
			if( targetCube == null ) return;
			GameObject go = new GameObject("_temp_probe");
			go.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideAndDontSave;
			go.SetActive(true);			
			Camera cam = go.AddComponent<Camera>();
			if( at != null ) {
				go.transform.position = at.position;
			}
			if(HDR) {
				Shader.EnableKeyword("MARMO_RGBM");
				Shader.DisableKeyword("MARMO_RGBA");
				Shader.SetGlobalFloat("_GlowStrength", 0f);
				Shader.SetGlobalFloat("_EmissionLM", 0f);
				cam.SetReplacementShader(Shader.Find("Hidden/Marmoset/RGBM Replacement"),"RenderType");
			}
			cam.RenderToCubemap(targetCube);
			targetCube.Apply(false);
			if(HDR) {
				cam.ResetReplacementShader();
				Shader.DisableKeyword("MARMO_RGBM");
				Shader.EnableKeyword("MARMO_RGBA");
			}
			GameObject.DestroyImmediate(go);
		}
	};
}