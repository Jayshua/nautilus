using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterSetup : MonoBehaviour {

	[SerializeField] GameObject waterPrefab;
	[SerializeField] float waterTileSize;
	[SerializeField] int gridSize;

	// Use this for initialization
	void Start () {
		for (int column = 1; column < gridSize; column++) {
			for (int row = 1; row < gridSize; row++) {
				GameObject.Instantiate (waterPrefab, this.transform.position 
					- transform.forward * column  * waterTileSize * Mathf.Sqrt (3f)
					+ transform.right * row * waterTileSize * Mathf.Sqrt (3f), Quaternion.identity, this.transform);
			}
		}
	}
}
