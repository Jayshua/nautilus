using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class FamePlank : NetworkBehaviour {
	Text[] slots;
	[SerializeField] GameController GameController;

	void Start() {
		slots = this.GetComponentsInChildren<Text> ();
		StartCoroutine (UpdatePlank ());
	}

	IEnumerator UpdatePlank() {
		while (true) {
			var sortedPlayers = GameController.allPlayers
				.Where (p => !p.playerName.Equals (string.Empty))
				.OrderByDescending (p => p.Fame)
				.ToArray ();

			int index = -1;
			foreach (var slot in slots) {
				index += 1;

				if (index < sortedPlayers.Length) {
					var player = sortedPlayers [index];

					slot.text = string.Format (
						"<size=20>{0}: {1}</size>" +
						"\n<size=13>Fame: {2}, Gold: {3}</size>",
						UserInterface.NumberOrdinality(index + 1),
						player.playerName,
						player.Fame,
						player.Gold);
				} else {
					slot.text = "";
				}
			}

			yield return new WaitForSeconds (2f);
		}
	}
}