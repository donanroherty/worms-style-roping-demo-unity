using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CheckpointManager : MonoBehaviour {

	public List<GameObject> checkPointArray;

	private GameObject playerMgr;
	
	// Use this for initialization
	void Awake () 
	{
		for(int i=0; i< checkPointArray.Count;i++)
		{
			CheckpointLogic cpLogic = checkPointArray[i].GetComponent<CheckpointLogic>();
			cpLogic.checkpointID = i;
		}

	}
}
