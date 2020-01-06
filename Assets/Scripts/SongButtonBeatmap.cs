using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SongButtonBeatmap : MonoBehaviour
{
	private Button button;
	private string beatmapTitle;
	private int beatmapId;

	public TMPro.TextMeshProUGUI songVersionText;
	public TMPro.TextMeshProUGUI songDifficultyText;
	public TMPro.TextMeshProUGUI songOriginalModeText;

	private void Awake(){
		button = GetComponent<Button>();
		button.onClick.AddListener(PlayBeatmap);
	}

	public void PlayBeatmap(){
		GameManager.instance.StartBeatmap(beatmapTitle, beatmapId);
	}

	public void SetBeatmapData(string oszName, int id){
		beatmapTitle = oszName;
		beatmapId = id;
	}

	public void UpdateButton(string version, string difficulty, string mode) {
		songVersionText.text = version;
		songDifficultyText.text = difficulty;
		songOriginalModeText.text = mode;
	}

	public void UpdateButton(string version, string difficulty) {
		songVersionText.text = version;
		songDifficultyText.text = difficulty;
	}
}
