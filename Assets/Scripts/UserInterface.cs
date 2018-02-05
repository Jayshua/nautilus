﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum ClassType {
	LargeShip,
	MediumShip,
	SmallShip
}

public class UserInterface : MonoBehaviour {
	private RectTransform ClassSelectionPanel;
	private RectTransform NameSelectionPanel;
	private Text NameSelectionMessage;
	private Text NameText;

	private Action<ClassType> handleClassSelected;
	private Action<string> handleNameSelected;

	void Start()
	{
		ClassSelectionPanel  = GameObject.Find ("ClassSelection").GetComponent<RectTransform>();
		NameSelectionPanel   = GameObject.Find ("UserNameSelection").GetComponent<RectTransform>();
		NameSelectionMessage = GameObject.Find ("UserNameSelection/Message").GetComponent<Text>();
		NameText             = GameObject.Find ("UserNameSelection/Name/NameText").GetComponent<Text>();

		//NameText = (Text) this.transform.Find ("UserNameSelection/Name/NameText").gameObject;
	}

	public void ShowClassSelection(Action<ClassType> callback) {
		handleClassSelected = callback;
		ClassSelectionPanel.anchoredPosition = new Vector2 (0, 0);
		NameSelectionPanel.anchoredPosition = new Vector2 (0, -10000);
	}

	public void ShowNameSelection(Action<string> callback, bool isNameTaken) {
		handleNameSelected = callback;
		NameSelectionPanel.anchoredPosition = new Vector2 (0, 0);

		if (isNameTaken) {
			NameSelectionMessage.text = "Name was taken. Please try again.";
		} else {
			NameSelectionMessage.text = "";
		}
	}

	public void SelectClass(string type) {
		if (handleClassSelected != null) {
			switch (type) {
			case "cube":
				handleClassSelected (ClassType.SmallShip);
				break;
			case "cylinder":
				handleClassSelected (ClassType.MediumShip);
				break;
			case "sphere":
				handleClassSelected (ClassType.LargeShip);
				break;
			default:
				Debug.Log ("Unknown class type in class selection GUI: " + type);
				break;
			}

			handleClassSelected = null;
		}

		ClassSelectionPanel.anchoredPosition = new Vector2 (0, -10000);
	}

	public void Submit() {
		if (handleNameSelected != null) {
			handleNameSelected (NameText.text);
		}
	}
}
