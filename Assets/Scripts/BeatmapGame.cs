using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeatmapGame : MonoBehaviour {
    
    public static BeatmapGame instance;
    public List<OsuParsers.Beatmaps.Objects.HitObject> hitObjects = new List<OsuParsers.Beatmaps.Objects.HitObject>();

    public GameObject beatPrefab;

    public int songDuration;
    public int songTimer;

    void Awake(){
        if (instance == null){
            instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    // Update is called once per frame
    void Update(){
        // Paused and Started.
        //  011
        // &011
        //  ---
        //  011 != 001

        // Paused and !Started.
        //  010
        // &011
        //  ---
        //  010 != 001

        // !Paused and Started.
        //  001
        // &011
        //  ---
        //  001 != 001

        // !Paused and !Started.
        //  000
        // &011
        //  ---
        //  000 != 001
        // print(GameManager.gameState & (GameState.Started | GameState.Paused));
        if ((GameManager.gameState & (GameState.Started | GameState.Paused)) != (GameState.Started)){
            return;
        }

        songTimer += (int) (Time.deltaTime * 1000);

        if (songTimer >= songDuration){
            OnSongEnd();
            return;
        }

        // Spawn beat.
        if (songTimer >= hitObjects[0].StartTime){
            hitObjects.RemoveAt(0);
        }
    }

    public void OnSongEnd(){
        GameManager.Stop();
    }
}
