// Marmoset Skyshop
// Copyright 2013 Marmoset LLC
// http://marmoset.co

using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace mset {
class ExportSplatmap : ScriptableWizard {
	public Terrain terrain = null;
	public bool flipVertical = true;
	public bool flipLayerOrder = false;
	public bool saveSeparateAlpha = false;
	 
	void OnWizardUpdate() {
		this.helpString = 	"This tool exports the splatmap (layer weight texture) of a terrain to a PNG image.\n"+
							"Optionally, the alpha channel is saved to a separate image.\n";
        if( Selection.activeGameObject != null ) this.terrain = Selection.activeGameObject.GetComponent<Terrain>();
		if(this.terrain == null) this.terrain = GameObject.FindObjectOfType(typeof(Terrain)) as Terrain;
		this.isValid = this.terrain != null && this.terrain.terrainData != null;
	}
	
	void OnWizardCreate() {
		TerrainData terrainData = this.terrain.terrainData;
		if(terrainData.alphamapLayers == 0 ) {
			EditorUtility.DisplayDialog("No Terrain Layers Found", "The selected terrain has no texture layers, at least one is required.", "Ok"); 
			return;
		}
		try {		
			int aw = terrainData.alphamapWidth;
			int ah = terrainData.alphamapHeight;
			int acount = terrainData.alphamapLayers;
			
			Texture2D tex, alpha; 			
			if( saveSeparateAlpha ){
				tex = new Texture2D(aw,ah,TextureFormat.RGB24,false);
				alpha = new Texture2D(aw,ah,TextureFormat.RGB24,false);
			} else {
				tex = new Texture2D(aw,ah,TextureFormat.ARGB32,false);
				alpha = null;
			}
			
			float[,,] map = terrainData.GetAlphamaps(0,0,aw,ah);
			for(int y=0; y<ah; ++y)
			for(int x=0; x<aw; ++x) {
				Color color = Color.black;
				if( flipLayerOrder ) {
					color.a = map[y,x,0];
					if(acount>1) color.b = map[y,x,1];
					if(acount>2) color.g = map[y,x,2];
					if(acount>3) color.r = map[y,x,3];
				} else {
					color.r = map[y,x,0];
					if(acount>1) color.g = map[y,x,1];
					if(acount>2) color.b = map[y,x,2];
					if(acount>3) color.a = map[y,x,3];
				}
				//normalize
				float sum = color.r + color.g + color.b + color.a;				
				if(sum > 0f) {
					sum = 1f/sum;
					color.r *= sum;
					color.g *= sum;
					color.b *= sum;
					color.a *= sum;
				}
				
				//v is flipped in the coordinate system but needs to be flipped w/ regard to photoshop also. Double-flip!
				if(!flipVertical)	tex.SetPixel(x,ah-y-1,color);
				else 				tex.SetPixel(x,y,color);
				
				if( saveSeparateAlpha ) {
				 	color.r = color.a;
					color.g = color.a;
					color.b = color.a;
					if(!flipVertical)	alpha.SetPixel(x,ah-y-1,color);
					else 				alpha.SetPixel(x,y,color);
				}
			}
			tex.Apply();
			if( saveSeparateAlpha ) alpha.Apply();
			
			string pngPath = EditorUtility.SaveFilePanel("Save Splatmap As", "", "untitled", "png");
			//save PNG
			try {
				FileStream fs = new FileStream(pngPath, FileMode.Create);
			    BinaryWriter bw = new BinaryWriter(fs);
			    bw.Write(tex.EncodeToPNG());
			    bw.Close();
			    fs.Close();		
			} catch(Exception e) {
				Debug.LogError("FileStream exception: " + e.ToString());
			}
			
			if( saveSeparateAlpha ) {
				pngPath = EditorUtility.SaveFilePanel("Save Alpha Channel As", "", "untitled", "png");
				//save PNG
				try {
					FileStream fs = new FileStream(pngPath, FileMode.Create);
				    BinaryWriter bw = new BinaryWriter(fs);
				    bw.Write(alpha.EncodeToPNG());
				    bw.Close();
				    fs.Close();		
				} catch(Exception e) {
					Debug.LogError("FileStream exception: " + e.ToString());
				}
			}
			
		} catch(Exception e) {
			EditorUtility.DisplayDialog("Error", "Failed to write splatmap to PNG file.\n"+e.ToString(), "Ok"); 
			return;
		}
	}
	
	[MenuItem("Edit/Terrain/Export Splatmap...")]
	static void Replace() {
		ScriptableWizard.DisplayWizard("Export Splatmap", typeof(ExportSplatmap), "Export");
	}
}
}