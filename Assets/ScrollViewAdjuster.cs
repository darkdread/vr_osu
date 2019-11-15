using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class ScrollViewAdjuster : MonoBehaviour
{
	private ScrollRect scrollRect;
	private Transform content;

	private void Awake() {
		scrollRect = GetComponent<ScrollRect>();
		content = transform.Find("Viewport").Find("Content");
	}

	public void AdjustContentHeight() {
		RectTransform contentRt = content.GetComponent<RectTransform>();

		float contentMaxHeight = 0;
		foreach(Transform contentChild in content) {
			contentMaxHeight += contentChild.GetComponent<RectTransform>().rect.height;
		}

		contentRt.sizeDelta = new Vector2(contentRt.rect.width, contentMaxHeight);
	}

	public void AdjustBasedOnContent() {

	}
}
