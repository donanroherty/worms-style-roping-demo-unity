using UnityEngine;
using System.Collections;

public class Projectile : MonoBehaviour {

	//public float speed; 
	public GameObject owner;
	protected WeaponManager weaponManager;
	protected  float speed;
	public int layerPlayer = 8;
	public int layerHook = 9;

	protected virtual void Awake(){
	}

	// Use this for initialization
	protected virtual void Start () {
		weaponManager = owner.GetComponent<WeaponManager>();

	}
}
