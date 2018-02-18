using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PirateKing : MonoBehaviour, IEvent {
	
	public GameObject zonePrefab;
	public event Action OnEnd;

	public void BeginEvent(NautilusServer server) {
		
		Transform location = transform.GetChild (UnityEngine.Random.Range (0, this.transform.childCount));

		GameObject.Instantiate (zonePrefab, location.transform);

		foreach (var player in server.activePlayers) {
			player.SendNotification ("<size=50>Event: Pirate King</size>\nConquer the zone and be the king!");
		}
	}
}
