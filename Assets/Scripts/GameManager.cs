using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public enum GameState {
    Started = 0x01,
    Paused = 0x02
}

public class GameManager : MonoBehaviour {

    public static GameManager instance;
    public static GameState gameState; 
    
    // https://github.com/mrflashstudio/OsuParsers.
    public string beatmapRepository = @"Assets/Beatmaps/";
    private static List<OsuParsers.Beatmaps.Beatmap> beatmaps = new List<OsuParsers.Beatmaps.Beatmap>();
    
    void Awake(){
        if (instance == null){
            instance = this;
            DirectoryInfo info = new DirectoryInfo(beatmapRepository);
            FileInfo[] fileInfo = info.GetFiles("*.osu", SearchOption.AllDirectories);

            foreach(FileInfo file in fileInfo){
                // print(file.FullName);

                OsuParsers.Beatmaps.Beatmap beatmap = OsuParsers.Decoders.BeatmapDecoder.Decode(file.FullName);
                beatmaps.Add(beatmap);
            }

            StartBeatmap(beatmaps[0]);
        } else {
            Destroy(gameObject);
        }
    }

    public void StartBeatmap(OsuParsers.Beatmaps.Beatmap beatmap){
        if ((gameState & GameState.Started) == GameState.Started){
            return;
        }

        BeatmapGame.instance.hitObjects = beatmap.HitObjects;
        BeatmapGame.instance.songDuration = beatmap.HitObjects[beatmap.HitObjects.Count - 1].EndTime;

        gameState = gameState | GameState.Started;
    }

    public static void Stop(){
        gameState = gameState & ~GameState.Started;
    }

    // Update is called once per frame
    void Update(){
        // print((int) gameState);
        
        if ((gameState & GameState.Started) == GameState.Started){

            if (Input.GetKeyDown(KeyCode.Escape)){
                Pause((gameState & GameState.Paused) != GameState.Paused);
            }
        }
    }

    public void Pause(bool toPause){
        if (toPause){
            gameState = gameState | GameState.Paused;
        } else {
            gameState = gameState ^ GameState.Paused;
        }

        if ((gameState & GameState.Paused) == GameState.Paused){
            print("Paused");
        } else {
            print("Resumed");
        }
    }
}
