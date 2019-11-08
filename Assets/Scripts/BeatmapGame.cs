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

[System.Serializable]
public class DrumTransformPair {
    public Drum drum;
    public Transform transform;
}

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(AudioSource))]
public class BeatmapGame : MonoBehaviour {
    
    public static BeatmapGame instance;
    // public static Dictionary<Drum, Transform> beatMarkers = new Dictionary<Drum, Transform>();
    public List<OsuParsers.Beatmaps.Objects.HitObject> hitObjects = new List<OsuParsers.Beatmaps.Objects.HitObject>();
    public List<Beat> beats = new List<Beat>();

    public List<DrumTransformPair> drumTransformPairs = new List<DrumTransformPair>();
    public Beat beatPrefab;
    public Transform beatHolder;
    public AudioClip drumSfx;

    public int beatHitGreatMs = 30;
    public int beatHitOkayMs = 50;
    public int beatHitRegisterAsHitMs = 300;
    public float beatSpeed = 3f;

    public float approachRate;
    public int songDuration;
    public int songTimer;

    public int songStartTimer = 0;
    public int score = 0;

    private int defaultApproachMs = 3000;
    private int approachMs = 1800;
    private int closestBeat = 0;
    private int latestSpawnedBeat = 0;
    private AudioSource musicSource;
    private AudioSource sfxSource;

    private Drum ColorToDrum(OsuParsers.Enums.Beatmaps.TaikoColor color){
        if (color == OsuParsers.Enums.Beatmaps.TaikoColor.Blue){
            return Drum.Left;
        }

        return Drum.Right;
    }

    private DrumTransformPair GetDrumTransformPair(Drum drum){
        for(int i = 0; i < drumTransformPairs.Count; i++){
            if (drumTransformPairs[i].drum == drum){
                return drumTransformPairs[i];
            }
        }

        return null;
    }

    void Awake(){
        if (instance == null){
            instance = this;
            // beatMarkers.Add(Drum.Left, );
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

        // Clear all old vars.
        songTimer = 0;
        latestSpawnedBeat = 0;
        closestBeat = 0;
        score = 0;
        musicSource.Stop();

        for(int i = 0; i < beats.Count; i++){
            Destroy(beats[i].gameObject);
        }

        beats.Clear();

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
        songTimer = songStartTimer;

        StartCoroutine(DelayPlay(audioClip, 1000));
    }

    private IEnumerator DelayPlay(AudioClip audioClip, int ms){
        GameManager.UpdateSongProgressSliderColor(Color.red);
        GameManager.UpdateSongProgressSlider((float) 1f);

        // Just wait 1 second 4HEad. Because the game is lagging.
        yield return new WaitForSeconds(1f);

        for(int i = ms; i > 0; i -= (int) (Time.deltaTime * 1000f)){
            GameManager.UpdateSongProgressSlider((float) i/ms);
            yield return new WaitForEndOfFrame();
        }

        GameManager.UpdateSongProgressSliderColor(Color.green);

        musicSource.clip = audioClip;
        musicSource.time = songTimer / 1000;
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
        GameManager.UpdateSongProgressSlider((float) songTimer/songDuration);

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
        for(int i = 0; i < beats.Count; i++){
            Beat beat = beats[i];
            Transform beatTransform = beat.transform;

            if (!beat.gameObject.activeSelf){
                continue;
            }

            // beatTransform.position += -Vector3.forward * (Time.deltaTime * musicSource.pitch) * beatSpeed;
            beatTransform.position = GetDrumTransformPair(ColorToDrum(beat.color)).transform.position + Vector3.forward * ((float) (beat.offset - songTimer) / 1000) * beatSpeed;

            // Miss.
            if (beat.offset + beatHitOkayMs < songTimer){
                ScoreBeat(beat, Drum.Left);
            }

            // Perfect time formula: beat.hitObject.StartTime + beat.delay <= songTimer
            if (beat.offset <= songTimer){
                HitDrum(ColorToDrum(beat.color));
            }
        }

        if (Input.GetKeyDown(KeyCode.X)){
            HitDrum(Drum.Right);
        }

        if (Input.GetKeyDown(KeyCode.Z)){
            HitDrum(Drum.Left);
        }
    }

    private Score CalculateScore(Beat beat, Drum drum){
        Score score = Score.Miss;
        int startTime = beat.offset;

        print(string.Format("startTime: {0}, hitTime: {1}, color: {2}, drum: {3}", startTime, songTimer, beat.color, drum));

        // Miss.
        if (drum != ColorToDrum(beat.color)){
            return score;
        }

        if (Mathf.Abs(songTimer - startTime) <= beatHitGreatMs){
            score = Score.Great;
        } else if (Mathf.Abs(songTimer - startTime) <= beatHitOkayMs){
            score = Score.Okay;
        }

        return score;
    }

    private void ScoreBeat(Beat beat, Drum drum){
        beat.gameObject.SetActive(false);
        closestBeat += 1;

        score += (int) CalculateScore(beat, drum);
        GameManager.UpdateScoreText(score.ToString());
    }

    // https://osu.ppy.sh/help/wiki/Beatmap_Editor/Song_Setup#approach-rate
    private int CalculateApproachMs(){
        int approachMs = defaultApproachMs;
        
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
            int startTime = beat.offset;

            // startTime > songTimer = +offset
            // startTime < songTimer = -offset
            // startTime == songTimer = 0

            if (songTimer + beatHitRegisterAsHitMs <= startTime){
                print("Don't register drum hits.");
                return;
            }

            sfxSource.PlayOneShot(drumSfx);
            ScoreBeat(beat, drum);
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
        beat.offset = beat.hitObject.StartTime + delay;

        beat.name = beat.name + latestSpawnedBeat;

        if (hitObject is OsuParsers.Beatmaps.Objects.Slider){
            beat.GetComponent<MeshRenderer>().material.color = Color.black;
        }

        if (hitObject is OsuParsers.Beatmaps.Objects.Taiko.TaikoHit){
            OsuParsers.Beatmaps.Objects.Taiko.TaikoHit taikoHit = (OsuParsers.Beatmaps.Objects.Taiko.TaikoHit) hitObject;
            beat.color = taikoHit.Color;
        } else {
            if (beats.Count > 0){
                // Color randomizer logic here.
                Beat prevBeat = beats[latestSpawnedBeat - 1];

                // If previous beat is within 50ms, force swap.
                if (beat.offset - 50 <= prevBeat.offset){
                    beat.color = prevBeat.color == OsuParsers.Enums.Beatmaps.TaikoColor.Blue ? OsuParsers.Enums.Beatmaps.TaikoColor.Red : OsuParsers.Enums.Beatmaps.TaikoColor.Blue;
                } else {
                    int r = Random.Range(0, 2);
                    if (r == 0){
                        beat.color = OsuParsers.Enums.Beatmaps.TaikoColor.Blue;
                    } else {
                        beat.color = OsuParsers.Enums.Beatmaps.TaikoColor.Red;
                    }
                }
            }
        }

        if (beat.color == OsuParsers.Enums.Beatmaps.TaikoColor.Blue){
            beat.GetComponent<MeshRenderer>().material.color = Color.blue;
            beat.transform.Find("Line").GetComponent<MeshRenderer>().material.color = Color.blue;
            beat.transform.Find("Line").GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", Color.blue * 0.5f);
        } else {
            beat.GetComponent<MeshRenderer>().material.color = Color.red;
            beat.transform.Find("Line").GetComponent<MeshRenderer>().material.color = Color.red;
            beat.transform.Find("Line").GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", Color.red * 0.5f);
        }

        // beat.transform.position = GetDrumTransformPair(ColorToDrum(beat.color)).transform.position + Vector3.forward * ((float)(hitObject.StartTime + delay - songTimer)/(1000f / beatSpeed));
        beat.transform.position = GetDrumTransformPair(ColorToDrum(beat.color)).transform.position + Vector3.forward * ((float) (beat.offset - songTimer) / 1000) * beatSpeed;

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
