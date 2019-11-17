using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SongButtonHeader : MonoBehaviour
{
	private static List<SongButtonHeader> songButtons = new List<SongButtonHeader>();
	private Button button;

	private ScrollRect scrollView;
	private RectTransform containerRt;
	private RectTransform contentRt;
	private float originalHeight;
	private float songButtonBeatmapHeight;

	public Text songTitle;
	public List<SongButtonBeatmap> songButtonChildren;
	public bool isListOpen = false;

	private void Awake() {
		button = GetComponent<Button>();
		scrollView = GetComponentInParent<ScrollRect>();
		containerRt = GetComponentsInParent<RectTransform>()[1];
		contentRt = containerRt.transform.GetComponentsInParent<RectTransform>()[1];
		originalHeight = GetComponent<RectTransform>().rect.height;
		songButtons.Add(this);

		button.onClick.AddListener(OpenList);
	}

	public void UpdateSongButton(string title) {
		songTitle.text = title;
	}

	public void HideList() {
		isListOpen = false;

		for (int i = 0; i < songButtonChildren.Count; i++) {
			SongButtonBeatmap child = songButtonChildren[i];

			RectTransform childRt = child.GetComponent<RectTransform>();
			childRt.anchoredPosition = new Vector2(childRt.anchoredPosition.x, 0);

			child.gameObject.SetActive(false);
		}
	}

	public IEnumerator AnimateOpenList(float duration){
		float t = 0;

		for(int i = 0; i < songButtonChildren.Count; i++) {
			SongButtonBeatmap child = songButtonChildren[i];

			child.gameObject.SetActive(true);
		}

		// Wait one frame for game to update containerRt's new position.
		yield return new WaitForEndOfFrame();

		contentRt.anchoredPosition = new Vector2(contentRt.anchoredPosition.x, -containerRt.anchoredPosition.y - scrollView.GetComponent<RectTransform>().rect.height/3 - songButtonBeatmapHeight/2);
		// print(-containerRt.anchoredPosition.y - scrollView.GetComponent<RectTransform>().rect.height/3 - songButtonBeatmapHeight/2);

		if (contentRt.anchoredPosition.y <= 0f){
			contentRt.anchoredPosition = new Vector2(contentRt.anchoredPosition.x, 0f);
		}

		while(t < duration){

			if (!isListOpen){
				yield break;
			}

			for(int i = 0; i < songButtonChildren.Count; i++) {
				SongButtonBeatmap child = songButtonChildren[i];
				RectTransform childRt = child.GetComponent<RectTransform>();

				float endY = -originalHeight -childRt.rect.height/2 - childRt.rect.height * i;

				// childRt.anchoredPosition = new Vector2(childRt.anchoredPosition.x, -mainRtOriginalHeight/2 - childRt.rect.height/2 - childRt.rect.height * i);
				childRt.anchoredPosition = new Vector2(childRt.anchoredPosition.x, Mathf.Lerp(-originalHeight - childRt.rect.height/2, endY, t/duration));
				// print(childRt.anchoredPosition);

				child.gameObject.SetActive(true);
			}

			t += Time.unscaledDeltaTime;

			yield return new WaitForEndOfFrame();
		}
	}

	public void OpenList() {
		// Hide all lists that isn't the clicked menu.
		foreach(SongButtonHeader songButton in songButtons){
			if (songButton != this){
				songButton.HideList();
			}
		}

		isListOpen = true;
		StartCoroutine(AnimateOpenList(1f));
	}

	public static void HideAllList(){
		// Hide all lists that isn't the clicked menu.
		foreach(SongButtonHeader songButton in songButtons){
			songButton.HideList();
		}
	}

	public void AddSongButtonChild(SongButtonBeatmap songButtonChild) {
		songButtonBeatmapHeight += songButtonChild.GetComponent<RectTransform>().rect.height;
		songButtonChildren.Add(songButtonChild);
	}
}
