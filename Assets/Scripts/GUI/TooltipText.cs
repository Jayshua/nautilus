using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TooltipText : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
	[SerializeField] GameObject tooltip;

	public void Start() {
		tooltip.SetActive(false);
	}

	public void OnPointerEnter (PointerEventData eventData) {
		Invoke ("ShowTooltip", 0.4f);
	}

	public void OnPointerExit (PointerEventData eventData) {
		tooltip.SetActive(false);
		CancelInvoke ("ShowTooltip");
	}

	void ShowTooltip() {
		tooltip.SetActive(true);
	}
}
