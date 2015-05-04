using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {

	public float groundSpeed = 10f;
	public float airSpeed = 0.2f;
	public float speedDamp = 0.4f;
	public float jumpForce = 1000;
	public bool grounded = false;
	private GameObject groundCheck;
	public Vector2 groundCheckPosition = new Vector2(0,-0.5f);
	public float groundRadius = 0.2f;
	public LayerMask whatIsGround;
	public Vector2 targetVelocity;
	private WeaponManager weaponManager;

	private PhysicsMaterial2D physMatBouncy;
	private PhysicsMaterial2D physMatRegular;
	private bool hooked;

	void Awake()
	{
		groundCheck = new GameObject();
		groundCheck.transform.name = "GroundCheck";
		groundCheck.transform.parent = transform;
		groundCheck.transform.localPosition = groundCheckPosition;

		weaponManager = gameObject.GetComponent<WeaponManager>();

		physMatBouncy = Resources.Load<PhysicsMaterial2D>("p_BouncyPhysMat");
		physMatRegular = Resources.Load<PhysicsMaterial2D>("p_regularPhysMat");

	//	Physics2D.IgnoreLayerCollision (2,31);
	}
		
	void FixedUpdate()
	{
		grounded = Physics2D.OverlapCircle (groundCheck.transform.position, groundRadius, whatIsGround);

		Vector2 newVelocity;
		newVelocity = new Vector2(Input.GetAxis ("Horizontal"), 0);
		if(newVelocity.magnitude > 1)
			newVelocity.Normalize ();

		if(grounded)
		{
			GetComponent<Rigidbody2D>().velocity += newVelocity * groundSpeed;
			
			float desiredSpeed = GetComponent<Rigidbody2D>().velocity.x;
			
			desiredSpeed = Mathf.Clamp (desiredSpeed, -groundSpeed, groundSpeed);
			GetComponent<Rigidbody2D>().velocity = new Vector2(desiredSpeed, GetComponent<Rigidbody2D>().velocity.y);

			if(Input.GetButtonDown ("Jump"))
			{
				GetComponent<Rigidbody2D>().AddForce (new Vector2(0, jumpForce));
			}
		}
		else if(weaponManager.hook && weaponManager.hookScript.hooked)
		{
			{
				GetComponent<Rigidbody2D>().velocity += newVelocity * airSpeed;
			}
		}

		if(weaponManager.hook && weaponManager.hookScript.hooked)
		{
			ChangeCollMat(physMatBouncy);
		}
		else
		{
			ChangeCollMat(physMatRegular);
		}
	}

	void ChangeCollMat(PhysicsMaterial2D physMat)
	{
		if(gameObject.GetComponent<Collider2D>().sharedMaterial != physMat)
		{
			gameObject.GetComponent<Collider2D>().sharedMaterial = physMat;
			gameObject.GetComponent<Collider2D>().enabled = false;
			gameObject.GetComponent<Collider2D>().enabled = true;
		}
	}
}
