using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class RopeHook : Projectile {

	public bool hooked = false;
	public float distance;

	private SpringJoint2D spring;
	public List<Vector2> anchors;
	public float Length;
	public float linecastOffset = 0.01f;
	public float returnLCDist = 0.2f;
	public LineRenderer LR;
	public float ropeClimbSpeed = .4f;
	public float maxLength = 30;


	protected override void Awake()
	{
		base.Awake ();
	}

	protected override void Start () 
	{
		base.Start ();
		speed = weaponManager.ropeHookSpeed;
		transform.name = "RopeHook";

		Physics2D.IgnoreLayerCollision (layerPlayer, layerHook, true);
		rigidbody2D.velocity = transform.TransformDirection(Vector3.up * speed);

		spring = owner.GetComponent<SpringJoint2D>();
		anchors = new List<Vector2>();
	}

	void OnCollisionEnter2D(Collision2D col) 
	{
		if(col.gameObject.tag == "Hookable")
		{
			Debug.Log ("Collided");
			hooked = true;
			collider2D.enabled = false;
			rigidbody2D.isKinematic = true;
			AddAnchor (transform.position);
		}
	}

	void FixedUpdate () 
	{
		if(hooked)
			//RopeJointManager();

		DrawDebugLines();
	}
	/*
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
		

	}*/

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
		LR.SetPosition (anchors.Count,owner.transform.position);
		LR.SetPosition (anchors.Count-1, anchors[anchors.Count-1]);
	}
	
	void DrawDebugLines()
	{
		if(anchors.Count==0)
			Debug.DrawLine (owner.transform.position, transform.position, Color.green);
		if(anchors.Count!=0)
			Debug.DrawLine (owner.transform.position, anchors[anchors.Count-1], Color.green);
	}
	void DestroySelf()
	{
		Destroy (gameObject);
	}
}
