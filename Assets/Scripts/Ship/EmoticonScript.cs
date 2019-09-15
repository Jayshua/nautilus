using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public enum Emoticon {
	Angry, Sad, Adkins, Hook
}

public class EmoticonScript : NetworkBehaviour {
	[SerializeField] Sprite Angry;
	[SerializeField] Sprite Sad;
	[SerializeField] Sprite Adkins;
	[SerializeField] Sprite Hook;
	[SerializeField] Image image;
	UserInterface userInterface;
	Sprite defaultSprite;

	// Use this for initialization
	[Client]
	public override void OnStartAuthority () {
		base.OnStartAuthority ();
		userInterface = GameObject.Find ("User Interface").GetComponent<UserInterface> ();
		userInterface.OnEmoticon += HandleEmoticon;
		this.defaultSprite = image.sprite;
	}

	[Everywhere]
	void OnDestroy() {
		if (this.userInterface != null) {
			userInterface.OnEmoticon -= HandleEmoticon;
			userInterface = null;
		}
	}

	[Client]
	void HandleEmoticon(Emoticon emoticon) {
		CmdSetEmoticon (emoticon);
	}

	[Command]
	void CmdSetEmoticon(Emoticon emoticon) {
		RpcSetEmoticon (emoticon);
	}

	[ClientRpc]
	void RpcSetEmoticon(Emoticon emoticon) {
		var map = new Dictionary<Emoticon, Sprite> {
			{ Emoticon.Angry, this.Angry },
			{ Emoticon.Adkins, this.Adkins },
			{ Emoticon.Hook, this.Hook },
			{ Emoticon.Sad, this.Sad }
		};

		this.image.color  = new Color (1,1,1,1);
		this.image.sprite = map [emoticon];

		CancelInvoke ("RemoveEmoticon");
		Invoke ("RemoveEmoticon", 10f);
	}

	[Client]
	void RemoveEmoticon() {
		this.image.sprite = this.defaultSprite;
		this.image.color  = new Color (0,0,0,0);
	}
}
