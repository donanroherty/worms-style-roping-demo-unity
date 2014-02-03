using UnityEngine;
using System.Collections;

public class AimLogic : MonoBehaviour {

	private MeshRenderer meshRenderer;


	
	// Use this for initialization
	void Start () 
	{
		meshRenderer = gameObject.GetComponent<MeshRenderer>();
		meshRenderer.enabled = false;
	}
	
	// Update is called once per frame
	void Update () 
	{
		Vector3 aimVector = new Vector2(Input.GetAxis ("RightHorizontal"), Input.GetAxis ("RightVertical"));

		//If player aims, activate crosshair and set rotation
		if(Input.GetAxis ("RightHorizontal") != 0 || Input.GetAxis ("RightVertical") != 0)
		{
			transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, Mathf.Atan2(aimVector.x *-1, aimVector.y * -1) * Mathf.Rad2Deg);
			meshRenderer.enabled = true;
		}
		else
			//Deactivate crosshair when not aiming;
			meshRenderer.enabled = false;
	}


}
