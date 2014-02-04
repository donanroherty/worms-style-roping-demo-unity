using UnityEngine;
using System.Collections;

public class KillZ : MonoBehaviour {

	void Start()
	{

	}
	
void OnTriggerEnter2D(Collider2D col) 
	{
		if(col.gameObject.tag == "Player")
		{
			PlayerStateManager pSM = col.GetComponent<PlayerStateManager>();
			pSM.KillPlayer();
		}
	}
}
