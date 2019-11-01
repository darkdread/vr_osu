using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OsuParsers;
using System.IO;

[Flags]
public enum GameState {
    Started = 0x01,
    Paused = 0x02
}

public class GameManager : MonoBehaviour {

    public static GameManager instance;
    public static GameState gameState; 
    
    // https://github.com/mrflashstudio/OsuParsers.
    public string beatmapRepository = "Assets/Beatmaps/";
    private static List<OsuParsers.Beatmaps.Beatmap> beatmaps = new List<OsuParsers.Beatmaps.Beatmap>();
    
    void Awake(){
        if (instance == null){
            instance = this;
            DirectoryInfo info = new DirectoryInfo(beatmapRepository);
            FileInfo[] fileInfo = info.GetFiles(".osu");

            foreach(FileInfo file in fileInfo){
                print(file.FullName);
                // OsuParsers.Beatmaps.Beatmap beatmap = OsuParsers.Decoders.BeatmapDecoder.Decode();
                // beatmaps.Add(beatmap);
            }
        } else {
            Destroy(gameObject);
        }
    }

    public void StartBeatmap(OsuParsers.Beatmap.Beatmap beatmap){
        if (gameState == GameState.Started){
            return;
        }

        BeatmapGame.instance.hitObjects = beatmap.HitObjects;
        // BeatmapGame.instance.songDuration = beatmap.

        gameState = gameState | GameState.Started;
    }

    // Update is called once per frame
    void Update(){
        
    }
}
