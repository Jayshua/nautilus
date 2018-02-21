using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Compass : MonoBehaviour {
	Ship playerShip;
	RectTransform compass;
	RectTransform missionMarker;
	Vector3 northDirection;
	Vector3 mission;

	void OnEnable () {
		compass = GameObject.Find("Compass Board").GetComponent<RectTransform> ();
		var missionGameObject = GameObject.Find ("Mission Marker");

		if (missionMarker == null) {
			missionMarker = GameObject.Find ("Mission Marker").GetComponent<RectTransform> ();
		}

		StartCoroutine (FindMission ());
	}

	void Update () {
		updateCompass ();
		updateMarker ();
	}

	void updateCompass() {
		if (playerShip != null) {
			northDirection.z = playerShip.gameObject.transform.eulerAngles.y;
			compass.localEulerAngles = northDirection;
		}
	}

	void updateMarker() {
		if (playerShip != null && mission != Vector3.zero) {
			if (missionMarker.gameObject.activeSelf == false) {
				missionMarker.gameObject.SetActive (true);
			}

			Vector3 direction = mission - playerShip.gameObject.transform.position;
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
			yield return new WaitForSeconds(1.0f);

			if (playerShip == null) {
				continue;
			}

			Vector3 newMission =
				GameObject.FindGameObjectsWithTag ("Mission")
					.Select(m => m.transform.position)
					.Aggregate (Vector3.zero, (nearestSoFar, thisMission) => {
						var distanceToNearest = Vector3.Distance (nearestSoFar, playerShip.gameObject.transform.position);
						var distanceToThis = Vector3.Distance (thisMission, playerShip.gameObject.transform.position);
						if (nearestSoFar == Vector3.zero || distanceToThis < distanceToNearest) {
							return thisMission;
						} else {
							return nearestSoFar;
						}
					});

			this.mission = newMission;
		}
	}

	public void PlayerConnected(Player player) {
		player.OnLaunch += HandlePlayerLaunch;
		player.OnLogout += HandlePlayerLogout;
	}

	void HandlePlayerKeel(Ship ship) {
		ship.OnKeel -= HandlePlayerKeel;
		this.playerShip = null;
	}

	void HandlePlayerLaunch(Ship ship) {
		ship.OnKeel += HandlePlayerKeel;
		this.playerShip = ship;
	}

	void HandlePlayerLogout(Player player) {
		player.OnLogout -= HandlePlayerLogout;
		player.OnLaunch -= HandlePlayerLaunch;

		if (this.playerShip != null) {
			this.playerShip.OnKeel -= HandlePlayerKeel;
		}

		this.playerShip = null;
	}
}
