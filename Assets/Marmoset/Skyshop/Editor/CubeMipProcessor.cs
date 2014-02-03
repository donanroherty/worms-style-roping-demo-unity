// Marmoset Skyshop
// Copyright 2013 Marmoset LLC
// http://marmoset.co

using UnityEngine;
using UnityEditor;
using System;
using System.IO;
		
namespace mset {
	//This class will be run after every cubemap is imported and will look for mip level textures as sub-assets of the cubemap.
	//If mip textures are found in the proper dimensions, their contents are uploaded to the cubemap. Mip textures are expected
	//to come in vertical column format, all faces in a row, 1:6 aspect ratio.
	public class CubeMipProcessor : AssetPostprocessor {
		public static void OnPostprocessAllAssets ( string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
	        for (int a=0; a<importedAssets.Length; ++a) {
	            string path = importedAssets[a];
				string ext = Path.GetExtension(path).ToLowerInvariant();
				
				if( ext == ".cubemap" ) {
					//load all sub-assets
					UnityEngine.Object[] mips = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
					
					// ignore cubemaps with less than two sub-objects, these are likely regular cubemaps
					if( mips.Length < 2 ) {
						continue;
					}
					Cubemap cube = AssetDatabase.LoadAssetAtPath(path,typeof(Cubemap)) as Cubemap;
					SerializedObject srCube = new SerializedObject(cube);
					
					// if the cubemap is not configured to be mipmapped, skip loading
					//NOTE: changing serialized properties inside post-processors crashes unity so there's little we can do
					if( !mset.Util.isMipmapped(srCube) ) continue;
					cube.Apply(true);
					
					for( int i=0; i<mips.Length; ++i ) {
						if( mips[i].GetType() != typeof(Texture2D) ) {
							Debug.LogWarning("Non-texture found, ignoring.");
							continue;
						}
						Texture2D tex = (mips[i]) as Texture2D;
						if( tex.width*6 != tex.height ) {
							Debug.LogWarning("Mip texture \'" + tex.name + "\' with wrong aspect ratio (must be 1x6) found in cubemap \'" + path + "\', ignoring.");
							continue;
						}
						//pick mip level
						int mip = 0;
						int mipSize = cube.width;
						while( mipSize!=tex.width && mipSize > 0 ) {
							mipSize=mipSize>>1;
							mip++;
						};
						if( mipSize == 0 ) {
							Debug.LogWarning("Mip texture \'" + tex.name + "\' with wrong size found in cubemap \'" + path + "\', ignoring.");
							continue;
						} else if( mipSize == 1 ) {
							//skip 1x1 mip, it will have problems and a better one was generated with cube.Apply
							continue;
						}
						
						for( int face=0; face<6; ++face ) {
							cube.SetPixels(tex.GetPixels(0,mipSize*face,mipSize,mipSize), (CubemapFace)face, mip);
						}
					}
					cube.Apply(false);
				}
			}
	    }
	};
}