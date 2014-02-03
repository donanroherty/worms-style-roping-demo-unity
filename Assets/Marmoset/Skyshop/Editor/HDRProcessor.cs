// Marmoset Skyshop
// Copyright 2013 Marmoset LLC
// http://marmoset.co

using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace mset {
	//This class will be run after every .hdr or .pfm image file is imported into the project and generate an additional .png file next to
	//them in R-G-B-Multiplier format.
	public class HDRProcessor : AssetPostprocessor {
		public string assetExt;
		private delegate bool ImageReader(ref Texture2D tex, string path);
		public void OnPreprocessTexture() {			
	    }
		public void OnPostprocessTexture(Texture2D texture) {			
		}
		public static void OnPostprocessAllAssets ( string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
	        for (int a=0; a<importedAssets.Length; ++a) {
	        	string imagePath = importedAssets[a];
				string ext = Path.GetExtension(imagePath).ToLowerInvariant();
				
				//images with the ignore string in their filename will not be imported
				string name = Path.GetFileNameWithoutExtension(imagePath).ToLowerInvariant();
				if(name.Contains("_noimport")) continue;
					
				HDRProcessor.ImageReader readImage=null;
				ext = ext.ToLowerInvariant();
				if 		(ext.Equals(".pfm") || ext.Equals(".pbm")) readImage = readPFM;
				else if (ext.Equals(".hdr") || ext.Equals(".rgbe") || ext.Equals(".xyze")) readImage = readHDR;
				
				if( readImage != null ) {
					string texPath = Path.ChangeExtension(imagePath,".png");
					Texture2D tex = AssetDatabase.LoadAssetAtPath(texPath,typeof(Texture2D)) as Texture2D;
					if( !tex ) {
						tex = new Texture2D(32,32);
						AssetDatabase.CreateAsset(tex, texPath);
					}
					SerializedObject srTex = new SerializedObject(tex);
					mset.Util.setReadable(ref srTex,true);
					if( readImage(ref tex, imagePath) ) {
						mset.Util.writePNG(ref tex, texPath);
						AssetDatabase.Refresh();
						AssetDatabase.SaveAssets();
					} else {
						Debug.LogError("Failed to import custom image file \"" + imagePath + "\"");
						AssetDatabase.DeleteAsset(texPath);
					}
				}
			}
	    }
		//LOADERS
		public static bool readPFM(ref Texture2D tex, string path) {
			string fullPath = Application.dataPath + "/" + path.Substring(7);
			FileStream fs = new FileStream(fullPath, FileMode.Open);
		    BinaryReader stream = new BinaryReader(fs);
				
			string [] line = new string[4];
			char c;

			for( int i=0; i<4; ++i ) {
				do {//read up to whitespace
					c = stream.ReadChar();
					line[i] += c;
				} while( c != '\n' && c!='\t' && c!=' ' && c!='\r' );
		
				//in between lines kill the whitespace
				if( i<3 )
				while( c != '\n' && c!='\t' && c!=' ' && c!='\r' ) {
					c = stream.ReadChar();
				}
			}
			
			int mComponentSize, mComponentCount;
			bool float32 = false;
			bool float16 = false;
		
			if( line[0][0] == 'P' )			{ mComponentSize = 4; float32 = true; }
			else if( line[0][0] == 'p' )	{ mComponentSize = 2; float16 = true; }
			else { Debug.LogError( "image not in PFM format\n" ); return false; }
			
			if( line[0][1] == 'F' )			{ mComponentCount = 3; }
			else if( line[0][1] == 'f' )	{ mComponentCount = 1; }
			else { Debug.LogError( "image not in PFM format\n" ); return false; }
			
			if( float16 ) {
				Debug.LogError("float16 PFMs not supported, please convert to float32\n");
				return false;
			}
		
			int mWidth = Convert.ToInt32(line[1]);
			int mHeight = Convert.ToInt32(line[2]);
			float scale = Convert.ToSingle(line[3]);
			int mDataSize = mComponentSize * mComponentCount * mWidth * mHeight;
			
			float[] floatData = new float[mDataSize/4];
			for( int i = 0; i<mDataSize/4; ++i ) {
				floatData[i] = stream.ReadSingle();
			}
			stream.Close();
		    fs.Close();
		
			tex.Resize(mWidth, mHeight,TextureFormat.ARGB32,false);
			Color32[] pixels32 = tex.GetPixels32();
			//apply scale
			scale = Mathf.Abs(scale);
			if( float32 ) {
				Color pixel = new Color();
				for( int x=0; x<mWidth; ++x )
				for( int y=0; y<mHeight; ++y ) {
					int i = x + y*mWidth;
					int j = x + (mHeight-y-1)*mWidth;
					pixel.r = scale * floatData[j*mComponentCount];
					pixel.g = scale * floatData[j*mComponentCount+1];
					pixel.b = scale * floatData[j*mComponentCount+2];					
					RGB.toRGBM(ref pixels32[i], pixel, true);
				}
			}
			else if( float16 ) {
				//float16 pfms are something we made up for toolbag apparently.
			}
			tex.SetPixels32(pixels32);
			tex.Apply(false);
			return true;
		}
		private static void writeCString(ref BinaryWriter stream, string str) {
			for(int i=0; i<str.Length; ++i) stream.Write(str[i]);
		}
		public static bool writeHDR(ref Color[] pixels, int width, int height, string fullPath) {
			FileStream fs = new FileStream(fullPath,FileMode.Create);
			BinaryWriter stream = new BinaryWriter(fs);
			
			writeCString(ref stream, "#?RADIANCE\n");
			writeCString(ref stream, "# Organically grown in Marmoset Skyshop for Unity\n");
			writeCString(ref stream, "FORMAT=32-bit_rle_rgbe\n");
			writeCString(ref stream, "EXPOSURE=1.0\n\n");
			
			string Y;
			bool flipY = true; //Photoshop and HDRshop happily ignore this flag and expect a flipped Y
			if( flipY ) Y = "+Y ";
			else 		Y = "+Y ";
			writeCString(ref stream, Y + height + " +X " + width + "\n");
			
			byte[] rowHeader=new byte[4];
			for(int r=0; r<height; ++r) {
				rowHeader[0] = 2;
				rowHeader[1] = 2;
				rowHeader[2] = (byte)(width>>8);
				rowHeader[3] = (byte)(width&255);
				stream.Write(rowHeader);
				
				int rowOffset;
				if(flipY)	rowOffset = (height-r-1)*width;
				else 		rowOffset = r*width;
				for( int c=0; c<4; ++c ) {
					int i=0;
					while( i < width ) {
						//HACK: always dump, no RLE compression
						byte n = (byte)Mathf.Min(width-i, 128);
						stream.Write(n);
						for( byte dump=0; dump<n; ++dump ) {							
							Color32 rgbe = new Color32();
							RGB.toRGBE(ref rgbe, pixels[rowOffset+i]);
							if(c==0)stream.Write(rgbe.r);
							else if(c==1)stream.Write(rgbe.g);
							else if(c==2)stream.Write(rgbe.b);
							else stream.Write(rgbe.a);
							++i;
						}
					}
				}
			}
			stream.Close();
		    fs.Close();
			return true;
		}
		
		public static bool readHDR(ref Texture2D tex, string path) {
			string fullPath = Application.dataPath + "/" + path.Substring(7);
			FileStream fs = new FileStream(fullPath, FileMode.Open);
		    BinaryReader stream = new BinaryReader(fs);			
			
			string properMagic = "#?RADIANCE";
			char[] magic = stream.ReadChars(properMagic.Length);
			string magicStr = new string(magic,0,magic.Length);
			if( !magicStr.Equals(properMagic) ) {
				Debug.LogError("Invalid .hdr file, magic: " + magic);
				return false;
			}
			
			//read in our parameters
			bool xyz = false;
			float exposure = 1f;
			//bool flipX=false;	//Yeah no we're not doing that. --Andres
			bool flipY=false;
			int width=-1;
			int height=-1;
			
			string name = "";
			string val = "";
			//HEADER - terminated by a blank line
			while( stream.PeekChar() != -1 && (width == -1 || height == -1) ) {
				readPair(ref stream, ref name, ref val);
				name = name.ToLowerInvariant();
				val = val.ToLowerInvariant();
				
				if( name.Equals("format") ) {
					if( val.Equals("32-bit_rle_rgbe") ) { xyz = false; }
					else if( val.Equals("32-bit_rle_xyze") ) { xyz = true; }
				}
				else if( name.Equals("exposure") ) {
					exposure = Convert.ToSingle(val);
				}
				if( name.Equals("+x") ) {
					//flipX = false;
					width = Convert.ToInt32(val);
				}
				else if( name.Equals("-x") ) {
					//flipX = true;
					width = Convert.ToInt32(val);
				}
				else if( name.Equals("+y") ) {
					flipY = false;
					height = Convert.ToInt32(val);
				}
				else if( name.Equals("-y") ) {
					flipY = true;
					height = Convert.ToInt32(val);
				} else {
					//do not want
				}
			}
			//Wat? some .HDR files do this and break everything.
			if( exposure <= 0f ) exposure = 1f;
		
			if( width <= 0 || height <= 0 ) {
				Debug.LogError("Invalid dimensions found in .hdr file (" + width + " x "+height+")");
				return false;
			}
			
			tex.Resize(width, height,TextureFormat.ARGB32,false);
			Color[] pixels = tex.GetPixels();
			byte[] rowHeader = null;
			for(int r=0; r<height; ++r) {
				int rowOffset = r*width;
				rowHeader = stream.ReadBytes(4);
				if( rowHeader[0] != 2 || rowHeader[1] != 2 || (256*rowHeader[2] + rowHeader[3]) != width ) {
					Debug.LogError("Invalid row header data found in .hdr file");
					return false;
				}
				for( int c=0; c<4; ++c ) {
					int i=0;
					while( i < width ) {
						byte n = stream.ReadByte();
						if( n > 128 ) {
							//run, duplicate the next byte 'n' times
							n -= 128;
							if( i+(int)n > width ) {
								Debug.LogError("invalid row size found in hdr file (corrupt or otherwise odd file?)");
								return false;
							}
							byte b = stream.ReadByte();
							float f = (float)b;
							for( byte run=0; run<n; ++run ) {
								pixels[rowOffset + i][c] = f;
								++i;
							}
						} else {
							//dump, read the next 'n' components
							if( i+n > width ) {
								Debug.LogError("invalid row size found in hdr file (corrupt or otherwise odd file?)");
								return false;
							}
							for( int dump=0; dump<n; ++dump ) {
								byte b = stream.ReadByte();
								float f = (float)b;
								pixels[rowOffset + i][c] = f;
								++i;
							}
						}
					}
				}
			}
			stream.Close();
		    fs.Close();
		
			Color32[] dst = new Color32[pixels.Length];
			//convert the row to real color
			for( int r=0; r<height; ++r ) {
				int rowOffset = r*width;
				int dstOffset = rowOffset;
				if( flipY ) dstOffset = (height-r-1)*width;
				
				int src_i, dst_i;
				float e, scale;
				for( int c=0; c<width; ++c ) {
					src_i = rowOffset + c;
					dst_i = dstOffset + c;
					e = pixels[src_i][3] - 128f;
					scale = exposure * Mathf.Pow(2f, e) / 255f;
					pixels[src_i][0] = pixels[src_i][0] * scale;
					pixels[src_i][1] = pixels[src_i][1] * scale;
					pixels[src_i][2] = pixels[src_i][2] * scale;
					pixels[src_i][3] = 1f;
					if(xyz) RGB.fromXYZ(ref pixels[src_i], pixels[src_i]);
					RGB.toRGBM(ref dst[dst_i], pixels[src_i], true);
				}
			}
			
			tex.SetPixels32(dst);
			tex.Apply(false);
			return true;
		}
		//HELPERS 
		
		//reads and discards every breaker character until a non-breaker character is found 
		private static int eatBreakers(ref BinaryReader stream, string breakers) {
			int i=0;
			int c = -1;
			for(; i<256; ++i ) {
				c = stream.PeekChar();
				if( c == -1 ) return i;
				bool match = false;
				for(int j=0; j<breakers.Length; ++j) {
					if((char)c == breakers[j]) {
						stream.ReadChar();
						match = true;
						break;
					}
				}
				if(!match) return i;
			}
			return i;
		}
		//reads and discards white-space preceeding # and the rest of the line following # as an ignored comment
		private static void eatComments(ref BinaryReader stream) {
			eatBreakers(ref stream, " \t");
			string comment = "";
			int c = stream.PeekChar();
			if( c != -1 && (char)c == '#' ) {
				readUntil(ref stream, ref comment, "\n\r");
				eatBreakers(ref stream, "\n\r");
			}
		}
		//appends to a string until one of the breaker characters is found, breaker character is left in the stream
		private static int readUntil(ref BinaryReader stream, ref string line, string breakers ) {
			int c = -1;
			int i=0;			
			for(i=0; i<256; ++i) {
				c = stream.PeekChar();
				if( c == -1 ) return i;
				for(int j=0; j<breakers.Length; ++j) {
					if((char)c == breakers[j]) return i;
				}
				line += (char)c;
				stream.ReadChar();
			}
			return i;
		}
		//reads a name-value assignment pair from a binary stream
		private static bool readPair( ref BinaryReader stream, ref string name, ref string val ) {
			eatBreakers(ref stream, "\n\r");
			eatComments(ref stream);
			eatBreakers(ref stream, " \t");
			name="";
			val="";
			int count = readUntil( ref stream, ref name, " =\t");
			if(count==0) {
				Debug.LogError("Bad .hdr header value: empty pair-name");
				return false;
			}
			eatBreakers(ref stream, " =\t");
			count = readUntil( ref stream, ref val, " \t\n\r");
			eatBreakers(ref stream, " \t\n\r");
			if( count == 0 ) {
				Debug.LogError("Bad .hdr header pair");
				return false;
			}
			return true;
		}
		
		
	};
}