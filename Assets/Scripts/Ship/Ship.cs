using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

[RequireComponent(typeof(Rigidbody))]
public class Ship : NetworkBehaviour {

	const float LEMON_JUICE_MULTIPLIER = .25f;
	public Player player;

    [Header("Ship Stat")]
    [SerializeField]
    float maxHealth;
    [SerializeField]
    float speed;
    [SerializeField]
    float damage;
	[SerializeField]
	float currentHealth;
    float backwardSpeed;

    [Header("Wheels")]
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

    [Header("Projectile")]
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

    void Awake()
    {
        backwardSpeed = speed / 1.5f;
		rb = GetComponent<Rigidbody> ();
		currentHealth = maxHealth;
    }

	void Start()
	{
		userInterface = GameObject.Find ("User Interface").GetComponent<UserInterface> ();
		if (isLocalPlayer) {
			GameObject.Find ("Main Camera").GetComponent<CameraFollow>().PlayerCreated (this.transform);
		}
	}
	
	void FixedUpdate ()
    {
		if (!isLocalPlayer)
			return;
        Movement();
        Fire();
	}

    void Movement()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");
        rb.drag = 0f;

        if (moveVertical > 0)
        {
			if (wheelBR.motorTorque < 0.0f)
			{
				rb.drag = 25f;
				wheelBR.motorTorque = moveVertical * backwardSpeed * 50f;
				wheelBL.motorTorque = moveVertical * backwardSpeed * 50f;
			}
			wheelBR.motorTorque = moveVertical * speed;
            wheelBL.motorTorque = moveVertical * speed;
        }
        else if (moveVertical < 0)
        {
			if (wheelBR.motorTorque > 0.0f)
			{
				rb.drag = 25f;
			}
            wheelBR.motorTorque = moveVertical * backwardSpeed;
            wheelBL.motorTorque = moveVertical * backwardSpeed;
        }
        else
        {
            rb.drag = 0.25f;
        }

        wheelFR.steerAngle = moveHorizontal * steerMultiplier;
        wheelFL.steerAngle = moveHorizontal * steerMultiplier;
    }
		
    public void Fire()
    {
        if(Input.GetMouseButtonDown(0) && Time.time > elapsedTimeOnLeft)
        {
            elapsedTimeOnLeft = Time.time + cooldown;
			CmdShootLeft ();
        }
        if(Input.GetMouseButtonDown(1) && Time.time > elapsedTimeOnRight)
        {
            elapsedTimeOnRight = Time.time + cooldown;
			CmdShootRight ();
        }
    }

	[Command]
	void CmdShootLeft()
	{
		StartCoroutine(InstantiateShotsOnLeft());
		RpcShootLeft ();
	}

	[Command]
	void CmdShootRight()
	{
		StartCoroutine(InstantiateShotsOnRight());
		RpcShootRight ();
	}

	[ClientRpc]
	void RpcShootLeft()
	{
		if (!isServer) {
			StartCoroutine (InstantiateShotsOnLeft ());
		}
	}

	[ClientRpc]
	void RpcShootRight()
	{
		if (!isServer) {
			StartCoroutine (InstantiateShotsOnRight ());
		}
	}

    IEnumerator InstantiateShotsOnLeft()
    {
        foreach (Transform shotPosition in shotPositionLeft)
        {
            GameObject cannonBall = (GameObject)Instantiate(cannonBallPrefab, shotPosition.position, Quaternion.identity);
			CannonBall cannonScript = cannonBall.GetComponent<CannonBall> ();
			cannonScript.ballDamage = damage;
			cannonScript.player = player;
            cannonBall.GetComponent<Rigidbody>().velocity = -transform.right * projectileSpeed;
            yield return new WaitForSeconds(projectilesOffset);
        }
        StopCoroutine(InstantiateShotsOnLeft());
    }

    IEnumerator InstantiateShotsOnRight()
    {
        foreach (Transform shotPosition in shotPositionRight)
        {
            GameObject cannonBall = (GameObject)Instantiate(cannonBallPrefab, shotPosition.position, Quaternion.identity);
			CannonBall cannonScript = cannonBall.GetComponent<CannonBall> ();
			cannonScript.ballDamage = damage;
			cannonScript.player = player;
            cannonBall.GetComponent<Rigidbody>().velocity = transform.right * projectileSpeed;
            yield return new WaitForSeconds(projectilesOffset);
        }
        StopCoroutine(InstantiateShotsOnRight());
    }
		
	public void TakeDamage(float amount)
	{
		if (currentHealth > 0f) {
			currentHealth -= amount;

			if (isLocalPlayer) {
				userInterface.UpdateHealth (currentHealth / maxHealth);
			}
		} else {
			Destroy (this.gameObject);
		}
	}

	void OnTriggerEnter (Collider collision)
	{
		if (collision.gameObject.tag == "Chest") {

			Chest chest = collision.gameObject.GetComponent<Chest>();

			player.Gold += chest.gold;
			player.Fame += chest.fame;
			player.AddPowerUps(chest.ChestPowerups);
		}
	}

	public void LemonJuiceHeal()
	{
		currentHealth *= LEMON_JUICE_MULTIPLIER;

		if (currentHealth > maxHealth)
			currentHealth = maxHealth;

		if (isLocalPlayer) {
			userInterface.UpdateHealth (currentHealth / maxHealth);
		}

	}
}