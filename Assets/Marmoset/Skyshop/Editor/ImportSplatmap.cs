// Marmoset Skyshop
// Copyright 2013 Marmoset LLC
// http://marmoset.co
//
// Credit where credit is due: This script is loosely based on the World Machine splatmap importer by Stephen Schmitt.
// http://www.world-machine.com/learn.php?page=workflow&workflow=wfunity

using UnityEngine;
using UnityEditor;
using System;
using System.IO;


namespace mset {
class ImportSplatmap : ScriptableWizard {
	public Texture2D newSplatmap = null;
	public Terrain terrain = null;
	public bool flipVertical = true;
	public bool flipLayerOrder = false;
	 
	void OnWizardUpdate() {
		this.helpString =	"This tool replaces the selected terrain's splatmap (layer weight texture) with another image.\n\n" +
							"The texture format must be uncompressed (set to \"Truecolor\" in inspector).\n\n" +
							"This tool cannot add or remove texture layers. The terrain must have all its layers defined\n" + 
							"before importing. If only 2 layers are present, just the red and green channels are imported.\n";
		
        if( Selection.activeGameObject != null ) this.terrain = Selection.activeGameObject.GetComponent<Terrain>();
		if(this.terrain == null) this.terrain = GameObject.FindObjectOfType(typeof(Terrain)) as Terrain;
		this.isValid = this.newSplatmap != null && this.terrain != null && this.terrain.terrainData != null;		
	}
	
	void OnWizardCreate() {
		if(newSplatmap.format != TextureFormat.ARGB32) {
			EditorUtility.DisplayDialog("Invalid Format", "Splatmap must be converted to ARGB 32-bit format.\nMake sure the texture format is \"Not Compressed\" or set the format manually using the \"Advanced\" texture type.", "Ok"); 
			return;
		}
		TerrainData terrainData = this.terrain.terrainData;
		if(terrainData.alphamapLayers == 0 ) {
			EditorUtility.DisplayDialog("No Terrain Layers Found", "The selected terrain has no texture layers, at least one is required.", "Ok"); 
			return;
		}
		try {			
			SerializedObject serialSplat = new SerializedObject(newSplatmap);
			mset.Util.setReadable(ref serialSplat, true );
			int aw = terrainData.alphamapWidth;
			int ah = terrainData.alphamapHeight;
			int acount = terrainData.alphamapLayers;
			
			float[,,] map = terrainData.GetAlphamaps(0,0,aw,ah);
			float u, v;
			for(int y=0; y<ah; ++y)
			for(int x=0; x<aw; ++x) {
				//Terrain u and v are swapped with respect to how heightmaps are loaded, thanks row col notation.
				v = ((float)x+0.5f) / (float)aw;
				u = ((float)y+0.5f) / (float)ah;
				
				//If the splatmap needs to be flipped w/ respect to the heightmap in photoshop, check "Flip Vertical" (irregardless of any base normal or diffuse textures).
				//NOTE: Since the terrain coordinate system also defines v = 1-y, flipVertical is a double-flip.
				if(!flipVertical) v = 1f - v;
				Color color = newSplatmap.GetPixelBilinear(u,v);
				
				//normalize
				float sum = color.r + color.g + color.b + color.a;				
				if(sum > 0f) {
					sum = 1f/sum;
					color.r *= sum;
					color.g *= sum;
					color.b *= sum;
					color.a *= sum;
				}
				if(flipLayerOrder) {
					map[x,y,0] = color.a;
					if(acount>1)map[x,y,1] = color.b;
					if(acount>2)map[x,y,2] = color.g;
					if(acount>3)map[x,y,3] = color.r;
				} else {
					map[x,y,0] = color.r;
					if(acount>1)map[x,y,1] = color.g;
					if(acount>2)map[x,y,2] = color.b;
					if(acount>3)map[x,y,3] = color.a;
				}
			}
			terrainData.SetAlphamaps(0,0,map);	
			if(terrain) terrain.Flush();
		} catch(Exception e) {
			EditorUtility.DisplayDialog("Error", "Failed to write terrain data with new splatmap.\n"+e.ToString(), "Ok"); 
			return;
		}
	}
	
	[MenuItem("Edit/Terrain/Import Splatmap...")]
	static void Replace() {
		ScriptableWizard.DisplayWizard("Import Splatmap", typeof(ImportSplatmap), "Import");
	}
}
}