using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Drum {
    Left,
    Right
}

[RequireComponent(typeof(AudioSource))]
public class BeatmapGame : MonoBehaviour {
    
    public static BeatmapGame instance;
    public List<OsuParsers.Beatmaps.Objects.HitObject> hitObjects = new List<OsuParsers.Beatmaps.Objects.HitObject>();
    public List<Transform> beats = new List<Transform>();

    public GameObject beatPrefab;
    public Transform beatHolder;
    public Transform beatMarker;

    public int beatSpawnDelay = -3000;
    public int songDuration;
    public int songTimer;

    private int latestSpawnedBeat = 0;
    private AudioSource audioSource;

    void Awake(){
        if (instance == null){
            instance = this;
            audioSource = GetComponent<AudioSource>();
        } else {
            Destroy(gameObject);
        }
    }

    public void StartBeatmap(OsuParsers.Beatmaps.Beatmap beatmap){
        print(string.Format("Starting beatmap: {0} {1}", beatmap.GeneralSection.AudioFilename, beatmap.MetadataSection.Version));
        
        // Check if it's osu!mania. If it is, prevent it from loading, because osu!mania has
        // points that can start consecutively. Meaning, 3 keys at a single time.
        if (beatmap.GeneralSection.Mode == OsuParsers.Enums.Ruleset.Mania){
            print(string.Format("Oh shit I'm sorry, we don't support osu!mania."));
            return;
        }

        hitObjects = beatmap.HitObjects;
        songDuration = beatmap.HitObjects[beatmap.HitObjects.Count - 1].EndTime;
        songTimer = 0;
        latestSpawnedBeat = 0;

        // songTimer = 10000;

        // Spawn beats.
        for(int i = 0; i < hitObjects.Count; i++){
            OsuParsers.Beatmaps.Objects.HitObject hitObject = hitObjects[i];

            if (hitObject.StartTime + beatSpawnDelay <= songTimer){
                SpawnBeat(hitObject);
            }
        }

        // Read mp3 file from local disk and convert byte to PCM. Look at MP3Sharp.
        StartCoroutine(GameManager.GetBeatmapAudioClip(beatmap, PlayBeatmapSong));

        // GameManager.gameState = GameManager.gameState | GameState.Started;
    }

    private void PlayBeatmapSong(AudioClip audioClip){
        audioSource.clip = audioClip;
        audioSource.time = songTimer;
        audioSource.Play();

        GameManager.gameState = GameManager.gameState | GameState.Started;
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

        // Spawn beats.
        for(int i = latestSpawnedBeat; i < hitObjects.Count; i++){
            OsuParsers.Beatmaps.Objects.HitObject hitObject = hitObjects[i];

            if (hitObject.StartTime + beatSpawnDelay <= songTimer){
                SpawnBeat(hitObject);
            }
        }

        // Move beats.
        for(int i = 0; i < beats.Count; i++){
            Transform beat = beats[i];

            beat.position += -Vector3.forward * Time.deltaTime * 10;
        }

        if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Z)){
            HitDrum(Drum.Left);
        }
    }

    public void HitDrum(Drum drum){
        // Get closest beat within song timer.
        GameObject beat = GetClosestBeat();
        print(beat);

        beat.SetActive(false);
    }

    public GameObject GetClosestBeat(){
        for(int i = 0; i < hitObjects.Count; i++){
            OsuParsers.Beatmaps.Objects.HitObject hitObject = hitObjects[i];

            if (hitObject.StartTime < songTimer){
                continue;
            }

            return beats[i].gameObject;
        }

        return null;
    }

    private void SpawnBeat(OsuParsers.Beatmaps.Objects.HitObject hitObject){
        GameObject beat = Instantiate(beatPrefab, beatHolder);
        beat.name = beat.name + latestSpawnedBeat;
        beat.transform.position = beatMarker.position + Vector3.forward * ((float)(hitObject.StartTime - songTimer)/100f);
        beats.Add(beat.transform);

        latestSpawnedBeat += 1;
    }

    public void OnSongEnd(){
        GameManager.Stop();
    }
}
