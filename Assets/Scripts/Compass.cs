using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Compass : MonoBehaviour {
	GameObject player;
	RectTransform compass;
	RectTransform missionMarker;
	Vector3 northDirection;
	Vector3 mission;

	void Start () {
		compass = GameObject.Find("Compass Board").GetComponent<RectTransform> ();
		missionMarker = GameObject.Find ("Mission Marker").GetComponent<RectTransform> ();
		StartCoroutine (FindMission ());
	}

	void Update () {
		updateCompass ();
		updateMarker ();
	}

	void updateCompass() {
		if (player != null) {
			northDirection.z = player.transform.eulerAngles.y;
			compass.localEulerAngles = northDirection;
		} else {
			//Debug.Log ("Null");
		}
	}

	void updateMarker() {
		if (player != null && mission != Vector3.zero) {
			if (missionMarker.gameObject.activeSelf == false) {
				missionMarker.gameObject.SetActive (true);
			}

			Vector3 direction = mission - player.transform.position;
			Quaternion missionDirection = Quaternion.LookRotation (direction);
			missionDirection.z = -missionDirection.y;
			missionDirection.x = 0f;
			missionDirection.y = 0f;

			// Without multiplying by the north direction, the marker only rotate base on the position of the ship
			// By multiplying with the north direction, which is the angle that the ship rotate, the marker is now
			// rotated based on the ship's rotation
			missionMarker.localRotation = missionDirection * Quaternion.Euler (northDirection);
		} else {
			if (missionMarker.gameObject.activeSelf == true) {
				missionMarker.gameObject.SetActive (false);
			}
		}
	}

	IEnumerator FindMission() {
		while (true) {
			Vector3 newMission =
				GameObject.FindGameObjectsWithTag ("Mission")
					.Select(m => m.transform.position)
					.Aggregate (Vector3.zero, (nearestSoFar, thisMission) => {
						var distanceToNearest = Vector3.Distance (nearestSoFar, player.transform.position);
						var distanceToThis = Vector3.Distance (thisMission, player.transform.position);
						if (nearestSoFar == Vector3.zero || distanceToThis < distanceToNearest) {
							return thisMission;
						} else {
							return nearestSoFar;
						}
					});


			this.mission = newMission;

			yield return new WaitForSeconds(1.0f);
		}
	}

	public void PlayerConnected(Player player) {
		player.OnKeel += HandlePlayerKeel;
		player.OnLaunch += HandlePlayerLaunch;
		player.OnLogout += HandlePlayerLogout;
		this.player = player.gameObject;
	}

	void HandlePlayerKeel(GameObject ship) {
		this.player = null;
	}

	void HandlePlayerLaunch(GameObject ship) {
		this.player = ship;
	}

	void HandlePlayerLogout(Player player) {
		player.OnLogout -= HandlePlayerLogout;
		player.OnLaunch -= HandlePlayerLaunch;
		player.OnKeel -= HandlePlayerKeel;
		this.player = null;
	}
}
