using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CheckpointManager : MonoBehaviour {

	public List<GameObject> checkPointArray;
	public GameObject checkpointsGO;

	// Use this for initialization
	void Start () 
	{
		checkPointArray = new List<GameObject>();
	}

	void OnTriggerEnter2D(Collider2D other) 
	{
		if(!other.gameObject.CompareTag ("Player"))
			return;

		if(other.gameObject.tag == "Player")
		{
			PlayerStateManager pSM = other.GetComponent<PlayerStateManager>();
			if(pSM.lastCheckpoint == gameObject)
				return;

			pSM.lastCheckpoint = gameObject;
			Debug.Log ("New CheckPoint Reached!!!!!!!!");
		}
	}
	

}
