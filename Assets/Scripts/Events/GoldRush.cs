// Prefab for coin zone
// - Detects when player enters and gives gold/fame
// - Player Enter Event
// - HashSet<Player>

// NautilusServer
// - OnPlayerJoin event

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GoldRush : MonoBehaviour, IEvent {
	public GameObject zonePrefab;
	public GameObject chestPrefab;

	GameController gameController;

	List<Zone> zones = new List<Zone> ();
	List<Chest> chests = new List<Chest> ();
	public event Action OnEnd;

	public void BeginEvent(GameController gameController) {
		this.gameController = gameController;

		int zoneCount = 3; //(int)Math.Ceiling((float)gameController.activePlayers.Count / 4.0f);

		var zoneSpawnLocations = new HashSet<GameObject>();
		while (zoneSpawnLocations.Count < zoneCount) {
			var newLocation = this.transform.GetChild(UnityEngine.Random.Range(0, this.transform.childCount)).gameObject;
			zoneSpawnLocations.Add (newLocation);
		}

		foreach (var zoneSpawn in zoneSpawnLocations) {
			var newZone = GameObject.Instantiate (zonePrefab, zoneSpawn.transform);
			NetworkServer.Spawn (newZone);
			this.zones.Add (newZone.GetComponent<Zone>());

			var newChest = GameObject.Instantiate (chestPrefab, zoneSpawn.transform);
			var newChestScript = newChest.GetComponent<Chest>();
			newChestScript.spoils = new Spoils() {
				Gold = 200,
				Fame = 400,
				Powerups = new int[] {},
			};
			NetworkServer.Spawn (newChest);
			this.chests.Add (newChestScript);
		}

		foreach (var player in gameController.allPlayers) {
			player.SendNotification (UserInterface.BuildHeadingNotification("Event: Gold Rush", "Be the first to visit as many gold zones as possible. Trust the compass Luke!"));
		}

		StartCoroutine (CheckIfDone ());
	}

	IEnumerator CheckIfDone() {
		while (this.chests.Count > 0) {
			int previousChestCount = this.chests.Count;
			this.chests = this.chests.Where (c => c != null).ToList ();
			int newChestCount = this.chests.Count;

			if (previousChestCount != newChestCount && newChestCount != 0) {
				foreach (var player in this.gameController.allPlayers) {
					player.SendNotification (String.Format ("Only {0} treasure{1} left!", newChestCount, newChestCount > 1 ? "s" : ""));
				}
			}

			if (this.chests.Count == 0) {
				if (this.OnEnd != null) {
					this.OnEnd ();
				}

				foreach (var player in this.gameController.allPlayers) {
					player.SendNotification (UserInterface.BuildHeadingNotification("Event: Gold Rush", "The event is complete. Congratulations to all captains!"));
				}

				foreach (var zone in this.zones) {
					Destroy(zone.gameObject);
				}

				Destroy (this.gameObject);
			}

			yield return new WaitForSeconds (1);
		}
	}
}