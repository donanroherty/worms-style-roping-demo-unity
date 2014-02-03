// Marmoset Skyshop
// Copyright 2013 Marmoset LLC
// http://marmoset.co

using UnityEngine;
using System.Collections;

namespace mset {
	public enum Corner {
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight
	}
	
	public class Logo : MonoBehaviour {
		public Texture2D logoTexture = null;
		public Color color = Color.white;
		public Vector2 logoPixelOffset = new Vector2(0,0);
		public Vector2 logoPercentOffset = new Vector2(0,0);
		public Corner placement = Corner.BottomLeft;
		private Rect texRect = new Rect(0,0,0,0);
		
		void Reset() {
			logoTexture = Resources.Load("renderedLogo") as Texture2D;
		}
		
		void Start() {
		}
	
		void updateTexRect() {
			if( logoTexture ) {
				float tw = logoTexture.width;
				float th = logoTexture.height;
				float cw = 0f;
				float ch = 0f;
				if( this.camera ) {
					//check attached camera first
					cw = camera.pixelWidth;
					ch = camera.pixelHeight;
				} else if( Camera.main ) {
					//use first camera tagged as MainCamera
					cw = Camera.main.pixelWidth;
					ch = Camera.main.pixelHeight;
				} else if( Camera.current ) {
					//use currently active camera (mostly harmless)
					//cw = Camera.current.pixelWidth;
					//ch = Camera.current.pixelHeight;
				}
				float ox = logoPixelOffset.x + logoPercentOffset.x*cw*0.01f;
				float oy = logoPixelOffset.y + logoPercentOffset.y*ch*0.01f;
				
				switch(placement) {
				case Corner.TopLeft:
					texRect.x = ox;
					texRect.y = oy;
					break;
				case Corner.TopRight:
					texRect.x = cw - ox - tw;
					texRect.y = oy;
					break;
				case Corner.BottomLeft:
					texRect.x = ox;
					texRect.y = ch - oy - th;
					break;
				case Corner.BottomRight:
					texRect.x = cw - ox - tw;
					texRect.y = ch - oy - th;
					break;
				};
				texRect.width = tw;
				texRect.height = th;
			}
		}
		
		void OnGUI() {
			updateTexRect();
			if( logoTexture ) {
				GUI.color = color;
				GUI.DrawTexture(texRect, logoTexture);
			}
		}
	}
}