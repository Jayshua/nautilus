using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

[RequireComponent (typeof(Rigidbody))]
public class Ship : NetworkBehaviour
{
	const float LEMON_JUICE_MULTIPLIER = 1.25f;
	const float WIND_BUCKET_MULTIPLIER = 1.5f;
	const float CANNON_SHOT_MULTIPLIER = 1.5f;

	[Header ("Ship Stat")]
	[SerializeField]
	float maxHealth;
	[SerializeField]
	float speed;
	[SerializeField]
	float upperSpeedLimit;
	[SerializeField]
	float damage;
	[SerializeField]
	float currentHealth;
	float backwardSpeed;
	float speedMultiplier = 1f;
	float damageMultiplier = 1f;

	[Header ("Wheels")]
	[SerializeField]
	WheelCollider wheelFR;
	[SerializeField]
	WheelCollider wheelFL;
	[SerializeField]
	WheelCollider wheelBR;
	[SerializeField]
	WheelCollider wheelBL;
	[SerializeField]
	float steerMultiplier;

	[Header ("Projectile")]
	[SerializeField]
	GameObject cannonBallPrefab;
	[SerializeField]
	Transform[] shotPositionLeft;
	[SerializeField]
	Transform[] shotPositionRight;
	[SerializeField]
	float projectileSpeed;
	[SerializeField]
	float projectilesOffset;
	[SerializeField]
	float cooldown;
	float elapsedTimeOnRight = 0f;
	float elapsedTimeOnLeft = 0f;

	Rigidbody rb;
	RectTransform healthBar;
	UserInterface userInterface;

	public Action<Ship> OnKeel;
	public Action<Chest> OnChestGet;

	void Awake ()
	{
		backwardSpeed = speed / 2f;
		rb = GetComponent<Rigidbody> ();
		currentHealth = maxHealth;
	}

	void Start ()
	{
		userInterface = GameObject.Find ("User Interface").GetComponent<UserInterface> ();
	}

	public override void OnStartAuthority ()
	{
		GameObject.Find ("Main Camera").GetComponent<CameraFollow> ().PlayerCreated (this.transform);
	}

	void FixedUpdate ()
	{
		if (!this.hasAuthority)
			return;
		Movement ();
		Fire ();
	}

	void Movement ()
	{
		float moveHorizontal = Input.GetAxis ("Horizontal");
		float moveVertical = Input.GetAxis ("Vertical");
		rb.drag = 0f;

		if (moveVertical > 0) {
			if (wheelBR.motorTorque < 0) {
				rb.drag = 24f;
			}
			if (rb.velocity.magnitude > upperSpeedLimit) {
				rb.drag = 2f;
			}
			wheelBR.motorTorque = moveVertical * speed * speedMultiplier;
			wheelBL.motorTorque = moveVertical * speed * speedMultiplier;
		} else if (moveVertical < 0) {
			if (wheelBR.motorTorque > 0) {
				rb.drag = 24f;
			}

			if (rb.velocity.magnitude > 10.0f && rb.velocity.magnitude < 15.0f) {
				rb.drag = 2f;
				//Debug.Log ("Magnitude is below 10.0"); // DEBUG, delete this
			}


			wheelBR.motorTorque = moveVertical * backwardSpeed;
			wheelBL.motorTorque = moveVertical * backwardSpeed;
		} else {
			rb.drag = 0.25f;
		}

		wheelFR.steerAngle = moveHorizontal * steerMultiplier;
		wheelFL.steerAngle = moveHorizontal * steerMultiplier;


		// DEBUG, Delete this when finish
		//Debug.Log("Magnitude: " + rb.velocity.magnitude);
	}

	public void Fire ()
	{
		if (Input.GetMouseButtonDown (0) && Time.time > elapsedTimeOnLeft) {
			elapsedTimeOnLeft = Time.time + cooldown;
			CmdShootLeft ();
		}
		if (Input.GetMouseButtonDown (1) && Time.time > elapsedTimeOnRight) {
			elapsedTimeOnRight = Time.time + cooldown;
			CmdShootRight ();
		}
	}

	[Command]
	void CmdShootLeft ()
	{
		StartCoroutine (InstantiateShotsOnLeft ());
		RpcShootLeft ();
	}

	[Command]
	void CmdShootRight ()
	{
		StartCoroutine (InstantiateShotsOnRight ());
		RpcShootRight ();
	}

	[ClientRpc]
	void RpcShootLeft ()
	{
		if (!isServer) {
			StartCoroutine (InstantiateShotsOnLeft ());
		}
	}

	[ClientRpc]
	void RpcShootRight ()
	{
		if (!isServer) {
			StartCoroutine (InstantiateShotsOnRight ());
		}
	}

	IEnumerator InstantiateShotsOnLeft ()
	{
		foreach (Transform shotPosition in shotPositionLeft) {
			GameObject cannonBall = (GameObject)Instantiate (cannonBallPrefab, shotPosition.position, Quaternion.identity);
			CannonBall cannonScript = cannonBall.GetComponent<CannonBall> ();
			cannonScript.ballDamage = damage * damageMultiplier;
			cannonBall.GetComponent<Rigidbody> ().velocity = -transform.right * projectileSpeed;
			yield return new WaitForSeconds (projectilesOffset);
		}
		StopCoroutine (InstantiateShotsOnLeft ());
	}

	IEnumerator InstantiateShotsOnRight ()
	{
		foreach (Transform shotPosition in shotPositionRight) {
			GameObject cannonBall = (GameObject)Instantiate (cannonBallPrefab, shotPosition.position, Quaternion.identity);
			CannonBall cannonScript = cannonBall.GetComponent<CannonBall> ();
			cannonScript.ballDamage = damage * damageMultiplier;
			cannonBall.GetComponent<Rigidbody> ().velocity = transform.right * projectileSpeed;
			yield return new WaitForSeconds (projectilesOffset);
		}
		StopCoroutine (InstantiateShotsOnRight ());
	}

	public void TakeDamage (float amount)
	{
		if (currentHealth > 0f) {
			currentHealth -= amount;

			if (this.hasAuthority) {
				userInterface.UpdateHealth (currentHealth / maxHealth);
			}
		} else {
			if (this.OnKeel != null) {
				this.OnKeel (this);
			}

			Destroy (this.gameObject);
		}
	}

	void OnTriggerEnter (Collider collision)
	{
		if (collision.gameObject.tag == "Chest") {
			Chest chest = collision.gameObject.GetComponent<Chest> ();

			if (this.OnChestGet != null) {
				this.OnChestGet (chest);
			}
		}
	}

	IEnumerator CannonShotRoutine ()
	{
		damageMultiplier = CANNON_SHOT_MULTIPLIER;
		yield return new WaitForSeconds (5f);
		damageMultiplier = 1f;
		StopCoroutine (CannonShotRoutine ());
	}

	IEnumerator WindBucketRoutine ()
	{
		speedMultiplier = WIND_BUCKET_MULTIPLIER;
		yield return new WaitForSeconds (5f);
		speedMultiplier = 1f;
		StopCoroutine (WindBucketRoutine ());
	}

	public void UseItem(PowerUps powerUp)
	{
		RpcUseItem (powerUp);
	}

	[ClientRpc]
	public void RpcUseItem (PowerUps powerUp)
	{
		switch (powerUp) {
		case PowerUps.LemonJuice:
			LemonJuiceHeal ();
			break;
		case PowerUps.CannonShot:
			StartCoroutine (CannonShotRoutine ());
			break;
		case PowerUps.PowderKeg:
			break;
		case PowerUps.WindBucket:
			StartCoroutine (WindBucketRoutine ());
			break;
		default:
			Debug.Log ("Incorrect powerup type in Ship Class");
			break;
		}	
	}

	public void LemonJuiceHeal ()
	{
		currentHealth *= LEMON_JUICE_MULTIPLIER;

		if (currentHealth > maxHealth)
			currentHealth = maxHealth;

		NetworkIdentity networkIdentity = this.gameObject.GetComponent<NetworkIdentity> ();

		if (networkIdentity.hasAuthority) {
			userInterface.UpdateHealth (currentHealth / maxHealth);
			Debug.Log ("Has authority");
		}
	}
}