// Marmoset Skyshop
// Copyright 2013 Marmoset LLC
// http://marmoset.co

using UnityEngine;
using UnityEditor;
using System.Collections;

namespace mset {
	struct PerfMetric {
		private float 	stamp;
		private float 	delta;
		private float 	sum;
		private float	minDelta;
		private float	maxDelta;
		private ulong	count;		
		
		public void reset() {
			stamp = Time.realtimeSinceStartup;
			sum = 0f;
			delta = 0f;
			count = 0;
			minDelta = float.PositiveInfinity;
			maxDelta = float.NegativeInfinity;
		}
		
		public void begin() {
			stamp = Time.realtimeSinceStartup;
		}
		
		public void end() {
			float now = Time.realtimeSinceStartup;
			delta = now - stamp;
			sum += delta;
			if( delta > 0f ) minDelta = Mathf.Min(delta,minDelta);
			maxDelta = Mathf.Max(delta,maxDelta);			
			
			count++;			
			stamp = now;
		}
		
		public string getString( string label, int indent ) {
			string prefix = "";
			for(int i = 0; i<indent; ++i) { prefix += ".   "; }
			
			string str = prefix + "=" + label + "=\n";
			str += prefix + "Delta:\t[" + minDelta + ", " + maxDelta + "]\tAverage: " + (sum/(float)count) + "\n";
			str += prefix + "Step Count:\t" + count + "\n";
			str += prefix + "Total Time:\t" + sum + "\n\n";
			
			return str;
		}
	};
}
