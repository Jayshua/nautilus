using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SpawnChest : NetworkBehaviour
{

	[SerializeField] GameObject ChestPrefab;
	[SerializeField] GameController GameController;

	public override void OnStartServer ()
	{
		base.OnStartServer ();
		StartCoroutine (SpawnChests ()); // Start coroutine to spawn chests
	}

	IEnumerator SpawnChests ()
	{
		while (true) {
			if (GameObject.FindGameObjectsWithTag ("Chest").Length < 100 + GameController.allPlayers.Count * 2) {
				Vector3 spawnLocation = GameController.FindSpawnPoint ();

				GameObject chest = GameObject.Instantiate (ChestPrefab, spawnLocation, Quaternion.identity);
				Chest chestScript = chest.GetComponent<Chest> ();
				int chestLoot = Random.Range (25, 150);
				chestScript.spoils = new Spoils () {
					Gold = chestLoot,
					Fame = chestLoot,
					Powerups = new [] { Random.Range (0, 5) },
				};
				NetworkServer.Spawn (chest);
			} else {
				yield return new WaitForSeconds (5f);
			}
		}
	}
}