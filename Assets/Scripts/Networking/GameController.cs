using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class GameController : NetworkBehaviour {
	public List<GameObject> eventPrefabs;
	public NautilusNetworkManager networkManager;

	public List<Player> activePlayers;

	public override void OnStartServer() {
		networkManager.OnPlayerJoin += player => activePlayers.Add (player);
		networkManager.OnPlayerLeave += player => activePlayers = activePlayers.Where (p => p != player).ToList ();
		StartCoroutine (gameLoop());
	}

	IEnumerator gameLoop() {
		IEvent currentEvent;

		while (true) {
			// Wait 5 minutes in between events
			//yield return new WaitForSeconds (5.0f * 60.0f);
			yield return new WaitForSeconds(1.0f);

			currentEvent = (IEvent)Instantiate (eventPrefabs [0]).GetComponent (typeof(IEvent));
			currentEvent.BeginEvent (this);

			yield return new WaitForSeconds (5.0f * 60.0f);
		}
	}
}
