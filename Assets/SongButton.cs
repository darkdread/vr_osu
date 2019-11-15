using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SongButton : MonoBehaviour
{
	private Button button;
	private RectTransform mainRt;

	public Text songTitle;
	public List<SongButtonChild> songButtonChildren;

	private void Awake() {
		button = GetComponent<Button>();
		mainRt = GetComponent<RectTransform>();

		button.onClick.AddListener(OpenList);
	}

	public void UpdateSongButton(string title) {
		songTitle.text = title;
	}

	public void HideList() {
		for (int i = 0; i < songButtonChildren.Count; i++) {
			SongButtonChild child = songButtonChildren[i];

			RectTransform childRt = child.GetComponent<RectTransform>();
			childRt.anchoredPosition = new Vector2(childRt.anchoredPosition.x, 0);

			child.gameObject.SetActive(false);
		}
	}

	public void OpenList() {

		for(int i = 0; i < songButtonChildren.Count; i++) {
			SongButtonChild child = songButtonChildren[i];

			RectTransform childRt = child.GetComponent<RectTransform>();
			childRt.anchoredPosition = new Vector2(childRt.anchoredPosition.x, -mainRt.rect.height - childRt.rect.height * i);

			child.gameObject.SetActive(true);
		}
	}

	public void AddSongButtonChild(SongButtonChild songButtonChild) {
		songButtonChildren.Add(songButtonChild);
	}
}
