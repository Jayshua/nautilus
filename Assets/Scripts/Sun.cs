using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sun : MonoBehaviour {

	[SerializeField]
	float DayTime;

	// Update is called once per frame
	void Update ()
	{
		transform.RotateAround (Vector3.zero, Vector3.right, DayTime * Time.deltaTime);
		transform.LookAt (Vector3.zero);
	}
}
