using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothCamera : MonoBehaviour
{
	public float height = 55f;
	public float back = 50f;
	public float speed = .5f;
	public float side = 40;
	public float sideSpeed = .2f;
	public float backSpeed = .2f;

	Transform target;
	Vector3 moveVelocity;

	Vector3 originalPosition;
	Quaternion originalRotation;

	public float horizontalSpeed = 2f;
	public float verticalSpeed = 2f;

	void Start ()
	{
		originalPosition = this.transform.position;
		originalRotation = this.transform.rotation;
	}

	// Uncomment and comment out the transform on Line 58 for testing
	//	void Update()
	//	{
	//		if (Input.GetMouseButton(2)) {
	//			float horizontalPan = horizontalSpeed * Input.GetAxis ("Mouse X");
	//			float verticalPan   = verticalSpeed   * Input.GetAxis ("Mouse Y");
	//			transform.Rotate(verticalPan, horizontalPan, 0);
	//		}
	//		else
	//			transform.LookAt (target);
	//	}

	void FixedUpdate ()
	{
		if (!target) {
			return;
		}

		if (Input.GetKey (KeyCode.E)) {
			// Turn camera to the left
			transform.position = Vector3.SmoothDamp (transform.position, target.position - target.forward * back / 2 - target.right * side + target.up * height, ref moveVelocity, sideSpeed);
		} else if (Input.GetKey (KeyCode.Q)) {
			// Turn camera to the right
			transform.position = Vector3.SmoothDamp (transform.position, target.position - target.forward * back / 2 + target.right * side + target.up * height, ref moveVelocity, sideSpeed);
		} else if (Input.GetKey (KeyCode.B)) {
			// Turn camera to the right
			transform.position = Vector3.SmoothDamp (transform.position, target.position - target.forward * -back + target.up * height, ref moveVelocity, backSpeed);
		} else {
			// Look straight at ship
			transform.position = Vector3.SmoothDamp (transform.position, target.position - target.forward * back + target.up * height, ref moveVelocity, speed);
		}

		// Keep the camera from sinking underwater
		if (transform.position.y < 5)
			transform.position += Vector3.up * 5;
		
		transform.LookAt (target);

		// Adjust the angle of the camera that is looking at the player
		transform.eulerAngles = new Vector3 (0, transform.eulerAngles.y, 0);
		transform.Rotate (35f, 0, 0);
	}

	public void PlayerCreated (Transform player)
	{
		this.target = player;
	}

	public void DetachCamera ()
	{
		this.target = null;
	}

	public void MainScreen ()
	{
		this.target = null;
		this.transform.position = originalPosition;
		this.transform.rotation = originalRotation;
	}
}
