// Marmoset Skyshop
// Copyright 2013 Marmoset LLC
// http://marmoset.co

using UnityEngine;
using UnityEditor;
using System;
using System.IO;
	
namespace mset {
	public class CubeBuffer {
		public enum FilterMode {
			NEAREST,
			BILINEAR,
			BICUBIC
		};
		public delegate void SampleFunc(ref Color dst, float u, float v, int face);
		public SampleFunc sample = null;
		
		private FilterMode _filterMode;
		public FilterMode filterMode {
			set {
				_filterMode = value;
				switch(_filterMode){
				case FilterMode.NEAREST:
					sample = sampleNearest;
					break;
				case FilterMode.BILINEAR:
					sample = sampleBilinear;
					break;
				case FilterMode.BICUBIC:
					sample = sampleBicubic;
					break;
				};
			}
			get {
				return _filterMode;
			}
		}
		
		public int		faceSize = 0;
		public Color[]	pixels = null;
		
		public int width  { get { return faceSize; } }
		public int height { get { return faceSize*6; } }
		
		public CubeBuffer() {
			filterMode = FilterMode.BILINEAR;
			clear();
		}
		
		~CubeBuffer() {
		}
		
		public void clear() {
			pixels = null;
			faceSize = 0;
		}
		
		public bool empty() {
			if( pixels == null ) return true;
			if( pixels.Length == 0 ) return true;
			return false;
		}
		
		public static void pixelCopy(ref Color[] dst, int dst_offset,
					         	  Color[] src, int src_offset, int count) {
			for( int i=0; i<count; ++i ) {
				dst[dst_offset + i] = src[src_offset+i];
			}
		}
		public static void pixelCopy(ref Color[] dst, int dst_offset,
					         	Color32[] src, int src_offset, int count) {
			float toFloat = 1f/255f;
			for( int i=0; i<count; ++i ) {
				dst[dst_offset + i].r = (float)src[src_offset+i].r*toFloat;
				dst[dst_offset + i].g = (float)src[src_offset+i].g*toFloat;
				dst[dst_offset + i].b = (float)src[src_offset+i].b*toFloat;
				dst[dst_offset + i].a = (float)src[src_offset+i].a*toFloat;
			}
		}
		//TODO: this is doing encode's work, make sure this doesn't happen twice!
		public static void pixelCopy(ref Color32[] dst, int dst_offset,
					         	    Color[] src, int src_offset, int count) {
			for( int i=0; i<count; ++i ) {
				dst[dst_offset + i].r = (byte)Mathf.Clamp(src[src_offset+i].r*255f,0f,255f);
				dst[dst_offset + i].g = (byte)Mathf.Clamp(src[src_offset+i].g*255f,0f,255f);
				dst[dst_offset + i].b = (byte)Mathf.Clamp(src[src_offset+i].b*255f,0f,255f);
				dst[dst_offset + i].a = (byte)Mathf.Clamp(src[src_offset+i].a*255f,0f,255f);
			}
		}
		
		public static void pixelCopyBlock<T>(ref T[] dst, int dst_x, int dst_y, int dst_w,
											  	 T[] src, int src_x, int src_y, int src_w,
										  		 		  int block_w, int block_h, bool flip ) {
			if(flip) {
				for( int block_x = 0; block_x<block_w; ++block_x )
				for( int block_y = 0; block_y<block_h; ++block_y ) {
					int dst_i = (dst_y+block_y)*dst_w + dst_x + block_x;
					int src_i =  (src_y+(block_h-block_y-1))*src_w + src_x + block_x;					
					dst[dst_i] = src[src_i];
				}
			} else {
				for( int block_x = 0; block_x<block_w; ++block_x )
				for( int block_y = 0; block_y<block_h; ++block_y ) {
					int dst_i = (dst_y+block_y)*dst_w + dst_x + block_x;
					int src_i =  (src_y+block_y)*src_w + src_x + block_x;
					dst[dst_i] = src[src_i];
				}
			}
		}
		
		//
		public static void encode(ref Color[] dst, Color[] src, ColorMode outMode, bool useGamma ){
			if(outMode == ColorMode.RGBM8) {
				for( int i=0; i<src.Length; ++i) {
					RGB.toRGBM(ref dst[i], src[i], useGamma);
				}
			} else {
				if( useGamma )	mset.Util.applyGamma(ref dst, src, Gamma.toSRGB);
				else 			pixelCopy(ref dst, 0, src, 0, src.Length);
			}
		}
		public static void encode(ref Color32[] dst, Color[] src, ColorMode outMode, bool useGamma ){
			if(outMode == ColorMode.RGBM8) {
				for( int i=0; i<src.Length; ++i) {
					RGB.toRGBM(ref dst[i], src[i], useGamma);
				}
			} else {
				if( useGamma )	mset.Util.applyGamma(ref src, src, Gamma.toSRGB);
				pixelCopy(ref dst, 0, src, 0, src.Length);
			}
		}
		public static void decode(ref Color[] dst, Color[] src, ColorMode inMode, bool useGamma ){
			if(inMode == ColorMode.RGBM8) {
				for( int i=0; i<src.Length; ++i) {
					RGB.fromRGBM(ref dst[i], src[i], useGamma);
				}
			} else {
				if( useGamma ){
					mset.Util.applyGamma(ref dst, src, Gamma.toLinear);
				}				
				else {
					pixelCopy(ref dst, 0, src, 0, src.Length);
				}
				clearAlpha(ref dst);
			}
		}
		public static void decode(ref Color[] dst, Color32[] src, ColorMode inMode, bool useGamma ){
			if(inMode == ColorMode.RGBM8) {
				for( int i=0; i<src.Length; ++i) {
					RGB.fromRGBM(ref dst[i], src[i], useGamma);
				}
			} else {
				pixelCopy(ref dst, 0, src, 0, src.Length);
				if( useGamma ) mset.Util.applyGamma(ref dst, Gamma.toLinear);
				clearAlpha(ref dst);
			}
		}
		public static void decode(ref Color[] dst, int dst_offset, Color[] src, int src_offset, int count, ColorMode inMode, bool useGamma ){
			if(inMode == ColorMode.RGBM8) {
				for( int i=0; i<count; ++i) {
					RGB.fromRGBM(ref dst[i+dst_offset], src[i+src_offset], useGamma);
				}
			} else {
				pixelCopy(ref dst, dst_offset, src, src_offset, count);
				if( useGamma ) mset.Util.applyGamma(ref dst, Gamma.toLinear);
				clearAlpha(ref dst, dst_offset, count);
			}
		}
		
		public static void decode(ref Color[] dst, int dst_offset, Color32[] src, int src_offset, int count, ColorMode inMode, bool useGamma ){
			if(inMode == ColorMode.RGBM8) {
				for( int i=0; i<count; ++i) {
					RGB.fromRGBM(ref dst[i+dst_offset], src[i+src_offset], useGamma);
				}
			} else {
				pixelCopy(ref dst, dst_offset, src, src_offset, count);
				if( useGamma ) mset.Util.applyGamma(ref dst, Gamma.toLinear);
				clearAlpha(ref dst, dst_offset, count);
			}
		}
		
		public static void clearAlpha(ref Color[] dst) { clearAlpha(ref dst, 0, dst.Length); }
		public static void clearAlpha(ref Color[] dst, int offset, int count) {
			for( int i=offset; i<offset+count; ++i ) {
				dst[i].a = 1f;
			}
		}
		public static void clearAlpha(ref Color32[] dst) { clearAlpha(ref dst, 0, dst.Length); }
		public static void clearAlpha(ref Color32[] dst, int offset, int count) {
			for( int i=offset; i<offset+count; ++i ) {
				dst[i].a = 255;
			}
		}
		public void applyExposure( float mult ) {			
			for(int i =0; i<pixels.Length; ++i) {
				pixels[i].r *= mult;
				pixels[i].g *= mult;
				pixels[i].b *= mult;
				//NOTE: alpha is not touched because it is treated as a multiplier
				//even in non-RGBM images. It must remain 1f when writing out LDR
				//images.
			}
		}
						
		public int toIndex(int face, int x, int y) {
			x = Mathf.Clamp(x,0,faceSize-1);
			y = Mathf.Clamp(y,0,faceSize-1);
			return faceSize*faceSize*face + faceSize*y + x;
		}
		public int toIndex(CubemapFace face, int x, int y) {
			x = Mathf.Clamp(x,0,faceSize-1);
			y = Mathf.Clamp(y,0,faceSize-1);
			return faceSize*faceSize*(int)face + faceSize*y + x;
		}
		
		private class CubeEdge {
			public int other;
			public bool flipped;
			public bool swizzled;
			public bool mirrored;
			public bool minEdge;
			public CubeEdge(int Other, bool flip, bool swizzle) {
				other = Other;
				flipped = flip;
				swizzled = swizzle;
				mirrored = false;
				minEdge = false;
			}
			public CubeEdge(int Other, bool flip, bool swizzle, bool mirror) {
				other = Other;
				flipped = flip;
				swizzled = swizzle;
				mirrored = mirror;
				minEdge = false;
			}
			
			public void transmogrify(ref int primary, ref int secondary, ref int face, int faceSize) {
				bool changed = false;
				if( minEdge && primary<0 ) {
					primary = faceSize+primary;
					changed = true;
				}
				else if( !minEdge && primary >= faceSize ) {
					primary %= faceSize;
					changed = true;
				}
				if( changed ) {
					if( mirrored ) {
						primary = faceSize-primary-1;
					}
					if( flipped ) {
						secondary = faceSize-secondary-1;
					}
					if( swizzled ) {
						int temp = secondary;
						secondary = primary;
						primary = temp;
					}
					face = other;
				}
			}
			
			//take out of bounds sample coordinates and map them onto neighboring faces
			public void transmogrify(ref int primary_i, ref int primary_j, ref int secondary_i, ref int secondary_j, ref int face_i, ref int face_j, int faceSize) {
				if( primary_i<0 ) {
					primary_i = primary_j = faceSize-1;
				} else {
					primary_i = primary_j = 0;
				}
				if( mirrored ) {
					primary_i = faceSize-primary_i-1;
					primary_j = faceSize-primary_j-1;
				}
				if( flipped ) {
					secondary_i = faceSize-secondary_i-1;
					secondary_j = faceSize-secondary_j-1;
				}
				if( swizzled ) {
					int temp;
					temp = secondary_i; secondary_i = primary_i; primary_i = temp;
					temp = secondary_j; secondary_j = primary_j; primary_j = temp;
				}
				face_i = face_j = other;
			}
		};
		private static CubeEdge[] _leftEdges = null;
		private static CubeEdge[] _rightEdges = null;
		
		private static CubeEdge[] _upEdges = null;
		private static CubeEdge[] _downEdges = null;
		
		private static void linkEdges() {
			if( _leftEdges == null ) {
				//Allocate edge mappings between faces, with flip and rotate flags for some weird cases
				_leftEdges = new CubeEdge[6];
				_leftEdges[(int)CubemapFace.NegativeX] = new CubeEdge((int)CubemapFace.NegativeZ,false,false);
				_leftEdges[(int)CubemapFace.PositiveX] = new CubeEdge((int)CubemapFace.PositiveZ,false,false);
				_leftEdges[(int)CubemapFace.NegativeY] = new CubeEdge((int)CubemapFace.NegativeX,true,true );
				_leftEdges[(int)CubemapFace.PositiveY] = new CubeEdge((int)CubemapFace.NegativeX,false,true,true);
				_leftEdges[(int)CubemapFace.NegativeZ] = new CubeEdge((int)CubemapFace.PositiveX,false,false);
				_leftEdges[(int)CubemapFace.PositiveZ] = new CubeEdge((int)CubemapFace.NegativeX,false,false);
								
				_rightEdges = new CubeEdge[6];
				_rightEdges[(int)CubemapFace.NegativeX] = new CubeEdge((int)CubemapFace.PositiveZ,false,false);
				_rightEdges[(int)CubemapFace.PositiveX] = new CubeEdge((int)CubemapFace.NegativeZ,false,false);
				_rightEdges[(int)CubemapFace.NegativeY] = new CubeEdge((int)CubemapFace.PositiveX,false,true,true );
				_rightEdges[(int)CubemapFace.PositiveY] = new CubeEdge((int)CubemapFace.PositiveX,true, true );
				_rightEdges[(int)CubemapFace.NegativeZ] = new CubeEdge((int)CubemapFace.NegativeX,false,false);
				_rightEdges[(int)CubemapFace.PositiveZ] = new CubeEdge((int)CubemapFace.PositiveX,false,false);
				
				_upEdges = new CubeEdge[6];
				_upEdges[(int)CubemapFace.NegativeX] = new CubeEdge((int)CubemapFace.PositiveY,false,true,true);
				_upEdges[(int)CubemapFace.PositiveX] = new CubeEdge((int)CubemapFace.PositiveY,true,true);
				_upEdges[(int)CubemapFace.NegativeY] = new CubeEdge((int)CubemapFace.PositiveZ,false,false);
				_upEdges[(int)CubemapFace.PositiveY] = new CubeEdge((int)CubemapFace.NegativeZ,true,false,true);
				_upEdges[(int)CubemapFace.NegativeZ] = new CubeEdge((int)CubemapFace.PositiveY,true,false,true);
				_upEdges[(int)CubemapFace.PositiveZ] = new CubeEdge((int)CubemapFace.PositiveY,false,false);
				
				_downEdges = new CubeEdge[6];
				_downEdges[(int)CubemapFace.NegativeX] = new CubeEdge((int)CubemapFace.NegativeY,true, true);
				_downEdges[(int)CubemapFace.PositiveX] = new CubeEdge((int)CubemapFace.NegativeY,false, true, true);
				_downEdges[(int)CubemapFace.NegativeY] = new CubeEdge((int)CubemapFace.NegativeZ,true,false,true);
				_downEdges[(int)CubemapFace.PositiveY] = new CubeEdge((int)CubemapFace.PositiveZ,false,false);
				_downEdges[(int)CubemapFace.NegativeZ] = new CubeEdge((int)CubemapFace.NegativeY,true,false,true);
				_downEdges[(int)CubemapFace.PositiveZ] = new CubeEdge((int)CubemapFace.NegativeY,false,false);
				
				for(int i=0;i<6;++i){
					_leftEdges[i].minEdge = _upEdges[i].minEdge = true;	// coord < 0 edges
					_rightEdges[i].minEdge = _downEdges[i].minEdge = false;	// coord >= faceSize edges
				}
			
				/*    ___
				     |+y |
				  ___|___|___ ___
				 |-x |+z |+x |-z |
				 |___|___|___|___|
				     |-y |
				     |___|
				 */
			}
		}
		
		public int toIndexLinked(int face, int u, int v) {
			linkEdges();
			int currFace = face;
			_leftEdges[currFace].transmogrify(	ref u, ref v, ref currFace, faceSize);
			_upEdges[currFace].transmogrify(	ref v, ref u, ref currFace, faceSize);
			_rightEdges[currFace].transmogrify(	ref u, ref v, ref currFace, faceSize);
			_downEdges[currFace].transmogrify(	ref v, ref u, ref currFace, faceSize);
			
			u = Mathf.Clamp (u,0,faceSize-1);
			v = Mathf.Clamp (v,0,faceSize-1);
			
			return toIndex(currFace,u,v);
		}
		
		public void sampleNearest(ref Color dst, float u, float v, int face) {
			int ui = Mathf.FloorToInt(faceSize*u);
			int vi = Mathf.FloorToInt(faceSize*v);
			dst = pixels[faceSize*faceSize*face + faceSize*vi + ui];
		}
		
		public void sampleBilinear(ref Color dst, float u, float v, int face) {
			u = faceSize*u + 0.5f;
			int ui = Mathf.FloorToInt(u)-1;
			u = Mathf.Repeat(u,1f);
			
			v = faceSize*v + 0.5f;
			int vi = Mathf.FloorToInt(v)-1;
			v = Mathf.Repeat(v,1f);
			
			int i00 = toIndexLinked(face,ui,vi);
			int i10 = toIndexLinked(face,ui+1,vi);
			int i11 = toIndexLinked(face,ui+1,vi+1);
			int i01 = toIndexLinked(face,ui,vi+1);
			
			Color c0, c1;			
			c0 = Color.Lerp(pixels[i00],pixels[i10],u);
			c1 = Color.Lerp(pixels[i01],pixels[i11],u);
			dst = Color.Lerp(c0,c1,v);
		}
		
		private static Color[,] cubicKernel = new Color[4,4];
		public void sampleBicubic(ref Color dst, float u, float v, int face) {
			u = faceSize*u + 0.5f;
			int ui = Mathf.FloorToInt(u)-1;
			u = Mathf.Repeat(u,1f);
			
			v = faceSize*v + 0.5f;
			int vi = Mathf.FloorToInt(v)-1;
			v = Mathf.Repeat(v,1f);
			
			for(int x=0;x<4; ++x)
			for(int y=0;y<4; ++y) {
				int index = toIndexLinked(face, ui-1+x, vi-1+y);
				cubicKernel[x,y] = pixels[index];
			}
#if true
			float weight = 0.85f;
			float anchor = 0.333f;
			Color c0,c1,w0,w1,l0,l1,l2;
			for(int y=0;y<4; ++y) {
				w0 = cubicKernel[0,y];
				c0 = cubicKernel[1,y];
				c1 = cubicKernel[2,y];
				w1 = cubicKernel[3,y];
				w0 = Color.Lerp(c0,w0,weight);
				w1 = Color.Lerp(c1,w1,weight);
				w0 = c0 + anchor*(c0 - w0);
				w1 = c1 + anchor*(c1 - w1);
				
				l0 = Color.Lerp(c0,w0,u);
				l1 = Color.Lerp(w0,w1,u);
				l2 = Color.Lerp(w1,c1,u);
				
				l0 = Color.Lerp(l0,l1,u);				
				l1 = Color.Lerp(l1,l2,u);
				
				cubicKernel[0,y] = Color.Lerp(l0,l1,u);
			}
			w0 = cubicKernel[0,0];
			c0 = cubicKernel[0,1];
			c1 = cubicKernel[0,2];
			w1 = cubicKernel[0,3];
			w0 = Color.Lerp(c0,w0,weight);
			w1 = Color.Lerp(c1,w1,weight);
			w0 = c0 + anchor*(c0 - w0);
			w1 = c1 + anchor*(c1 - w1);
			
			l0 = Color.Lerp(c0,w0,v);
			l1 = Color.Lerp(w0,w1,v);
			l2 = Color.Lerp(w1,c1,v);
		
			l0 = Color.Lerp(l0,l1,v);
			l1 = Color.Lerp(l1,l2,v);
			
			dst = Color.Lerp(l0,l1,v);
#else
			float weight = 0.3333f;
			Color c0,c1,w0,w1,l0,l1,l2;
			for(int x=0;x<4; ++x) {
				w0 = cubicKernel[x,0];
				c0 = cubicKernel[x,1];
				c1 = cubicKernel[x,2];
				w1 = cubicKernel[x,3];
				w0 = c0 + weight*(c0 - w0);
				w1 = c1 + weight*(c1 - w1);
				
				l0 = Color.Lerp(c0,w0,v);
				l1 = Color.Lerp(w0,w1,v);
				l2 = Color.Lerp(w1,c1,v);
				
				l0 = Color.Lerp(l0,l1,v);
				l1 = Color.Lerp(l1,l2,v);
				
				cubicKernel[x,0] = Color.Lerp(l0,l1,v);
			}
			c0 = cubicKernel[1,0];
			c1 = cubicKernel[2,0];
			w0 = c0 + weight*(c0 - cubicKernel[0,0]);
			w1 = c1 + weight*(c1 - cubicKernel[3,0]);
			
			l0 = Color.Lerp(c0,w0,u);
			l1 = Color.Lerp(w0,w1,u);
			l2 = Color.Lerp(w1,c1,u);
		
			l0 = Color.Lerp(l0,l1,u);
			l1 = Color.Lerp(l1,l2,u);
			
			dst = Color.Lerp(l0,l1,u);
#endif
		}
		
//////////////////////////////////
		
		//NOTE: resize clears pixel data
		public void resize(int newFaceSize) {
			if( newFaceSize == faceSize ) return;
			faceSize = newFaceSize;
			pixels = null;
			pixels = new Color[faceSize*faceSize*6];
			mset.Util.clearTo(ref pixels, Color.black);
		}
		public void resize(int newFaceSize, Color clearColor) {
			resize(newFaceSize);
			mset.Util.clearTo(ref pixels, clearColor);
		}
		
		public void resample(int newSize) {
			if( newSize == faceSize ) return;
			Color[] newPixels = new Color[newSize*newSize*6];
			resample(ref newPixels, newSize);
			pixels = newPixels;
			faceSize = newSize;
		}
		public void resample(ref Color[] dst, int newSize) {
			int new_faceLength = newSize*newSize;
			float inv_newSize = 1f/(newSize);
			
			for(int face=0; face<6; ++face){
				for(int y=0; y<newSize; ++y){
					float v = ((float)y + 0.5f) * inv_newSize;
					for(int x=0; x<newSize; ++x){
						float u = ((float)x + 0.5f) * inv_newSize;
						int i = new_faceLength*face + y*newSize + x;
						sample(ref dst[i], u, v, face);
					}
				}
			}
		}
		public void resampleFace(ref Color[] dst, int face, int newSize, bool flipY) { resampleFace(ref dst, 0, face, newSize, flipY); }
		public void resampleFace(ref Color[] dst, int dstOffset, int face, int newSize, bool flipY) {
			if( newSize == faceSize ) {
				//copy directly
				pixelCopy(ref dst, dstOffset,  pixels, face*faceSize*faceSize, faceSize*faceSize);
				return;
			}
			float inv_newSize = 1f/(newSize);
			if( flipY ) {
				for(int y=0; y<newSize; ++y){
					float v = 1f - ((float)y + 0.5f) * inv_newSize;
					for(int x=0; x<newSize; ++x){
						float u = ((float)x + 0.5f) * inv_newSize;
						int i = y*newSize + x + dstOffset;
						sample(ref dst[i], u, v, face);
					}
				}
			} else {
				for(int y=0; y<newSize; ++y){
					float v = ((float)y + 0.5f) * inv_newSize;
					for(int x=0; x<newSize; ++x){
						float u = ((float)x + 0.5f) * inv_newSize;
						int i = y*newSize + x + dstOffset;
						sample(ref dst[i], u, v, face);
					}
				}
			}
		}
		
		// input/output
		public void fromCube(Cubemap cube, int mip, ColorMode cubeColorMode, bool useGamma) {
			int mipSize = cube.width >> mip;
			if( pixels == null || faceSize != mipSize ) resize(mipSize);
			for( int face=0; face<6; ++face) {
				Color[] src = cube.GetPixels((CubemapFace)face,mip);
				pixelCopy(ref pixels, face*faceSize*faceSize, src, 0, src.Length);
			}
			decode(ref pixels, pixels, cubeColorMode, useGamma);
		}
		
		public void toCube(ref Cubemap cube, int mip, ColorMode cubeColorMode, bool useGamma) {
			int faceLength = faceSize*faceSize;
			Color[] facePixels = new Color[faceLength];			
			for( int face=0; face<6; ++face) {
				pixelCopy(ref facePixels, 0, pixels, face*faceLength, faceLength);
				encode(ref facePixels, facePixels, cubeColorMode, useGamma);
				cube.SetPixels(facePixels, (CubemapFace)face, mip);
			}
			cube.Apply(false);
		}
		public void resampleToCube(ref Cubemap cube, int mip, ColorMode cubeColorMode, bool useGamma) {
			int mipSize = cube.width >> mip;
			int mipLength = mipSize*mipSize*6;
			Color[] mipPixels = new Color[mipLength];
			
			for( int face=0; face<6; ++face) {
				resampleFace(ref mipPixels, face, mipSize, false);
				encode(ref mipPixels, mipPixels, cubeColorMode, useGamma);
				cube.SetPixels(mipPixels, (CubemapFace)face, mip);
			}
			cube.Apply(false);
		}
		
		public void resampleToBuffer( ref CubeBuffer dst ) {
			int dstFaceLength = dst.faceSize*dst.faceSize;
			for( int face=0; face<6; ++face) {
				resampleFace(ref dst.pixels, face*dstFaceLength, face, dst.faceSize, false);
			}
		}
		
		public void fromBuffer( CubeBuffer src ) {
			clear();
			faceSize = src.faceSize;
			pixels = new Color[src.pixels.Length];
			pixelCopy(ref pixels, 0, src.pixels, 0, pixels.Length);
		}
		
		//TODO: mipmapping here? how come getBilinear doesn't have mipmaps?
		public void fromPanoTexture(Texture2D tex, int _faceSize, ColorMode texColorMode, bool useGamma) {
			resize(_faceSize);
			ulong fsize = (ulong)faceSize;
			for(ulong face=0; face<6; ++face) {
				for(ulong y=0; y<fsize; ++y) {
					for(ulong x=0; x<fsize; ++x) {
						float u = 0f;
						float v = 0f;
						mset.Util.cubeToLatLongLookup(ref u, ref v, face, x, y, fsize);
						//prepare for the horror of repeating textures
						float theclamps = 1f/(float)faceSize;
						v = Mathf.Clamp(v, theclamps, 1f-theclamps);
						pixels[face*fsize*fsize + y*fsize + x] = tex.GetPixelBilinear(u,v);
					}
				}
			}
			decode(ref pixels, pixels, texColorMode, useGamma);
		}
		
		public void fromColTexture(Texture2D tex, ColorMode texColorMode, bool useGamma) 	{ fromColTexture(tex,0,texColorMode,useGamma); }
		public void fromColTexture(Texture2D tex, int mip, ColorMode texColorMode, bool useGamma) {
			if( tex.width*6 != tex.height ) {
				Debug.LogError("CubeBuffer.fromColTexture takes textures of a 1x6 aspect ratio");
				return;
			}
			int newSize = tex.width>>mip;
			if( pixels == null || faceSize != newSize ) {
				resize(newSize);	
			}
			
			Color32[] src = tex.GetPixels32(mip);
			if(!mset.Util.hasAlpha(tex)) clearAlpha(ref src);
			decode(ref pixels, src, texColorMode, useGamma);
		}
		
		public void fromHorizCrossTexture(Texture2D tex, ColorMode texColorMode, bool useGamma) 		{ fromHorizCrossTexture(tex,0,texColorMode,useGamma); }
		public void fromHorizCrossTexture(Texture2D tex, int mip, ColorMode texColorMode, bool useGamma) {
			if( tex.width*3 != tex.height*4 ) {
				Debug.LogError("CubeBuffer.fromHorizCrossTexture takes textures of a 4x3 aspect ratio");
				return;
			}
			int newSize = (tex.width/4)>>mip;
			if( pixels == null || faceSize != newSize ) {
				resize(newSize);
			}
			
			Color32[] crossData = tex.GetPixels32(mip);
			if(!mset.Util.hasAlpha(tex)) { clearAlpha(ref crossData); }
			Color32[] faceData = new Color32[faceSize*faceSize];
			for(int f = 0; f<6; ++f) {
				CubemapFace face = (CubemapFace)f;
				int cross_x = 0; int cross_y = 0;
				int col_offset = f*faceSize*faceSize;
				switch(face) {
					case CubemapFace.NegativeX: cross_x=0; 			cross_y=faceSize*1;	break;
					case CubemapFace.NegativeY: cross_x=faceSize*1;	cross_y=0;			break;
					case CubemapFace.NegativeZ: cross_x=faceSize*3; cross_y=faceSize*1;	break;
					case CubemapFace.PositiveX: cross_x=faceSize*2; cross_y=faceSize*1;	break;
					case CubemapFace.PositiveY: cross_x=faceSize*1; cross_y=faceSize*2;	break;
					case CubemapFace.PositiveZ: cross_x=faceSize*1; cross_y=faceSize*1;	break;
				};
				pixelCopyBlock<Color32>(ref faceData,0,0,faceSize, //dst
										crossData, cross_x, cross_y, faceSize*4, //src
										faceSize, faceSize,	 //block
										true);//flip
				decode(ref pixels, col_offset, faceData, 0, faceSize*faceSize, texColorMode, useGamma);
			}
		}
		
		//TODO: vertical cross
		
		//copies cube face pixels into one long column image
		public void toColTexture(ref Texture2D tex, ColorMode texColorMode, bool useGamma) {
			if( tex.width != faceSize || tex.height != faceSize*6 ) {
				tex.Resize(faceSize, 6*faceSize);
			}
			Color32[] dst = tex.GetPixels32();
			encode(ref dst, pixels, texColorMode, useGamma);
			tex.SetPixels32(dst);
			tex.Apply(false);
		}
		
		//resamples cubemap pixels into a latitude-longitude panorama
		//NOTE: destination texture size determines size of sampling 
		public void toPanoTexture(ref Texture2D tex, ColorMode texColorMode, bool useGamma) {
			ulong w = (ulong)tex.width;			
			ulong h = (ulong)tex.height;
			Color[] dst = tex.GetPixels();
			for(ulong x=0; x<w; ++x) {
				for(ulong y=0; y<h; ++y) {
					float u=0f;
					float v=0f;
					ulong face=0;
					mset.Util.latLongToCubeLookup(ref u, ref v, ref face, x, y, w, h);
					sample(ref dst[y*w+x],u,v,(int)face);
				}
			}
			encode(ref dst, dst, texColorMode, useGamma);
			tex.SetPixels(dst);
			tex.Apply(tex.mipmapCount>1);
		}
		
		//HACK: samples pixels directly to a panorama buffer in full, linear HDR without any color encoding
		public void toPanoBuffer(ref Color[] buffer, int width, int height) {
			ulong w = (ulong)width;			
			ulong h = (ulong)height;
			for(ulong x=0; x<w; ++x) {
				for(ulong y=0; y<h; ++y) {
					float u=0f;
					float v=0f;
					ulong face=0;
					mset.Util.latLongToCubeLookup(ref u, ref v, ref face, x, y, w, h);
					sample(ref buffer[y*w+x],u,v,(int)face);
				}
			}
		}
	};
}

