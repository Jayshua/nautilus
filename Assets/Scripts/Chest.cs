using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Chest : MonoBehaviour {

	public int gold,
			   fame;

	public List<PowerUps> ChestPowerups = new List<PowerUps>();

	public event Action OnDestroy;

	[SerializeField]
	private float rotateSpeed = 0.1f; // In rotations per second

	[SerializeField]
	private float floatSpeed = 0.1f; // In cycles (up and down) per second

	[SerializeField]
	private float movementDistance = 1f; // The maximum distance the coin can move up and down

	private float startingY;
	private bool isMovingUp = true;

	// Use this for initialization
	void Start () {
		startingY = transform.position.y;
		transform.Rotate (transform.up, UnityEngine.Random.Range (0f, 360f));
		StartCoroutine (Spin ());
		StartCoroutine (Float ());
	}

	private IEnumerator Spin()
	{
		while (true) 
		{
			transform.Rotate (transform.up, 360 * rotateSpeed * Time.deltaTime);
			yield return 0;
		}
	}

	private IEnumerator Float()
	{
		while (true)
		{
			float newY = transform.position.y + (isMovingUp ? 1 : -1) * 2 * movementDistance * floatSpeed * Time.deltaTime;

			if (newY > startingY + movementDistance)
			{
				newY = startingY + movementDistance;
				isMovingUp = false;
			}
			else if (newY < startingY)
			{
				newY = startingY;
				isMovingUp = true;
			}

			transform.position = new Vector3(transform.position.x, newY, transform.position.z);
			yield return 0;
		}
	}

	void OnTriggerEnter(Collider collision){
		if (OnDestroy != null) {
			OnDestroy();
		}

		if (collision.gameObject.tag == "Player") {

			Ship ship = collision.GetComponent<Ship> ();

			ship.player.Gold += gold;
			ship.player.Fame += fame;
			ship.player.Inventory.AddRange (ChestPowerups);

			Destroy (this.gameObject);
		}
	}
}