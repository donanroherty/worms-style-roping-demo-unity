// Marmoset Skyshop
// Copyright 2013 Marmoset LLC
// http://marmoset.co

using UnityEngine;
using System;

namespace mset {
	class QPow {	
		// specular pow delegates
		public delegate float PowFunc(float f);
		
		public static float Pow1(float f) {
			return f;
		}
		public static float Pow2(float f) {
			return f*f;
		}
		public static float Pow4(float f) {
			f*=f;
			return f*f;
		}
		public static float Pow8(float f) {
			f*=f;
			f*=f;
			return f*f;
		}
		public static float Pow16(float f) {
			f*=f;
			f*=f;
			f*=f;
			return f*f;
		}
		public static float Pow32(float f) {
			f*=f;
			f*=f;
			f*=f;
			f*=f;
			return f*f;
		}
		public static float Pow64(float f) {
			f*=f;
			f*=f;
			f*=f;
			f*=f;
			f*=f;
			return f*f;
		}
		public static float Pow128(float f) {
			f*=f;
			f*=f;
			f*=f;
			f*=f;
			f*=f;
			f*=f;
			return f*f;
		}
		public static float Pow256(float f) {
			f*=f;
			f*=f;
			f*=f;
			f*=f;
			f*=f;
			f*=f;
			f*=f;
			return f*f;
		}
		public static float Pow512(float f) {
			f*=f;
			f*=f;
			f*=f;
			f*=f;
			f*=f;
			f*=f;
			f*=f;
			f*=f;
			return f*f;
		}
		public static PowFunc closestPowFunc(int exp) {
			if( exp+128 >= 512 ) 		return Pow512;
			else if( exp+64 >= 256 ) 	return Pow256;
			else if( exp+32 >= 128 ) 	return Pow128;
			else if( exp+16 >= 64 ) 	return Pow64;
			else if( exp+8 >= 32 ) 		return Pow32;
			else if( exp+4 >= 16 ) 		return Pow16;
			else if( exp+2 >= 8 )		return Pow8;
			else if( exp+1 >= 4 ) 		return Pow4;
			else if( exp >= 2 ) 		return Pow2;
			return Pow1;
		}
		
		public static int Log2i(int val) {
			int log = 0;
			while( val > 0 ) {
				log++;
				val = val >> 1;
			}
			return log;
		}
		
		public static int Log2i(ulong val) {
			int log = 0;
			while( val > 0 ) {
				log++;
				val = val >> 1;
			}
			return log;
		}
	}
}