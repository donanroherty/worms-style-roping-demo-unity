using UnityEngine;
using System.Collections;

public class CheckpointLogic : MonoBehaviour {

	public int checkpointID;
	private GameObject cpManager;
	private CheckpointManager cpMgrScript;

	void Awake()
	{
		cpManager = GameObject.Find ("_checkpointmanager");
		cpMgrScript = cpManager.gameObject.GetComponent<CheckpointManager>();
	}

	void OnTriggerEnter2D(Collider2D other)
	{
		if(!other.gameObject.CompareTag ("Player"))
			return;
		
		if(other.gameObject.tag == "Player")
		{
			PlayerStateManager pSM = other.GetComponent<PlayerStateManager>();
			GetCheckpoint(pSM);

			if(checkpointID == pSM.activeCP)
				SetMaterial ();
		}
	}

	void GetCheckpoint(PlayerStateManager pSM)
	{
		if(pSM.activeCP == checkpointID)
		{
			Debug.Log ("Same CheckPoint");
		}
		else if(pSM.activeCP+1 == checkpointID)
		{
			Debug.Log ("New CheckPoint Reached!!!!!!!!");
			CycleCheckpoints (pSM);
		}
		else if(pSM.activeCP == cpMgrScript.checkPointArray.Count-1 && checkpointID == 0)
			CycleCheckpoints (pSM);
	}

	void CycleCheckpoints(PlayerStateManager pSM)
	{
		pSM.activeCP++;
		
		if(pSM.activeCP == cpMgrScript.checkPointArray.Count)
		{
			pSM.activeCP = 0;
		}
	}

	void SetMaterial()
	{
		for(int i=0;i<cpMgrScript.checkPointArray.Count;i++)
		{

			cpMgrScript.checkPointArray[i].GetComponentInChildren<MeshRenderer>().material = Resources.Load ("Materials/mat_CheckpointInactive") as Material;
			cpMgrScript.checkPointArray[i].GetComponentInChildren<Light>().enabled = false;
			cpMgrScript.checkPointArray[i].GetComponentInChildren<LensFlare>().enabled = false;

		}
		gameObject.GetComponentInChildren<MeshRenderer>().material = Resources.Load ("Materials/mat_CheckpointActive") as Material;
		gameObject.GetComponentInChildren<Light>().enabled = true;
		gameObject.GetComponentInChildren<LensFlare>().enabled = true;

	}
}
