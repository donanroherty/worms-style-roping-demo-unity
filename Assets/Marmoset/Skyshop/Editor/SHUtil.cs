// Marmoset Skyshop
// Copyright 2013 Marmoset LLC
// http://marmoset.co

using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace mset {
	//Utilities to project vectors and cubemaps onto SH basis vectors
	public class SHUtil {
		static float	project_l0_m0( Vector3 u ) {
			// 1/2 * sqrt(1/pi)
			return	SHEncoding.sEquationConstants[0];
		}
	
		static float	project_l1_mneg1( Vector3 u ) {
			// 1/2 * sqrt(3/pi) * y
			return	SHEncoding.sEquationConstants[1] * u.y;
		}
	
		static float	project_l1_m0( Vector3 u ) {
			// 1/2 * sqrt(3/pi) * z
			return	SHEncoding.sEquationConstants[2] * u.z;
		}
	
		static float	project_l1_m1( Vector3 u ) {
			// 1/2 * sqrt(3/pi) * x
			return	SHEncoding.sEquationConstants[3] * u.x;
		}
	
		static float	project_l2_mneg2( Vector3 u ) {
			// 1/2 * sqrt(15/pi) * y * x
			return	SHEncoding.sEquationConstants[4] * u.y * u.x;
		}
	
		static float	project_l2_mneg1( Vector3 u ) {
			// 1/2 * sqrt(15/pi) * y * z
			return	SHEncoding.sEquationConstants[5] * u.y * u.z;
		}
	
		static float	project_l2_m0( Vector3 u ) {
			// 1/4 * sqrt(5/pi) * (3*z^2 - 1)
			return	SHEncoding.sEquationConstants[6] * (3f*u.z*u.z - 1f);
		}
	
		static float	project_l2_m1( Vector3 u ) {
			// 1/2 * sqrt(15/pi) * z * x
			return	SHEncoding.sEquationConstants[7] * u.z * u.x;
		}
	
		static float	project_l2_m2( Vector3 u ) {
			// 1/4 * sqrt(15/pi) * (x^2 - y^2)
			return	SHEncoding.sEquationConstants[8] * (u.x*u.x - u.y*u.y);
		}

		static void scale( ref SHEncoding sh, float s ) {
			for(int i=0; i<27; ++i) { sh.c[i] *= s; }
		}

		public static void projectCubeBuffer(ref SHEncoding sh, CubeBuffer cube) {
			sh.clearToBlack();
			float totalarea = 0f;
			ulong faceSize = (ulong)cube.faceSize;			
			float[] dc = new float[9];
			Vector3 u = Vector3.zero;
			
			for(ulong face=0; face<6; ++face)
			for(ulong y=0; y<faceSize; ++y)
			for(ulong x=0; x<faceSize; ++x) {
				//compute cube direction
				float areaweight = 1f;
				mset.Util.invCubeLookup(ref u, ref areaweight, face, x, y, faceSize);				
				float shscale = 4f / 3f;
				ulong index = face*faceSize*faceSize + y*faceSize + x;
				Color rgba = cube.pixels[index];
				
				//project on basis functions, and accumulate					
				dc[0] = project_l0_m0(u);

				dc[1] = project_l1_mneg1(u);
				dc[2] = project_l1_m0(u);
				dc[3] = project_l1_m1(u);
				
				dc[4] = project_l2_mneg2(u);
				dc[5] = project_l2_mneg1(u);
				dc[6] = project_l2_m0(u);
				dc[7] = project_l2_m1(u);
				dc[8] = project_l2_m2(u);
				for(int i=0; i<9; ++i ) {
					sh.c[3*i + 0] += shscale * areaweight * rgba[0] * dc[i];
					sh.c[3*i + 1] += shscale * areaweight * rgba[1] * dc[i];
					sh.c[3*i + 2] += shscale * areaweight * rgba[2] * dc[i];
				}
				totalarea += areaweight;
			}

			//complete the integration by dividing by total area
			scale( ref sh, 16f / totalarea );
		}

		public static void convolve(ref SHEncoding sh) { convolve(ref sh, 1f, 2f/3f, 0.25f); }
		public static void convolve(ref SHEncoding sh, float conv0, float conv1, float conv2) {
			for( int i=0; i<27; ++i ) {
				if(i<3)			sh.c[i] *= conv0;
				else if(i<12)	sh.c[i] *= conv1;
				else 			sh.c[i] *= conv2;
			}
		}
	};
}
