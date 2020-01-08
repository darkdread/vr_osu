using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SongButtonRanking : MonoBehaviour
{
	private Button button;
    private SongPlayRanking songPlayRanking;

	public TMPro.TextMeshProUGUI songGradeText;
	public TMPro.TextMeshProUGUI songScoreText;
    public TMPro.TextMeshProUGUI songComboText;
	public TMPro.TextMeshProUGUI songAccuracyText;

	private void Awake(){
		button = GetComponent<Button>();
		button.onClick.AddListener(ShowRank);
	}

    private void ShowRank(){
        GameManager.UpdateScoreboardText(songPlayRanking);

        GameManager.ShowGameMenu(false);
        GameManager.ShowSongMenu(false);
        GameManager.ShowGradeMenu(true);

        GameManager.backToSelectRank = true;
    }

    public void SetRanking(SongPlayRanking ranking){
        songPlayRanking = ranking;

        UpdateButton(ranking.grade, ranking.score, ranking.highestCombo, ranking.accuracy);
    }

	public void UpdateButton(string grade, int score, int combo, float accuracy) {
		songGradeText.text = grade;
		songScoreText.text = $"Score: {score.ToString()}";
        songComboText.text = $"Combo: {combo.ToString()}x";
		songAccuracyText.text = $"{GameManager.CalcAccuracy(accuracy).ToString()}%";
	}
}
