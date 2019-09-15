using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Chest : NetworkBehaviour
{
	[SyncVar] public Spoils spoils;

	// In rotations per second
	[SerializeField] float rotateSpeed;
	// In cycles (up and down) per second
	[SerializeField] float floatSpeed;
	// The maximum distance the coin can move up and down
	[SerializeField] float movementDistance;
	// The floating text prefab
	[SerializeField] GameObject floatingText;

	// The number of seconds after which this chest should be destroyed
	public float? destroyAfterSeconds = null;

	// Use this for initialization
	void Start ()
	{
		transform.Rotate (transform.up, UnityEngine.Random.Range (0f, 360f));
	}

	void FixedUpdate()
	{
		if (isClient) {
			transform.rotation = Quaternion.Euler (0f, Time.fixedTime * rotateSpeed, 0f);
			transform.position = new Vector3 (transform.position.x, Mathf.Sin (Time.fixedTime * floatSpeed) * movementDistance - 2.2f, transform.position.z);
		}

		if (isServer && destroyAfterSeconds != null) {
			destroyAfterSeconds -= Time.fixedDeltaTime;

			if (destroyAfterSeconds <= 0) {
				Destroy (this.gameObject);
			}
		}
	}

	void OnTriggerEnter (Collider collision)
	{
		if (collision.gameObject.tag == "Player") {
			var text = GameObject.Instantiate (floatingText, this.transform.position + Vector3.up * 2.0f, this.transform.rotation);
			text.transform.Translate (Vector3.right * 6.0f, Space.Self);
			var floating = text.GetComponent<FloatingText>();
			floating.SetText (string.Format(
				"{0} Gold\n{1} Fame\n{2}",
				this.spoils.Gold,
				this.spoils.Fame,
				string.Join("\n", this.spoils.Powerups.Select(Powerups.ToString).ToArray())
			));

			Destroy (this.gameObject);
		}
	}
}