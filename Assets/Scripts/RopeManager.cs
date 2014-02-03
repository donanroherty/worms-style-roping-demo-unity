using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RopeManager : MonoBehaviour {
	
	public WeaponManager weaponManager;
	private SpringJoint2D spring;
	public List<Vector2> anchors;
	public float Length;
	public float linecastOffset = 0.01f;
	public float returnLCDist = 0.2f;
	public LineRenderer LR;
	public float ropeClimbSpeed = .4f;
	public float maxLength = 30;
	
	// Use this for initialization
	void Awake () {
		weaponManager = gameObject.GetComponent<WeaponManager>();

		spring = gameObject.GetComponent<SpringJoint2D>();
		anchors = new List<Vector2>();
		//LR = gameObject.AddComponent<LineRenderer>();
	}
	
	void FixedUpdate () 
	{
		if(weaponManager.hook)
			AddAnchor (weaponManager.hook.transform.position);
		if(weaponManager.hook)
			RopeJointManager();
	}
	//Manages the creation and removal of joints in a rope
	void RopeJointManager()
	{
		//Creates an anchor point when a linecast from player to previous anchor is broken
		RaycastHit2D hit = Physics2D.Linecast (transform.position, anchors[anchors.Count-1], 1>>12);
		if(hit)
		{
			Vector2 anchorPoint = hit.point + (hit.normal.normalized * linecastOffset);
			AddAnchor(anchorPoint);
		}
		//Removes anchors when player has line of sight on the previous anchor
		if(anchors.Count > 1)
		{
			Vector2 ABVector = new Vector2(anchors[anchors.Count - 1].x - transform.position.x, anchors[anchors.Count - 1].y - transform.position.y).normalized;
			Vector2 shortLCStart = anchors[anchors.Count - 1] + (-returnLCDist * ABVector);
			RaycastHit2D returnHitShort = Physics2D.Linecast (shortLCStart, anchors[anchors.Count-2]);
			
			Debug.DrawLine (shortLCStart, anchors[anchors.Count-2], Color.blue);
			
			if (!returnHitShort) 
			{
				KillAnchor();
			}
		} 
		
		spring.distance = Mathf.Clamp ( spring.distance + Input.GetAxis ("Vertical") * -1 * ropeClimbSpeed, 1, maxLength);

		DrawDebugLines();
	}
	void Update()
	{
		//	LineRenderer();

		if(weaponManager.hook)
		{
			//LR.SetPosition (0,hook.transform.position);
			//LR.SetPosition (anchors.Count,transform.position);
		}
	}
	
	void AddAnchor(Vector2 pos)
	{
		anchors.Add (pos);
		SetSpring ();
	}
	void KillAnchor()
	{
		anchors.RemoveAt (anchors.Count-1);
		SetSpring ();
	}
	
	void SetSpring()
	{
		Length = Vector2.Distance (transform.position, anchors[anchors.Count-1]);
		spring.connectedAnchor = anchors[anchors.Count-1];
		spring.distance = Length;
		spring.enabled = true;
		//LineRenderer();
	}
	
	void LineRenderer()
	{
		LR.SetPosition (0,anchors[0]);
		LR.SetVertexCount (anchors.Count+1);
		LR.SetPosition (anchors.Count,transform.position);
		LR.SetPosition (anchors.Count-1, anchors[anchors.Count-1]);
	}
	
	void DrawDebugLines()
	{
		if(anchors.Count==0)
			Debug.DrawLine (transform.position, weaponManager.hook.transform.position, Color.green);
		if(anchors.Count!=0)
			Debug.DrawLine (transform.position, anchors[anchors.Count-1], Color.green);
	}
}
