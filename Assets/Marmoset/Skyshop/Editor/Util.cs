// Marmoset Skyshop
// Copyright 2013 Marmoset LLC
// http://marmoset.co

using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace mset {
	public enum TexSize {
		_16 = 4,
		_32 = 5,
		_64 = 6,
		_128 = 7,
		_256 = 8,
		_512 = 9,
		_1024 = 10
	};
	public enum Quality {
		ultra,
		high,
		medium,
		low,
		custom
	};
	
	public class Util {
		public static void RegisterUndo(UnityEngine.Object obj, string name) {
			#if UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2
				Undo.RegisterUndo(obj, name);
			#else
				Undo.RecordObject(obj, name);
			#endif
		}
		public static void RegisterUndo(UnityEngine.Object[] objs, string name) {
			#if UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2
				Undo.RegisterUndo(objs, name);
			#else
				Undo.RecordObjects(objs, name);	
			#endif
		}
		public static void RegisterCreatedObjectUndo(UnityEngine.Object obj, string name) {
			#if UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2
				Undo.RegisterCreatedObjectUndo(obj, name);
			#else
				Undo.RegisterCreatedObjectUndo(obj, name);
			#endif
		}
		
		public static TextureImporter getTextureImporter(String path) { return getTextureImporter("getTextureImporter"); }
		public static TextureImporter getTextureImporter(String path, String errorLabel) {
			if( path.Length == 0 ) {
				Debug.LogError(errorLabel + " needs an asset path (empty string provided).");
				return null;
			}
			AssetImporter ai = AssetImporter.GetAtPath(path);
			if(ai == null) {
				Debug.LogError(errorLabel + " failed to fetch any asset importer for path '" + path + "'.");
				return null;
			}
			TextureImporter ti = ai as TextureImporter;
			if(ti == null) {
				Debug.LogError(errorLabel + " failed to cast AssetImporter of type " + ai.GetType() + " to TextureImporter at path '" + path + "'.");
				return null;
			}
			return ti;
		}
		/*
		public static bool makeReadable( ref Texture tex ) {
			string path = AssetDatabase.GetAssetPath(tex);
			TextureImporter ti = getTextureImporter(path,"generic mset.Util.makeReadable");
			if( ti == null ) return false;
			ti.isReadable = true;
			AssetDatabase.ImportAsset(path);
			return true;
		}
		
		public static bool makeCubeReadable( ref Cubemap cube ) {
			string path = AssetDatabase.GetAssetPath(cube);
			TextureImporter ti = getTextureImporter(path, "mset.Util.makeCubeReadable");
			if( ti == null ) return false;
			ti.isReadable = true;
			AssetDatabase.ImportAsset(path);
			cube = (Cubemap)AssetDatabase.LoadAssetAtPath(path,typeof(Cubemap));
			return true;
		}
		public static bool makeCubeFrom2D( String path, bool mipmap ) {
			return makeCubeFrom2D(path,mipmap,-1);
		}
		public static bool makeCubeFrom2D( String path, bool mipmap, int maxSize ) {
			TextureImporter ti = getTextureImporter(path, "mset.Util.makeCubeFrom2D");
			ti.isReadable = true;
			if( maxSize > 0 ) ti.maxTextureSize = maxSize;
			if( ti.generateCubemap != TextureImporterGenerateCubemap.Cylindrical ) {
				ti.generateCubemap = TextureImporterGenerateCubemap.Cylindrical;
				ti.mipmapEnabled = mipmap;
				AssetDatabase.ImportAsset(path);
				AssetDatabase.Refresh();
			}
			return true;
		}
		public static void resampleTexture2D( ref Texture2D srcdst, int width, int height ) { resampleTexture2D(ref srcdst, srcdst, width, height); }
		public static void resampleTexture2D( ref Texture2D dst, Texture2D src ) 			{ resampleTexture2D(ref dst, src, dst.width, dst.height); }
		public static void resampleTexture2D( ref Texture2D dst, Texture2D src, int width, int height ) {
			Color[] c = new Color[width*height];
			
			float ow = (float)width;
			float oh = (float)height;
			float ih = (float)src.height;
			
			for( int y = 0; y < height; ++y )
			for( int x = 0; x < width; ++x ) {
				float u = (float)x/ow;
				float v = (float)y/oh;
				v = Mathf.Min(v,(ih-1f)/ih);
				c[y*width+x] = src.GetPixelBilinear(u,v);
			}
			
			dst.Resize(width,height);
			dst.SetPixels(c);
		}
		public static Texture2D cloneTexture2D( Texture2D src ) {
			string srcpath = AssetDatabase.GetAssetPath(src);
			string dstpath = AssetDatabase.GenerateUniqueAssetPath(srcpath);
			AssetDatabase.CopyAsset(srcpath,dstpath);
			AssetDatabase.Refresh();
			return (Texture2D)AssetDatabase.LoadAssetAtPath(dstpath,typeof(Texture2D));
		}
		public static void resizeTexture2D( ref Texture2D tex, int maxSize ) {
			string path = AssetDatabase.GetAssetPath(tex);
			TextureImporter ti = (TextureImporter)TextureImporter.GetAtPath(path);
			ti.maxTextureSize = maxSize;
			AssetDatabase.ImportAsset(path);
			tex = (Texture2D)AssetDatabase.LoadAssetAtPath(path,typeof(Texture2D));
			AssetDatabase.Refresh();
		}
		public static bool resizeCube( ref Cubemap cube, int maxSize ) {
			string path = AssetDatabase.GetAssetPath(cube);
			TextureImporter ti = getTextureImporter(path, "mset.Util.resizeCube");
			if( ti == null ) return false;
			ti.maxTextureSize = maxSize;
			AssetDatabase.ImportAsset(path);
			cube = (Cubemap)AssetDatabase.LoadAssetAtPath(path,typeof(Cubemap));
			AssetDatabase.Refresh();
			return true;
		}
		public static void writeTexture2D( Texture2D tex, string dir, string name ) {
			string path = Application.dataPath + "/";
			if( dir.Length > 0 ) path += dir;
			path += name + ".png";
		    FileStream fs = new FileStream(path, FileMode.Create);
		    BinaryWriter bw = new BinaryWriter(fs);
		    bw.Write(tex.EncodeToPNG());
		    bw.Close();
		    fs.Close();			
			AssetDatabase.ImportAsset(path);
			AssetDatabase.Refresh();
		}
		public static bool makeReflection( Texture tex ) { return makeReflection( AssetDatabase.GetAssetPath(tex) ); }
		public static bool makeReflection( string path ) {
			TextureImporter ti = getTextureImporter(path,"mset.Util.makeReflection");	
			if( ti == null ) return false;
			ti.textureType = TextureImporterType.Reflection;
			ti.textureFormat = TextureImporterFormat.AutomaticTruecolor;
			ti.generateCubemap = TextureImporterGenerateCubemap.Cylindrical;
			AssetDatabase.ImportAsset(path);
			AssetDatabase.Refresh();
			return true;
		}
		*/
		
		public static void cubeLookup( ref float s, ref float t, ref ulong face, Vector3 dir ) {
			float xmag=Mathf.Abs(dir.x);
			float ymag=Mathf.Abs(dir.y);
			float zmag=Mathf.Abs(dir.z);
			
			//NOTE: Because the result is the unfiltered nearest pixel hit, edges of the cube are
			// going to be tricky. A cube edge lies where magx and magz are equal and could be considered
			// either the x or z face. We just hammer it to be the x face and return face=x, s=1. --Andres
			if( xmag >= ymag && xmag >= zmag ) {
				if( dir.x >= 0.0f ) { face = 0; } //+x
				else { face = 1; } //-x
			} 
			else if( ymag >= xmag && ymag >= zmag ) {
				if( dir.y >= 0.0f ) { face = 2; } //+y
				else { face = 3; } //-y
			}
			else {
				if( dir.z >= 0.0f ) { face = 4; } //+z
				else { face = 5; } //-z
			}
			switch( face ) {
			case 0:
				s = 0.5f*( -dir.z/xmag + 1.0f );
				t = 0.5f*( -dir.y/xmag + 1.0f );
				break;
			case 1:
				s = 0.5f*( dir.z/xmag + 1.0f );
				t = 0.5f*( -dir.y/xmag + 1.0f );
				break;	
			case 2:
				s = 0.5f*( dir.x/ymag + 1.0f );
				t = 0.5f*( dir.z/ymag + 1.0f );
				break;
			case 3:
				s = 0.5f*( dir.x/ymag + 1.0f );
				t = 0.5f*( -dir.z/ymag + 1.0f );
				break;
			case 4:
				s = 0.5f*( dir.x/zmag + 1.0f );
				t = 0.5f*( -dir.y/zmag + 1.0f );
				break;
			case 5:
				s = 0.5f*( -dir.x/zmag + 1.0f );
				t = 0.5f*( -dir.y/zmag + 1.0f );
				break;
			};
		}
		// cube face uv to vector
		public static void invCubeLookup( ref Vector3 dst, ref float weight, ulong face, ulong col, ulong row, ulong faceSize ) {
			float invFaceSize = 2f/(float)faceSize;
			float x = ((float)col+0.5f) * invFaceSize - 1f;
			float y = ((float)row+0.5f) * invFaceSize - 1f;
			switch( face ) {
			case 0:	//+x rotated 180
				dst[0]= 1f; dst[1]= -y; dst[2] =-x;
				break;
			case 1: //-x rotated 180
				dst[0]=-1f; dst[1]= -y; dst[2] = x;
				break;
			case 2: //+y
				dst[0]=  x; dst[1]= 1f; dst[2] = y;
				break;			
			case 3: //-y
				dst[0]=  x; dst[1]=-1f; dst[2] =-y;
				break;
			case 4: //+z
				dst[0]=  x; dst[1]= -y; dst[2] = 1f;
				break;			
			case 5: //-z rotated 180
				dst[0]= -x; dst[1]= -y; dst[2] =-1f;
				break;
			};
			// solid angle is: 4/( (X^2 + Y^2 + Z^2)^(3/2) ) = 4/mag^3
			float mag = dst.magnitude;
			weight = 4f / (mag*mag*mag);
			dst /= mag; //normalize
		}
		// lat-long uv to vector
		public static void invLatLongLookup( ref Vector3 dst, ref float cosPhi, ulong col, ulong row, ulong width, ulong height ) {
			float uvshift = 0.5f;
			float u = ((float)col + uvshift) / (float)width;
			float v = ((float)row + uvshift) / (float)height;			
			float theta = -2f*Mathf.PI*u - 0.5f*Mathf.PI; // minus half a pie to match unity reflection maps
			float phi =   0.5f*Mathf.PI*(2*v-1);
			cosPhi = Mathf.Cos(phi);
			dst.x = Mathf.Cos(theta) * cosPhi;
			dst.y = Mathf.Sin(phi);
			dst.z = Mathf.Sin(theta) * cosPhi;
		}
		public static void cubeToLatLongLookup(ref float pano_u, ref float pano_v, ulong face, ulong col, ulong row, ulong faceSize) {
			Vector3 dir = new Vector3();
			float ignore = -1f;
			invCubeLookup(ref dir, ref ignore, face, col, row, faceSize);
			pano_v = Mathf.Asin(dir.y)/Mathf.PI + 0.5f;
			pano_u = 0.5f*Mathf.Atan2(-dir.x, -dir.z)/Mathf.PI;
			pano_u = Mathf.Repeat(pano_u,1f);
		}
		public static void latLongToCubeLookup(/*cube*/ ref float cube_u, ref float cube_v, ref ulong face, /*pano*/ ulong col, ulong row, ulong width, ulong height ) {
			Vector3 dir = new Vector3();
			float ignore = -1f;
			invLatLongLookup(ref dir, ref ignore, col, row, width, height);
			cubeLookup(ref cube_u, ref cube_v, ref face, dir);
		}
		public static void rotationToInvLatLong(out float u, out float v, Quaternion rot) {
			u = rot.eulerAngles.y;
			v = rot.eulerAngles.x;
			u = Mathf.Repeat(u,360f)/360f;
			v = 1f - Mathf.Repeat(v+90,360f)/180f;
		}
		public static void dirToLatLong(out float u, out float v, Vector3 dir) {
			dir = dir.normalized;
			u = 0.5f*Mathf.Atan2(-dir.x, -dir.z)/Mathf.PI;
			u = Mathf.Repeat(u,1f);
			v = Mathf.Asin(dir.y)/Mathf.PI + 0.5f;
			v = 1f - Mathf.Repeat(v,1f);
		}
		
		public static void applyGamma( ref Color c, float gamma ) {
			c.r = Mathf.Pow(c.r,gamma);
			c.g = Mathf.Pow(c.g,gamma);
			c.b = Mathf.Pow(c.b,gamma);
		}
		public static void applyGamma( ref Color[] c, float gamma ) {
			for(int i=0; i<c.Length; ++i) {
				c[i].r = Mathf.Pow(c[i].r,gamma);
				c[i].g = Mathf.Pow(c[i].g,gamma);
				c[i].b = Mathf.Pow(c[i].b,gamma);
			}		
		}
		public static void applyGamma( ref Color[] dst, Color[] src, float gamma ) {
			for(int i=0; i<src.Length; ++i) {
				dst[i].r = Mathf.Pow(src[i].r,gamma);
				dst[i].g = Mathf.Pow(src[i].g,gamma);
				dst[i].b = Mathf.Pow(src[i].b,gamma);
				dst[i].a = src[i].a; //NOTE: this is here for lazy programmers who use applyGamma to copy data
			}		
		}
		public static void applyGamma2D( ref Texture2D tex, float gamma ) {
			for( int mip = 0; mip < tex.mipmapCount; ++mip ) {
				Color[] c = tex.GetPixels(mip);
				applyGamma(ref c,gamma);
				tex.SetPixels(c);
			}
			tex.Apply(false);
		}
	
		public static void clearTo( ref Color[] c, Color color ) {
			for(int i=0; i<c.Length; ++i) {
				c[i] = color;
			}
		}
		public static void clearTo2D( ref Texture2D tex, Color color ) {
			for( int mip = 0; mip < tex.mipmapCount; ++mip ) {
				Color[] c = tex.GetPixels(mip);
				clearTo(ref c, color);
				tex.SetPixels(c,mip);
			}
			tex.Apply(false);
		}
		
		public static void clearChecker2D( ref Texture2D tex ) {
			Color gray0 = new Color(0.25f,0.25f,0.25f,0.25f);
			Color gray1 = new Color(0.50f,0.50f,0.50f,0.25f);
			Color[] c = tex.GetPixels();
			int w = tex.width;
			int h = tex.height;
			int sqw = h/4;	//width of square
			for(int x=0; x<w; ++x)
			for(int y=0; y<h; ++y) {
				if(((x/sqw)%2) == ((y/sqw)%2))	c[y*w + x] = gray0;
				else 							c[y*w + x] = gray1;
			}
			tex.SetPixels(c);
			tex.Apply(false);
		}
		
		public static void clearCheckerCube( ref Cubemap cube ) {
			Color gray0 = new Color(0.25f,0.25f,0.25f,0.25f);
			Color gray1 = new Color(0.50f,0.50f,0.50f,0.25f);
			Color[] c = cube.GetPixels(CubemapFace.NegativeX);
			int w = cube.width;
			int sqw = Mathf.Max(1,w/4);	//width of square
			for( int face=0; face<6; ++face ) {
				for(int x=0; x<w; ++x)
				for(int y=0; y<w; ++y) {
					if(((x/sqw)%2) == ((y/sqw)%2))	c[y*w + x] = gray0;
					else 							c[y*w + x] = gray1;
				}
				cube.SetPixels(c, (CubemapFace)face);
			}
			cube.Apply(true);
		}

		
		public static bool writePNG( ref Texture2D tex, string assetPath ) {
			try {
				assetPath = assetPath.Substring(7);
				string path = Application.dataPath + "/" + assetPath;
				FileStream fs = new FileStream(path, FileMode.Create);
			    BinaryWriter bw = new BinaryWriter(fs);
			    bw.Write(tex.EncodeToPNG());
			    bw.Close();
			    fs.Close();
				assetPath = "Assets/" + assetPath;				
			} catch(Exception e) {
				Debug.LogError("FileStream exception: " + e.ToString());
				return false;
			}
			return true;
		}
		
		// serialized asset management
		public static void printSerializedProperties(UnityEngine.Object obj) {
			SerializedObject srobj = new SerializedObject(obj);
			
			int breaker = 1000;
			SerializedProperty itr = srobj.GetIterator();
			
			string props = "";
			itr.Next(true);
			do {
				if(itr.name.StartsWith("m_")) props += itr.name + "\n";
				breaker--;
			} while(breaker>0 && itr.Next(true));
			Debug.Log("Properties of " + obj.name + ":\n" + props);
		}
		
		public static bool isReadable(ref SerializedObject serialTex) {
			if(serialTex == null) return false;
			SerializedProperty prop;
			prop = serialTex.FindProperty("m_IsReadable"); 	if( prop != null ) return prop.boolValue;
			prop = serialTex.FindProperty("m_ReadAllowed"); if( prop != null ) return prop.boolValue;
			Debug.LogError("m_IsReadable or m_ReadAllowed SerializedProperty not found!");
			return false;
		}
		public static void setReadable(ref SerializedObject serialTex, bool readable) {
			if(serialTex == null) return;
			SerializedProperty prop;
			prop = serialTex.FindProperty("m_IsReadable"); 	if( prop != null ) prop.boolValue = readable;
			prop = serialTex.FindProperty("m_ReadAllowed"); if( prop != null ) prop.boolValue = readable;
			serialTex.ApplyModifiedProperties();
		}

		//not all texture compression formats are getPixel readable
		public static bool isReadableFormat(Texture2D tex) {
			//must be ARGB32, RGBA32, BGRA32, RGB24, Alpha8 or DXT
			return 
				tex.format == TextureFormat.Alpha8 || 
				tex.format == TextureFormat.ARGB32 ||
				tex.format == TextureFormat.RGBA32 ||
				tex.format == TextureFormat.BGRA32 ||
				tex.format == TextureFormat.RGB24 ||
				tex.format == TextureFormat.DXT1 ||
				tex.format == TextureFormat.DXT5;
		}


		// returns whether the "Linear" checkbox is checked on a cubemap or 2D texture
		public static bool isLinear(SerializedObject serialTex) {
			if(serialTex == null) return false;
			SerializedProperty prop = serialTex.FindProperty("m_ColorSpace");
			if( prop != null ) {
				//lol wut? Did unity get this backwards?
				return prop.intValue == (int)ColorSpace.Gamma;
			}
			Debug.LogError("m_ColorSpace SerializedProperty not found!");
			return false;
		}
		public static void setLinear(ref SerializedObject serialTex, bool linear) {
			if(serialTex == null) return;
			SerializedProperty prop = serialTex.FindProperty("m_ColorSpace");
			if( prop != null ) {
				//lol wut? Did unity get this backwards?
				prop.intValue = linear ? (int)ColorSpace.Gamma : (int)ColorSpace.Linear;
				serialTex.ApplyModifiedProperties();
			} else {
				Debug.LogError("m_ColorSpace SerializedProperty not found!");
			}
		}
				
		public static bool isMipmapped(SerializedObject serialTex) {
			if(serialTex == null) return false;
			SerializedProperty prop = serialTex.FindProperty("m_MipMap");
			if( prop != null ) { return prop.boolValue; }
			Debug.LogError("m_MipMap SerializedProperty not found!");
			return false;
		}
		public static void setMipmapped(ref SerializedObject serialTex, bool mipmap) {
			if(serialTex == null) return;
			SerializedProperty prop = serialTex.FindProperty("m_MipMap");
			if( prop != null ) {
				prop.boolValue = mipmap;
				serialTex.ApplyModifiedProperties();
			} else {
				Debug.LogError("m_MipMap SerializedProperty not found!");
			}
		}
		public static bool hasAlpha(Texture2D tex) {
			if(tex==null) return false;
			TextureImporter ti = mset.Util.getTextureImporter(AssetDatabase.GetAssetPath(tex), "mset.Util.hasAlpha");
			if( ti ) return ti.DoesSourceTextureHaveAlpha();
			return false;
		}
		
		public class GUILayout {
			public static Rect drawTexture( float width, float height, string label, Texture2D tex, bool blended ) { return drawTexture(0,0,width,height,label,tex,blended); }
			public static Rect drawTexture( float xoffset, float yoffset, float width, float height, string label, Texture2D tex, bool blended ) {
				Rect border = GUILayoutUtility.GetRect(width+2, height+2);
				border.width = width+2;
				border.x += xoffset;
				border.y += yoffset;
				UnityEngine.GUI.Box(border, label, "HelpBox");
				border.x++;
				border.y++;
				border.width-=2;
				border.height-=2;		
				if( tex != null ) UnityEngine.GUI.DrawTexture(border, tex, ScaleMode.StretchToFill, blended);
				return border;
			}
			public static bool tinyButton( float x, float y, string label, float label_x, float label_y ) {
				return tinyButton(x,y,label,"",label_x,label_y);
			}
			public static bool tinyButton( float x, float y, string label, string tip, float label_x, float label_y ) {
				Rect rect = new Rect(x,y,12,14);
				bool b = GUI.Button(rect,new GUIContent("",tip),"Toggle");
				rect.x += label_x;
				rect.y += label_y;
			#if !(UNITY_3_5 || UNITY_4_0 || UNITY_4_1 || UNITY_4_2)
				rect.y -= 1;
			#endif
				GUI.Label(rect,label);
				return b;
			}
			public static bool tinyButton( float x, float y, Texture2D icon, string tip, float label_x, float label_y ) {
				Rect rect = new Rect(x,y,12,14);
				bool b = GUI.Button(rect,new GUIContent("",tip),"Toggle");
				rect.x += label_x;
				rect.y += label_y;
			#if !(UNITY_3_5 || UNITY_4_0 || UNITY_4_1 || UNITY_4_2)
				rect.y -= 1;
			#endif
				rect.width = 16;
				rect.height = 14;
				GUI.DrawTexture(rect,icon);
				return b;
			}
			public static bool tinyToggle( float x, float y, string label, float label_x, float label_y, bool val ) {
				return tinyToggle(x,y,label,"",label_x,label_y,val);
			}
			public static bool tinyToggle( float x, float y, string label, string tip, float label_x, float label_y, bool val ) {
				Rect rect = new Rect(x,y,12,14);
				GUIStyle style = new GUIStyle("Toggle");
				if( val ) style.normal = style.active;
				if( GUI.Button(rect,new GUIContent("",tip),style) ) val = !val;
				rect.x += label_x;
				rect.y += label_y;
				GUI.Label(rect,label);
				return val;
			}
		};
	};
}

