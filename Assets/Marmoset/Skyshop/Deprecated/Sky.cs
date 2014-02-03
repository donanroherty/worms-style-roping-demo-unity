// Marmoset Skyshop
// Copyright 2013 Marmoset LLC
// http://marmoset.co

using UnityEngine;
using System.Collections;

//This is a deprecated version of the mset.Sky script in the global namespace. 
//It will go away in future releases of Skyshop, but is here to facilitate a
//clean transition to the mset namespace and the upgrade scripts found in
//SkyshopUtil.cs.

public class Sky : mset.Sky {
	public void OnValidate() {
		Debug.LogWarning("Skyshop sky \"" + this.gameObject.name + "\" is using a deprecated script. Please Run the \"Edit->Skyshop->Upgrade Skies\" macro on this scene.");
	}
	
	new public static Sky activeSky {
		set {
			Debug.LogError("Trying to access Sky.activeSky in the global namespace (deprecated script). Use mset.Sky.activeSky instead.");
		}
		get {
			Debug.LogError("Trying to access Sky.activeSky in the global namespace (deprecated script). Use mset.Sky.activeSky instead.");
			return null;
		}
	}
}