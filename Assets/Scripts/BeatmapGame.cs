using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Drum {
    Left,
    Right
}

public enum Score {
    Great = 300,
    Okay = 100,
    Miss = 0
}

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(AudioSource))]
public class BeatmapGame : MonoBehaviour {
    
    public static BeatmapGame instance;
    public List<OsuParsers.Beatmaps.Objects.HitObject> hitObjects = new List<OsuParsers.Beatmaps.Objects.HitObject>();
    public List<Beat> beats = new List<Beat>();

    public Beat beatPrefab;
    public Transform beatHolder;
    public Transform beatMarker;
    public AudioClip drumSfx;

    public int beatHitGreatMs = 50;
    public int beatHitOkayMs = 100;
    public int beatHitMissMs = 300;
    public float beatSpeed = 3f;

    public float approachRate;
    public int songDuration;
    public int songTimer;

    private int approachMs = 1800;
    private int closestBeat = 0;
    private int latestSpawnedBeat = 0;
    private AudioSource musicSource;
    private AudioSource sfxSource;

    void Awake(){
        if (instance == null){
            instance = this;
            musicSource = GetComponents<AudioSource>()[0];
            sfxSource = GetComponents<AudioSource>()[1];
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
        approachRate = beatmap.DifficultySection.ApproachRate;
        approachMs = CalculateApproachMs();
        songTimer = 0;
        latestSpawnedBeat = 0;

        // songTimer = 10000;

        // Spawn beats.
        for(int i = 0; i < hitObjects.Count; i++){
            OsuParsers.Beatmaps.Objects.HitObject hitObject = hitObjects[i];

            if (hitObject.StartTime + -approachMs <= songTimer){
                SpawnHitObject(hitObject);
            }
        }

        // Read mp3 file from local disk and convert byte to PCM. Look at MP3Sharp.
        StartCoroutine(GameManager.GetBeatmapAudioClip(beatmap, PlayBeatmapSong));

        // GameManager.gameState = GameManager.gameState | GameState.Started;
    }

    private void PlayBeatmapSong(AudioClip audioClip){
        // Start song with delay of approachMs.
        StartCoroutine(DelayPlay(audioClip, approachMs * 2));
    }

    private IEnumerator DelayPlay(AudioClip audioClip, int ms){
        yield return new WaitForSeconds((float) ms/1000);

        musicSource.clip = audioClip;
        musicSource.Play();

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

        // songTimer += (int) (1);
        songTimer = (int)(musicSource.time * 1000);

        if (songTimer >= songDuration){
            OnSongEnd();
            return;
        }

        // Spawn beats.
        for(int i = latestSpawnedBeat; i < hitObjects.Count; i++){
            OsuParsers.Beatmaps.Objects.HitObject hitObject = hitObjects[i];

            if (hitObject.StartTime + -approachMs <= songTimer){
                SpawnHitObject(hitObject);
            }
        }

        // Move beats.
        for(int i = closestBeat; i < beats.Count; i++){
            Beat beat = beats[i];
            Transform beatTransform = beat.transform;

            beatTransform.position += -Vector3.forward * (Time.deltaTime * musicSource.pitch) * beatSpeed;

            // Perfect time formula: beat.hitObject.StartTime + beat.delay <= songTimer
            if (beat.hitObject.StartTime + beat.delay <= songTimer){
                HitDrum(Drum.Left);
            }

            // Miss.
            if (beat.hitObject.StartTime + beat.delay + beatHitOkayMs < songTimer){
                ScoreBeat(beat);
            }
        }

        if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Z)){
            HitDrum(Drum.Left);
        }
    }

    private Score CalculateScore(Beat beat){
        Score score = Score.Miss;
        int startTime = beat.hitObject.StartTime + beat.delay;

        print(string.Format("startTime: {0}, hitTime: {1}", startTime, songTimer));

        if (Mathf.Abs(songTimer - startTime) <= beatHitGreatMs){
            score = Score.Great;
        } else if (Mathf.Abs(songTimer - startTime) <= beatHitOkayMs){
            score = Score.Okay;
        }

        return score;
    }

    private void ScoreBeat(Beat beat){
        beat.gameObject.SetActive(false);
        closestBeat += 1;

        GameManager.UpdateScore((int) CalculateScore(beat));
    }

    // https://osu.ppy.sh/help/wiki/Beatmap_Editor/Song_Setup#approach-rate
    private int CalculateApproachMs(){
        int approachMs = 1800;
        
        if (approachRate < 5){
            return approachMs - (int) approachRate * 120;
        } else {
            return approachMs - (int) approachRate * 150;
        }
    }

    public void HitDrum(Drum drum){
        // Get closest beat within song timer.
        Beat beat = GetClosestBeat();

        if (beat){
            int startTime = beat.hitObject.StartTime + beat.delay;

            // startTime > songTimer = +offset
            // startTime < songTimer = -offset
            // startTime == songTimer = 0
            if (startTime - beatHitMissMs >= songTimer){
                print("Don't register drum hits.");
                return;
            }

            sfxSource.PlayOneShot(drumSfx);
            ScoreBeat(beat);
        }
    }

    public Beat GetClosestBeat(){
        if (beats.Count > closestBeat){
            return beats[closestBeat];
        }

        return null;
    }

    private void SpawnHitObject(OsuParsers.Beatmaps.Objects.HitObject hitObject){
        if (hitObject is OsuParsers.Beatmaps.Objects.Slider){
            // Spawn extra beats.
            OsuParsers.Beatmaps.Objects.Slider slider = (OsuParsers.Beatmaps.Objects.Slider) hitObject;
            SpawnSlider(slider);

        } else if (hitObject is OsuParsers.Beatmaps.Objects.Taiko.TaikoDrumroll){
            OsuParsers.Beatmaps.Objects.Taiko.TaikoDrumroll taikoDrumroll = (OsuParsers.Beatmaps.Objects.Taiko.TaikoDrumroll) hitObject;

            SpawnSlider(taikoDrumroll);
        } else {
            SpawnBeat(hitObject);
        }
    }

    private void SpawnBeat(OsuParsers.Beatmaps.Objects.HitObject hitObject, int delay = 0){
        Beat beat = Instantiate(beatPrefab, beatHolder);
        beat.hitObject = hitObject;
        beat.delay = delay;

        beat.name = beat.name + latestSpawnedBeat;
        beat.transform.position = beatMarker.position + Vector3.forward * ((float)(hitObject.StartTime + delay - songTimer)/(1000f / beatSpeed));

        if (hitObject is OsuParsers.Beatmaps.Objects.Slider){
            beat.GetComponent<MeshRenderer>().material.color = Color.black;
        }

        beats.Add(beat);
        latestSpawnedBeat += 1;
    }

    private void SpawnSlider(OsuParsers.Beatmaps.Objects.Slider slider){
        print(string.Format("slider.EndTime{0}, slider.StartTime{1}, slider.Repeats{2}", slider.EndTime, slider.StartTime, slider.Repeats));

        // slider.Repeats can be 1.
        // slider by itself should count as 2 beats.

        for(int i = 0; i < slider.Repeats + 1; i++){
            // print(((slider.EndTime - slider.StartTime) / slider.Repeats) * i);
            SpawnBeat((OsuParsers.Beatmaps.Objects.HitObject) slider, ((slider.EndTime - slider.StartTime) / slider.Repeats) * i);
        }
    }

    public void OnSongEnd(){
        GameManager.Stop();
    }
}
