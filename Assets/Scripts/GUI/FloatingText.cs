using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingText : MonoBehaviour {
	[SerializeField] float HorizontalFrequency;
	[SerializeField] float HorizontalAmplitude;
	[SerializeField] float VerticalSpeed;
	[SerializeField] float FadeSpeed;

	TextMesh textMesh;

	void Awake() {
		this.textMesh = this.GetComponent<TextMesh> ();
		this.transform.LookAt (Camera.main.transform);
		this.transform.Rotate (0.0f, 180.0f, 0.0f, Space.Self);
	}

	public void SetText(string text) {
		textMesh.text = text;
	}

	// Update is called once per frame
	void FixedUpdate () {
		this.transform.Translate (Mathf.Sin(Time.fixedTime * HorizontalFrequency) * HorizontalAmplitude, VerticalSpeed * Time.fixedDeltaTime, 0.0f);
		this.textMesh.color = new Color (this.textMesh.color.r, this.textMesh.color.g, this.textMesh.color.b, this.textMesh.color.a - FadeSpeed * Time.fixedDeltaTime);

		if (this.textMesh.color.a <= 0f) {
			Destroy (this.gameObject);
		}
	}
}
