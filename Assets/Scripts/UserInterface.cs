using System;
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

	const int HEALTHBARWIDTH = 1145;

	private GameObject ClassSelectionPanel;
	private GameObject NameSelectionPanel;
	private GameObject GuiPanel;
	private Text NameSelectionMessage;
	private Text NameText;
	private RectTransform healthBar;

	private Action<ClassType> handleClassSelected;
	private Action<string> handleNameSelected;

	void Start()
	{
		ClassSelectionPanel  = GameObject.Find ("ClassSelection");
		GuiPanel             = GameObject.Find ("GuiPanel");
		NameSelectionPanel   = GameObject.Find ("UserNameSelection");
		NameSelectionMessage = GameObject.Find ("UserNameSelection/Message").GetComponent<Text>();
		NameText             = GameObject.Find ("UserNameSelection/Name/NameText").GetComponent<Text>();
		healthBar            = GameObject.Find ("HealthBar").GetComponent<RectTransform>();
		ClassSelectionPanel.SetActive(false);
		GuiPanel.SetActive(false);
		NameSelectionPanel.SetActive(false);
	}

	public void ShowClassSelection(Action<ClassType> callback) {
		handleClassSelected = callback;
		ClassSelectionPanel.SetActive(true);
		NameSelectionPanel.SetActive(false);
	}

	public void ShowNameSelection(Action<string> callback, bool isNameTaken) {
		handleNameSelected = callback;
		NameSelectionPanel.SetActive(true);

		if (isNameTaken) {
			NameSelectionMessage.text = "Name was taken. Please try again.";
		} else {
			NameSelectionMessage.text = "";
		}
	}


	public void ShowGUI() {
		NameSelectionPanel.SetActive (false);
		ClassSelectionPanel.SetActive (false);
		GuiPanel.SetActive(true);
	}

	public void SelectClass(string type) {
		ShowGUI ();
		if (handleClassSelected != null) {
			switch (type) {
			case "Black Pearl":
				handleClassSelected (ClassType.SmallShip);
				break;
			case "Captian Fortune":
				handleClassSelected (ClassType.MediumShip);
				break;
			case "Royal Dutchman":
				handleClassSelected (ClassType.LargeShip);
				break;
			default:
				Debug.Log ("Unknown class type in class selection GUI: " + type);
				break;
			}

			handleClassSelected = null;
		}
	}

	public void Submit() {
		if (handleNameSelected != null) {
			handleNameSelected (NameText.text);
		}
	}

	public void UpdateHealth (float health)
	{
		healthBar.sizeDelta = new Vector2 (health * HEALTHBARWIDTH, healthBar.sizeDelta.y);
	}
}
