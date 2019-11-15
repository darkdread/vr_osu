using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SongButtonChild : MonoBehaviour
{
	public Text songVersionText;
	public Text songDifficultyText;

	public void UpdateButton(string version, string difficulty) {
		songVersionText.text = version;
		songDifficultyText.text = difficulty;
	}
}
