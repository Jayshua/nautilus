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
				Gold = 500,
				Fame = 0,
				Powerups = new PowerUps[] {},
			};
			NetworkServer.Spawn (newChest);
			this.chests.Add (newChestScript);
		}

		foreach (var player in gameController.activePlayers) {
			player.SendNotification ("<size=50>Event: Gold Rush</size>\nBe the first visit as many zones as possible. Trust the compass Luke!");
		}

		StartCoroutine (CheckIfDone ());

		//server.OnPlayerJoin += HandlePlayerJoin;

		// Create coin zones
		// Create the chests
		// Notify players
		// Listen for new players
		// Listen for lost players
	}

	IEnumerator CheckIfDone() {
		while (this.chests.Count > 0) {
			int previousChestCount = this.chests.Count;
			this.chests = this.chests.Where (c => c != null).ToList ();
			int newChestCount = this.chests.Count;

			Debug.Log(String.Format("Old: {0}, New: {1}", previousChestCount, newChestCount));

			if (previousChestCount != newChestCount && newChestCount != 0) {
				Debug.Log ("Not equal");
				foreach (var player in this.gameController.activePlayers) {
					player.SendNotification (String.Format ("Only {0} treasure{1} left!", newChestCount, newChestCount > 1 ? "s" : ""));
				}
			}

			if (this.chests.Count == 0) {
				if (this.OnEnd != null) {
					this.OnEnd ();
				}

				foreach (var player in this.gameController.activePlayers) {
					player.SendNotification ("<size=50>Event Complete!</size>\nCongratulamations! The event is complete!");
				}

				foreach (var zone in this.zones) {
					Destroy(zone.gameObject);
				}

				Destroy (this.gameObject);
			}

			yield return new WaitForSeconds (1);
		}
	}

	// Ongoing:
	// - Update compass markers
	// - End when all zones are found

	// Cleanup:
	// - Remove coin zones
	// - Remove compass markers
	// - Remove player messages
	// - Remove event listeners on the server
}