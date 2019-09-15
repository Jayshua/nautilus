using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Notification : MonoBehaviour {
	Text text;
	Animation mation; // "animation" is a property on MonoBehaviour
	AnimationState animationState;
	RectTransform rectTransform;

	void Start() {
		this.text = this.GetComponentInChildren<Text> ();
		this.mation = this.GetComponent<Animation> ();
		this.animationState = this.mation ["NotificationIn"];
		this.rectTransform = this.GetComponent<RectTransform> ();
	}

	public void ShowNotification(string notification, float seconds) {
		text.text = notification;
		this.rectTransform.sizeDelta = new Vector2 (this.rectTransform.sizeDelta.x, text.preferredHeight + 130);
		this.animationState.speed = 1;
		this.animationState.time = 0;
		this.mation.Play ();
		Invoke ("closeNotification", seconds);
	}

	void closeNotification() {
		this.animationState.speed = -1;
		this.animationState.time = this.animationState.length;
		mation.Play ();
	}
}
