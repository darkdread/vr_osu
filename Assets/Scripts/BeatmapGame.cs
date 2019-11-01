using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OsuParsers;

public class BeatmapGame : MonoBehaviour {
    
    public static BeatmapGame instance;
    public List<OsuParsers.Beatmaps.Objects.HitObject> hitObjects = new List<OsuParsers.Beatmaps.Objects.HitObject>();

    public float songDuration;
    public float songTimer;

    void Awake(){
        if (instance == null){
            instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    // Update is called once per frame
    void Update(){
        if (GameManager.gameState == ~GameState.Started | GameState.Paused){
            return;
        }


    }
}
