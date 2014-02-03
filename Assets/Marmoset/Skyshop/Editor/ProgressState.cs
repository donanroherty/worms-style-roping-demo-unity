// Marmoset Skyshop
// Copyright 2013 Marmoset LLC
// http://marmoset.co

using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace mset
{
	public struct DirLookup {
		public float x;
		public float y;
		public float z;
		public float weight;
	};
	//ghetto-yield state. Stores the state of everything in the computation required to pick up where we left off next frame.
	struct ProgressState {
		//iterators
		public ulong curr;
		
		//play/pause/break logic
		private bool _running;
		public ulong total;
		public ulong stepsPerFrame;
		public bool pendingRepaint;
		
		//data
		public Color[]		lights;
		public DirLookup[]	lightDirs;
		public ulong		lightCount;
		
		public CubeBuffer	IN;
		public CubeBuffer	CONVO;	//convolution buffer
		
		//math state
		public Color LColor;
		public float exposure;
		public int   exponent;
		
		public ulong exponentCount;
		public DirLookup	inLookup;
		public DirLookup	outLookup;
		public QPow.PowFunc exponentFunc;
		public bool  		gammaCompress;
		
		//ui bindings
		public bool buildMipChain;
		public bool reflectionInSIM;
		
		public PerfMetric totalMetric;
		public PerfMetric blockMetric;
		public PerfMetric initMetric;		
		public PerfMetric passMetric;
		public PerfMetric passWriteMetric;
		public PerfMetric repaintMetric;
		public PerfMetric finishMetric;
				
		public void setOpCount( ulong stepsInFrame, ulong totalSteps ) {
			//every GUI frame computes this many pixels (there is an overhead to high repaint frequency)
			stepsPerFrame = stepsInFrame;
			total = totalSteps;
		}
		
		public void init() {
			CONVO = new CubeBuffer();
			IN = new CubeBuffer();
		}
		
		public void reset() {
			curr = 0;
			_running = false;
			total = 0;
			stepsPerFrame = 1;
			pendingRepaint = false;
			
			LColor = Color.black;
			exposure = 1f;
			exponent = 1;
			lights = null;
			lightDirs = null;
			lightCount = 0;
			
			buildMipChain = true;
			reflectionInSIM =false;
			gammaCompress = true;
			
			inLookup.x = inLookup.y = inLookup.z = 0f;
			inLookup.weight = 0f;
			
			outLookup.x = outLookup.y = outLookup.z = 0f;
			outLookup.weight = 0f;
			
			//initMetric.reset();
			//totalMetric.reset();
			blockMetric.reset();
			passMetric.reset();
			passWriteMetric.reset();
			repaintMetric.reset();
			finishMetric.reset();
		}
		
		public void play()			{ _running = true; }
		public void pause()			{ _running = false; }
		public bool isPlaying()		{ return _running; }
		public void next()			{ curr++; if(curr%stepsPerFrame == 0) pendingRepaint = true; }	
		public void skip(ulong count){
			//if we pass a repaint point, flag it
			if( ((curr%stepsPerFrame) + count) >= stepsPerFrame ) pendingRepaint = true; 
			curr += count;
		}
		public bool done()			{ return curr >= total; }
		public bool needsRepaint()	{ return pendingRepaint; }
		public float progress()		{ return (float)curr / (float)total; }
	};
}
