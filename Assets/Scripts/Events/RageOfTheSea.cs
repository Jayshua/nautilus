using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Networking;

public class RageOfTheSea : MonoBehaviour, IEvent {

	[SerializeField] GameObject objectPrefab;

	GameController gameController;
	GameObject rageObject;
	public event Action OnEnd;

	public void BeginEvent(GameController gameController) {
		this.gameController = gameController;

		SpawnObject ();

		foreach (var player in gameController.allPlayers) {
			player.SendNotification (UserInterface.BuildHeadingNotification("Event: Rage Of The Sea", "Find and hold the parrot for a minute and half to win. Follow the compass!"));
		}
	}

	void SpawnObject(){
		rageObject = GameObject.Instantiate (objectPrefab, gameController.FindSpawnPoint(), Quaternion.identity);
		NetworkServer.Spawn (rageObject);
		rageObject.GetComponent<RageObject> ().OnDone += FinishEvent;
	}

	void FinishEvent(){
		if (this.OnEnd != null) {
			this.OnEnd ();
		}

		foreach (var player in gameController.allPlayers) {
			player.SendNotification (UserInterface.BuildHeadingNotification("Event: Rage Of The Sea", "The event is complete! Congratulations to <player> who held the parrot the longest!"));
		}

		Destroy (gameObject);
	}
}