using UnityEngine;
using System.Collections;

public class PlayerStateManager : MonoBehaviour {

	private GameObject cpManager;
	private CheckpointManager cpMgrScript;
	public int activeCP;

	public WeaponManager weaponManager;

	void Start()
	{
		cpManager = GameObject.Find ("_checkpointmanager");
		cpMgrScript = cpManager.gameObject.GetComponent<CheckpointManager>();
		activeCP = 0;

		Respawn();
	}
	
	public void KillPlayer()
	{
		weaponManager = gameObject.GetComponent<WeaponManager>();
		weaponManager.DestroyHook ();
		rigidbody2D.velocity = new Vector2(0,0);
		Respawn();
	}
	void Respawn()
	{
		cpManager = GameObject.Find ("_checkpointmanager");
		cpMgrScript = cpManager.gameObject.GetComponent<CheckpointManager>();

		transform.position = cpMgrScript.checkPointArray[activeCP].transform.position + new Vector3(0,1,0);
	}

}
