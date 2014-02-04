using UnityEngine;
using System.Collections;

public class PlayerStateManager : MonoBehaviour {

	public GameObject lastCheckpoint;
	public WeaponManager weaponManager;

	public void ManageKillZ()
	{
		weaponManager = gameObject.GetComponent<WeaponManager>();
		weaponManager.DestroyHook ();

		transform.position = lastCheckpoint.transform.position + new Vector3(0,1,0);
		rigidbody2D.velocity = new Vector2(0,0);
	}

}
