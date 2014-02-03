// Marmoset Skyshop
// Copyright 2013 Marmoset LLC
// http://marmoset.co

using UnityEngine;
using System;

namespace mset {
	public enum ColorMode {
		RGB8,
		RGBM8,
		RGBE8
	};
	public enum ColorModeLabel {
		LDR,
		HDR,
	};
	public class Gamma {
		public static float toLinear = 2.2f;
		public static float toSRGB = 1f/2.2f;
	};
	public class RGB {
		/*
		// Takes a linear, floating-point HDR color and encodes it into an 8-bits-per-channel RGBM color.
		// If gammaRGBM is true, rgbm is encoded in sRGB space (gamma of 2.2), and should be sampled
		// using sRGB sampling in the shader.
		public static void toRGBM(ref Color32 rgbm, Color color, bool useGamma) {
			color *= 1f/6f;
			float m = Mathf.Max(Mathf.Max(color.r,color.g),color.b);
			m = Mathf.Clamp01(m);
			m = Mathf.Ceil(m*255f)/255f;			
			if( m > 0f ) {
				float inv_m = 1f/m;
				color.r = Mathf.Clamp01(color.r*inv_m);
				color.g = Mathf.Clamp01(color.g*inv_m);
				color.b = Mathf.Clamp01(color.b*inv_m);
			
				if( useGamma ) {
					color.r = Mathf.Pow(color.r,Gamma.toSRGB);
					color.g = Mathf.Pow(color.g,Gamma.toSRGB);
					color.b = Mathf.Pow(color.b,Gamma.toSRGB);
				}
				
				rgbm.r = (byte)(color.r*255f);
				rgbm.g = (byte)(color.g*255f);
				rgbm.b = (byte)(color.b*255f);
				rgbm.a = (byte)(m*255f);
			} else {
				rgbm.r = rgbm.g = rgbm.b = rgbm.a = 0;
			}
		}
		public static void toRGBM(ref Color rgbm, Color color, bool useGamma) {
			color *= 1f/6f;
			float m = Mathf.Max(Mathf.Max(color.r,color.g),color.b);
			m = Mathf.Clamp01(m);
			m = Mathf.Ceil(m*255f)/255f;
			if( m > 0f ) {
				float inv_m = 1f/m;
				rgbm.r = Mathf.Clamp01(color.r*inv_m);
				rgbm.g = Mathf.Clamp01(color.g*inv_m);
				rgbm.b = Mathf.Clamp01(color.b*inv_m);
				rgbm.a = m;
				if( useGamma ) {
					rgbm.r = Mathf.Pow(rgbm.r,Gamma.toSRGB);
					rgbm.g = Mathf.Pow(rgbm.g,Gamma.toSRGB);
					rgbm.b = Mathf.Pow(rgbm.b,Gamma.toSRGB);
				}
			} else {
				rgbm.r = rgbm.g = rgbm.b = rgbm.a = 0f;
			}
		}
		// Takes an 8-bits-per-channel RGBM color and decodes it into a floating-point HDR color.
		// If gammaRGBM is true, the RGBM color was encoded with a gamma curve which has to be undone
		// first (this is what sRGB sampling in the shader would do as well).
		// All return values are in linear space.
		public static void fromRGBM(ref Color color, Color32 rgbm, bool useGamma) {
			float inv_byte = 1f/255f;
			float m = (float)rgbm.a*inv_byte*6f;
			color.r = (float)rgbm.r*inv_byte;
			color.g = (float)rgbm.g*inv_byte;
			color.b = (float)rgbm.b*inv_byte;
			
			if( useGamma ) {
				color.r = Mathf.Pow(color.r,Gamma.toLinear);
				color.g = Mathf.Pow(color.g,Gamma.toLinear);
				color.b = Mathf.Pow(color.b,Gamma.toLinear);
			}
			
			color *= m;
			color.a = 1f;
		}
		public static void fromRGBM(ref Color color, Color rgbm, bool useGamma) {
			float m = rgbm.a * 6f;
			
			if( useGamma ) {
				color.r = Mathf.Pow(rgbm.r,Gamma.toLinear);
				color.g = Mathf.Pow(rgbm.g,Gamma.toLinear);
				color.b = Mathf.Pow(rgbm.b,Gamma.toLinear);
			} else {
				color = rgbm;
			}
			
			color *= m;
			color.a = 1f;
		}
		*/
		public static void toRGBM(ref Color32 rgbm, Color color, bool useGamma) {
			if( useGamma ) {
				color.r = Mathf.Pow(color.r,Gamma.toSRGB);
				color.g = Mathf.Pow(color.g,Gamma.toSRGB);
				color.b = Mathf.Pow(color.b,Gamma.toSRGB);
			}
			color *= 1f/6f;
			float m = Mathf.Max(Mathf.Max(color.r,color.g),color.b);
			m = Mathf.Clamp01(m);
			m = Mathf.Ceil(m*255f)/255f;			
			if( m > 0f ) {
				float inv_m = 1f/m;
				color.r = Mathf.Clamp01(color.r*inv_m);
				color.g = Mathf.Clamp01(color.g*inv_m);
				color.b = Mathf.Clamp01(color.b*inv_m);
				
				rgbm.r = (byte)(color.r*255f);
				rgbm.g = (byte)(color.g*255f);
				rgbm.b = (byte)(color.b*255f);
				rgbm.a = (byte)(m*255f);
			} else {
				rgbm.r = rgbm.g = rgbm.b = rgbm.a = 0;
			}
		}
		public static void toRGBM(ref Color rgbm, Color color, bool useGamma) {
			//apply a gamma curve to the HDR values to bring more range into
			//the 0-6 range and keep more precision in the lower end.
			if( useGamma ) {
				color.r = Mathf.Pow(color.r,Gamma.toSRGB);
				color.g = Mathf.Pow(color.g,Gamma.toSRGB);
				color.b = Mathf.Pow(color.b,Gamma.toSRGB);
			}
			//divide by a constant. Note: this happens in gamma-space so the
			//linear values are getting divided by 6^2.2 = ~51.5
			color *= 1f/6f;
			float m = Mathf.Max(Mathf.Max(color.r,color.g),color.b);
			m = Mathf.Clamp01(m);
			m = Mathf.Ceil(m*255f)/255f;
			if( m > 0f ) {
				float inv_m = 1f/m;
				rgbm.r = Mathf.Clamp01(color.r*inv_m);
				rgbm.g = Mathf.Clamp01(color.g*inv_m);
				rgbm.b = Mathf.Clamp01(color.b*inv_m);
				rgbm.a = m;
			} else {
				rgbm.r = rgbm.g = rgbm.b = rgbm.a = 0f;
			}
		}
		public static void fromRGBM(ref Color color, Color32 rgbm, bool useGamma) {
			float inv_byte = 1f/255f;
			float m = (float)rgbm.a*inv_byte;
			color.r = (float)rgbm.r*inv_byte;
			color.g = (float)rgbm.g*inv_byte;
			color.b = (float)rgbm.b*inv_byte;
			//multiply RGB by M and 6 in gamma-space. Note: this is the same as
			//doing toLinear(RGB) and multipling by toLinear(M)*51.5
			color *= m;
			color *= 6;
			if( useGamma ) {
				color.r = Mathf.Pow(color.r,Gamma.toLinear);
				color.g = Mathf.Pow(color.g,Gamma.toLinear);
				color.b = Mathf.Pow(color.b,Gamma.toLinear);
			}
			color.a = 1f;
		}
		public static void fromRGBM(ref Color color, Color rgbm, bool useGamma) {
			float m = rgbm.a;
			color = rgbm;
			//multiply RGB by M and 6 in gamma-space. Note: this is the same as
			//doing toLinear(RGB) and multipling by toLinear(M)*51.5
			color *= m;
			color *= 6;
			if( useGamma ) {
				color.r = Mathf.Pow(color.r,Gamma.toLinear);
				color.g = Mathf.Pow(color.g,Gamma.toLinear);
				color.b = Mathf.Pow(color.b,Gamma.toLinear);
			}
			color.a = 1f;
		}
		
		public static void fromXYZ(ref Color rgb, Color xyz) {
			rgb.r = 3.2404542f*xyz.r - 1.5371385f*xyz.g - 0.4985314f*xyz.b;
			rgb.g = -.9692660f*xyz.r + 1.8760108f*xyz.g + 0.0415560f*xyz.b;
			rgb.b = 0.0556434f*xyz.r - 0.2040259f*xyz.g + 1.0572252f*xyz.b;
		}	
		public static void toXYZ(ref Color xyz, Color rgb) {
			xyz.r = 0.4124564f*rgb.r + 0.3575761f*rgb.g + 0.1804375f*rgb.b;
			xyz.g = 0.2126729f*rgb.r + 0.7151522f*rgb.g + 0.0721750f*rgb.b;
			xyz.b = 0.0193339f*rgb.r + 0.1191920f*rgb.g + 0.9503041f*rgb.b;
		}
		
		public static void toRGBE(ref Color32 rgbe, Color color) {
			float m = Mathf.Max(Mathf.Max(color.r,color.g),color.b);
			int e = Mathf.CeilToInt(Mathf.Log(m,2f));
			e = Mathf.Clamp(e,-128,127);
			m = Mathf.Pow(2f,e);
			float inv_m = 255f/m;
			rgbe.r = (byte)Mathf.Clamp(color.r*inv_m,0f,255f);
			rgbe.g = (byte)Mathf.Clamp(color.g*inv_m,0f,255f);
			rgbe.b = (byte)Mathf.Clamp(color.b*inv_m,0f,255f);
			rgbe.a = (byte)(e+128);
		}
	};
}

