using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Compass : MonoBehaviour {

	Transform player;
	public Transform mission;
	RectTransform compass;
	RectTransform missionMarker;
	Quaternion missionDirection;
	Vector3 northDirection;

	void Start () {
		compass = GameObject.Find("Compass Board").GetComponent<RectTransform> ();
		missionMarker = GameObject.Find ("Mission Marker").GetComponent<RectTransform> ();
	}

	void Update () {
		ChangeNorthDirection ();
		ChangeMissionDirection ();
	}

	void ChangeNorthDirection(){
		northDirection.z = player.eulerAngles.y;
		compass.localEulerAngles = northDirection;
	}

	void ChangeMissionDirection(){
		Vector3 direction = mission.transform.position - player.transform.position;
		missionDirection = Quaternion.LookRotation (direction);
		missionDirection.z = -missionDirection.y;
		missionDirection.x = 0f;
		missionDirection.y = 0f;
		missionMarker.localRotation = missionDirection * Quaternion.Euler(northDirection);
		// Without multiplying by the north direction, the marker only rotate base on the position of the ship
		// By multiplying with the north direction, which is the angle that the ship rotate, the marker is now
		// rotate base on the ship's rotation
	}

	public void PlayerCreated(Transform player) {
		this.player = player;
	}
}
