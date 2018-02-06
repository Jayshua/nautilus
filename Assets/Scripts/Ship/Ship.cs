using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Ship : MonoBehaviour {

    [Header("Ship Stat")]
    [SerializeField]
    int health;
    [SerializeField]
    float speed;
    [SerializeField]
    int damage;
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

    void Awake()
    {
        backwardSpeed = speed / 2f;
        rb = GetComponent<Rigidbody>();
    }
	
	void FixedUpdate ()
    {
        movement();
        Fire();
	}

    void movement()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");
        rb.drag = 0f;

        if (moveVertical > 0)
        {
            wheelBR.motorTorque = moveVertical * speed;
            wheelBL.motorTorque = moveVertical * speed;
        }
        else if (moveVertical < 0)
        {
            wheelBR.motorTorque = moveVertical * backwardSpeed;
            wheelBL.motorTorque = moveVertical * backwardSpeed;
        }
        else
        {
            rb.drag = 0.5f;
        }

        wheelFR.steerAngle = moveHorizontal * steerMultiplier;
        wheelFL.steerAngle = moveHorizontal * steerMultiplier;
    }
		
    public void Fire()
    {
        if(Input.GetMouseButtonDown(0) && Time.time > elapsedTimeOnLeft)
        {
            elapsedTimeOnLeft = Time.time + cooldown;
            StartCoroutine(InstantiateShotsOnLeft());
        }
        if(Input.GetMouseButtonDown(1) && Time.time > elapsedTimeOnRight)
        {
            elapsedTimeOnRight = Time.time + cooldown;
            StartCoroutine(InstantiateShotsOnRight());
        }
    }

    IEnumerator InstantiateShotsOnLeft()
    {
        foreach (Transform shotPosition in shotPositionLeft)
        {
            GameObject cannonBall = (GameObject)Instantiate(cannonBallPrefab, shotPosition.position, Quaternion.identity);
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
            cannonBall.GetComponent<Rigidbody>().velocity = transform.right * projectileSpeed;
            yield return new WaitForSeconds(projectilesOffset);
        }
        StopCoroutine(InstantiateShotsOnRight());
    }
}