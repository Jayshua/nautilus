using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Networking;

public class PirateKing : MonoBehaviour, IEvent {

	public GameObject zonePrefab;
	int zoneChangeCounter = 0;
	GameObject currentZone;
	GameController gameController;
	public event Action OnEnd;

	public void BeginEvent(GameController gameController) {
		this.gameController = gameController;

		SpawnZone (null);

		foreach (var player in gameController.allPlayers) {
			player.SendNotification (UserInterface.BuildHeadingNotification("Event: Pirate King", "Whoever is inside the water ring will recieve untold amounts of Gold and Fame. But if there is more than one person in the ring, no rewards will be given. Follow your compass quickly, the ring likes to move!"));
		}
	}

	void SpawnZone(KingZone lastZone) {
		zoneChangeCounter += 1;

		if (zoneChangeCounter <= 4) {
			foreach (var player in gameController.allPlayers) {
				player.SendNotification (UserInterface.BuildHeadingNotification("Event: Pirate King", "The Zone has Moved! Follow your compass, there's still time to win!"));
			}

			if (lastZone != null) {
				lastZone.OnDone -= SpawnZone;
			}

			Transform location = transform.GetChild (UnityEngine.Random.Range (0, this.transform.childCount));
			currentZone = GameObject.Instantiate (zonePrefab, location.transform);
			NetworkServer.Spawn (currentZone);
			currentZone.GetComponent<KingZone> ().OnDone += SpawnZone;
		} else {
			foreach (var player in gameController.allPlayers) {
				player.SendNotification (UserInterface.BuildHeadingNotification("Event: Pirate King", "Event Complete!\n<player>managed to be inside the ring a total of <seconds> seconds! All hail Pirate King <player>!"));
			}

			if (this.OnEnd != null) {
				this.OnEnd ();
			}

			Destroy (this.gameObject);
		}
	}
}