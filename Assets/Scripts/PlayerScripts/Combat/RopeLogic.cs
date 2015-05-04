using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RopeLogic : Projectile {
	
	public bool hooked;
	public GameObject hook;
	private SpringJoint2D spring;
	public List<Vector2> anchors;
	public float linecastOffset = 0.01f;
	public float returnLCDist = 0.2f;
	private LineRenderer LR;
	public float combinedAnchorLen;
	public float totalLength;
	
	// Use this for initialization
	protected override void Start () {
		base.Start ();

		speed = weaponManager.ropeHookSpeed;
		transform.name = "RopeHook";
		Physics2D.IgnoreLayerCollision (layerPlayer, layerHook, true);
		GetComponent<Rigidbody2D>().velocity = transform.TransformDirection(Vector3.up * speed);
		GetComponent<Rigidbody2D>().drag = weaponManager.ropeHookSpeedDamp;
		
		spring = owner.GetComponent<SpringJoint2D>();
		spring.enableCollision = true;
		anchors = new List<Vector2>();
		LR = gameObject.GetComponent<LineRenderer>();
		LR.SetVertexCount (2);
	}
	
	void FixedUpdate () 
	{
		if(!hooked)
		{
			EarlyHookCheck();

			if(Vector2.Distance (owner.transform.position, transform.position) > weaponManager.maxLength)
				weaponManager.DestroyHook ();
		}
		
		if(hooked && anchors.Count == 0)
			AddAnchor (transform.position);
		
		if(anchors.Count > 0)
			RopeJointManager();

		float allowedDistance = weaponManager.maxLength - combinedAnchorLen;
		spring.distance = Mathf.Clamp ( spring.distance + Input.GetAxis ("Vertical") * -1 * weaponManager.ropeClimbSpeed, 1, allowedDistance);
	}
	void EarlyHookCheck()
	{
		Debug.DrawLine (owner.transform.position, transform.position);
		RaycastHit2D hit = Physics2D.Linecast (owner.transform.position, transform.position);

		if(hit && hit.collider.gameObject.CompareTag ("Hookable"))
		{
			transform.position = hit.point + (hit.normal.normalized * linecastOffset);
			ProcessHit ();
		}
		else
			return;
	}
	//Manages the creation and removal of joints in a rope
	void RopeJointManager()
	{
		//Creates an anchor point when a linecast from player to previous anchor is broken
		RaycastHit2D hit = Physics2D.Linecast (owner.transform.position, anchors[anchors.Count-1]);

		if(hit && hit.collider.gameObject.CompareTag ("Hookable"))
		{
			Vector2 anchorPoint = hit.point + (hit.normal.normalized * linecastOffset);
			AddAnchor(anchorPoint);
		}

		//Removes anchors when player has line of sight on the previous anchor
		if(anchors.Count > 1)
		{
			Vector2 ABVector = new Vector2(anchors[anchors.Count - 1].x - owner.transform.position.x, anchors[anchors.Count - 1].y - owner.transform.position.y).normalized;
			Vector2 shortLCStart = anchors[anchors.Count - 1] + (-returnLCDist * ABVector);
			RaycastHit2D returnHitShort = Physics2D.Linecast (shortLCStart, anchors[anchors.Count-2]);
			
			if (!returnHitShort) 
			{
				KillAnchor();
			}
		}

		LR.SetPosition (0,transform.position);
		if(!hooked)
			LR.SetPosition (anchors.Count+1,owner.transform.position);
		else
			LR.SetPosition (anchors.Count,owner.transform.position);
	}
	
	void AddAnchor(Vector2 pos)
	{
		anchors.Add (pos);
		if(anchors.Count > 1)
		{
			combinedAnchorLen += Vector2.Distance (anchors[anchors.Count-1], anchors[anchors.Count-2]);
			combinedAnchorLen = Mathf.Round (combinedAnchorLen * 100f) / 100f;
		}
		SetSpring ();
		
	}
	void KillAnchor()
	{
		if(anchors.Count > 1)
		{
			combinedAnchorLen -= Vector2.Distance (anchors[anchors.Count-1], anchors[anchors.Count-2]);
			combinedAnchorLen = Mathf.Round (combinedAnchorLen * 100f) / 100f;
		}
		
		anchors.RemoveAt (anchors.Count-1);
		
		SetSpring ();
	}
	
	void SetSpring()
	{
		float dist = Vector2.Distance (owner.transform.position, anchors[anchors.Count-1]);
	
		spring.connectedAnchor = anchors[anchors.Count-1];
		spring.distance = dist;
		spring.enabled = true;
		LineRenderer();
	}
	
	void LineRenderer()
	{
		LR.SetPosition (0,anchors[0]);
		LR.SetVertexCount (anchors.Count+1);
		LR.SetPosition (anchors.Count,owner.transform.position);
		LR.SetPosition (anchors.Count-1, anchors[anchors.Count-1]);
	}
	
	void OnCollisionEnter2D(Collision2D col) 
	{
		if(!col.gameObject.CompareTag ("Hookable"))
			return;
		else
		{
			ProcessHit();
		}
	}

	void ProcessHit()
	{
		hooked = true;
		GetComponent<Collider2D>().enabled = false;
		//rigidbody2D.isKinematic = true;
		Destroy (GetComponent<Rigidbody2D>());
	}
}
