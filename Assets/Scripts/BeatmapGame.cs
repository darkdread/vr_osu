using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

static class ButtonsMapping {
    public static string LeftDrum = "Left Drum";
    public static string RightDrum = "Right Drum";
    public static string LeftSideDrum = "Left Side Drum";
    public static string RightSideDrum = "Right Side Drum";
}

public enum TaikoColorExtended {
    Red = 0,
    Blue = 1,
    Green = 2,
    White = 3
}

public enum Drum {
    Empty = -1,
    Right = 0,
    Left = 1,
    LeftSide = 2,
    RightSide = 3
}

[System.Serializable]
public class SongRanking {
    public string songTitle;
    public SongVersionRanking[] songVersionRanking;
}

[System.Serializable]
public class SongVersionRanking {
    public string songVersion;
    public SongPlayRanking[] rankings;
}

[System.Serializable]
public class SongPlayRanking {
    public BeatsHit beatsHit;
    public int score;
    public int highestCombo;
    public float accuracy;
    public string grade;
}

public enum Score {
    Great = 300,
    Good = 100,
    Miss = 0
}

[System.Serializable]
public class DrumTransformPair {
    public Drum drum;
    public Transform drumTransform;
    public Transform drumMarkerTransform;
}

[System.Serializable]
public struct BeatsHit {
    public int Great;
    public int Good;
    public int Miss;
}

public enum Grade {
    SS,
    S,
    A,
    B,
    F
}

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(AudioSource))]
public class BeatmapGame : MonoBehaviour {
    
    public static BeatmapGame instance;
    public const bool DEBUG_MODE = false;

    public List<OsuParsers.Beatmaps.Objects.HitObject> hitObjects = new List<OsuParsers.Beatmaps.Objects.HitObject>();
    public List<Beat> beats = new List<Beat>();

    [Header("Setup")]
    public List<DrumTransformPair> drumTransformPairs = new List<DrumTransformPair>();
    public Light gameLight;
    public Beat beatPrefab;
    public Transform beatHolder;
    public Transform lineIndicatorHolder;
    public GameObject lineIndicator;
    public Transform hitTextHolder;
    public GameObject hitTextPrefab;
    public AudioClip drumSfx;

    [Header("Options")]
    public bool autoHitGood = false;
    public bool autoHitGreat = false;
    public bool autoHitPerfect = false;

    public int beatHitGreatMs = 30;
    public int beatHitGoodMs = 50;
    public int beatHitRegisterAsHitMs = 100;
    public float beatSpeed = 20f;

    public float approachRate;
    public int beatmapDuration;
    public string songTitle;
    public int songTimer;

    public int songStartTimer = 0;
    public int score = 0;
    public int combo = 0;
    public int highestCombo = 0;
    public float accuracy = 1;
    public Grade grade;
    public BeatsHit beatsHit;

    // Time it takes for beatmap to end after last beat.
    private int beatmapFade = 3000;
    private int defaultApproachMs = 3000;
    private int approachMs = 1800;
    private int closestBeat = 0;
    private int latestSpawnedBeat = 0;
    public OsuParsers.Enums.Ruleset originalMode;
    public AudioSource musicSource;
    public AudioSource sfxSource;

    public static Drum GetDrum(Transform t)
    {
        foreach (DrumTransformPair drumTransformPair in instance.drumTransformPairs)
        {
            if (drumTransformPair.drumTransform == t)
            {
                return drumTransformPair.drum;
            }
        }

        return Drum.Empty;
    }

    private Drum ColorToDrum(TaikoColorExtended color){
        return (Drum) color;
    }

    private DrumTransformPair GetDrumMarkerTransformPair(Drum drum){
        for(int i = 0; i < drumTransformPairs.Count; i++){
            if (drumTransformPairs[i].drum == drum){
                return drumTransformPairs[i];
            }
        }

        return null;
    }

    private void SpawnLinesIndicator(){
        foreach(Transform t in lineIndicatorHolder){
            Destroy(t.gameObject);
        }

        // beatTransform.position = GetDrumMarkerTransformPair(ColorToDrum(beat.color)).drumMarkerTransform.position + Vector3.forward * ((float) (beat.offset - songTimer) / 1000) * beatSpeed;

        GameObject linePerfect = Instantiate(lineIndicator, lineIndicatorHolder);
        linePerfect.transform.position += Vector3.forward * (((float) 0 / 1000) * beatSpeed);
        linePerfect.name = "LinePerfectIndicator";

        GameObject lineGreat = Instantiate(lineIndicator, lineIndicatorHolder);
        lineGreat.transform.position += Vector3.forward * (((float) beatHitGreatMs / 1000) * beatSpeed);
        lineGreat.name = "LineGreatIndicator";

        GameObject lineGood = Instantiate(lineIndicator, lineIndicatorHolder);
        lineGood.transform.position += Vector3.forward * (((float) beatHitGoodMs / 1000) * beatSpeed);
        lineGood.name = "LineGoodIndicator";
    }

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
        GameManager.StopPreview();

        print(string.Format("Starting beatmap: {0} {1}", beatmap.GeneralSection.AudioFilename, beatmap.MetadataSection.Version));
        
        // Check if it's osu!mania. If it is, prevent it from loading, because osu!mania has
        // points that can start consecutively. Meaning, 3 keys at a single time.
        if (beatmap.GeneralSection.Mode == OsuParsers.Enums.Ruleset.Mania){
            print(string.Format("Oh shit I'm sorry, we don't support osu!mania."));
            return;
        }

        songTitle = beatmap.MetadataSection.Title;
        originalMode = beatmap.GeneralSection.Mode;
        hitObjects = beatmap.HitObjects;
        beatmapDuration = beatmap.HitObjects[beatmap.HitObjects.Count - 1].EndTime;
        approachRate = beatmap.DifficultySection.ApproachRate;
        approachMs = CalculateApproachMs();

        // Clear all old vars.
        beatsHit = new BeatsHit(){
            Great = 0,
            Good = 0,
            Miss = 0
        };

        songTimer = 0;
        latestSpawnedBeat = 0;
        closestBeat = 0;
        score = 0;
        combo = 0;
        highestCombo = 0;
        accuracy = CalculateAccuracy();

        GameManager.UpdateAccuracyText((accuracy * 100).ToString() + "%");
        GameManager.UpdateComboText(combo.ToString());
        GameManager.UpdateScoreText(score.ToString());

        musicSource.Stop();

        for(int i = 0; i < beats.Count; i++){
            Destroy(beats[i].gameObject);
        }

        beats.Clear();

        // Spawn beats.
        for(int i = 0; i < hitObjects.Count; i++){
            OsuParsers.Beatmaps.Objects.HitObject hitObject = hitObjects[i];

            if (hitObject.StartTime + -approachMs <= songTimer){
                SpawnHitObject(hitObject);
            }
        }

        // Read from Resources folder.
        AudioClip clip = GameManager.GetBeatmapAudioClip(beatmap, GameManager.currentBeatmapOszName);
        PlayBeatmapSong(clip);

        // Read mp3 file from local disk and convert byte to PCM. Look at MP3Sharp.
        // StartCoroutine(GameManager.GetBeatmapAudioClip(beatmap, PlayBeatmapSong));

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

        SpawnLinesIndicator();

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
        GameManager.UpdateSongProgressSlider((float) songTimer/beatmapDuration);

        if (songTimer >= beatmapDuration){
            OnSongEnd();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Q)){
            OnSongEnd();
            return;
        }

        // We allow player to skip ahead.
        if (songTimer + beatmapFade < hitObjects[0].StartTime){
            if (Input.GetButtonDown("Skip Ahead")){
                SkipAheadToFirstBeat();
            }
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
            beatTransform.position = GetDrumMarkerTransformPair(ColorToDrum(beat.color)).drumMarkerTransform.position + Vector3.forward * ((float) (beat.offset - songTimer) / 1000) * beatSpeed;

            // Miss.
            if (beat.offset + beatHitGoodMs < songTimer){
                ScoreBeat(beat, ColorToDrum(beat.color));
            }

            if (autoHitGood && beat.offset - beatHitGoodMs <= songTimer){
                HitDrum(ColorToDrum(beat.color));
            }

            if (autoHitGreat && beat.offset - beatHitGreatMs <= songTimer){
                HitDrum(ColorToDrum(beat.color));
            }

            // Perfect time formula: beat.hitObject.StartTime + beat.delay <= songTimer
            if (autoHitPerfect && beat.offset <= songTimer){
                HitDrum(ColorToDrum(beat.color));
            }
        }

        if (DEBUG_MODE || true)
        {
            if (Input.GetButtonDown(ButtonsMapping.LeftDrum))
            {
                HitDrum(Drum.Left);
            }

            if (Input.GetButtonDown(ButtonsMapping.RightDrum))
            {
                HitDrum(Drum.Right);
            }

            if (Input.GetButtonDown(ButtonsMapping.LeftSideDrum))
            {
                HitDrum(Drum.LeftSide);
            }

            if (Input.GetButtonDown(ButtonsMapping.RightSideDrum))
            {
                HitDrum(Drum.RightSide);
            }
        }
    }

    public static void SkipAheadToFirstBeat()
    {
        float firstBeatTime = (instance.hitObjects[0].StartTime - instance.beatmapFade) / 1000;
        if (instance.musicSource.time < firstBeatTime)
        {
            instance.musicSource.time = firstBeatTime;
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

        float deltaOffset = Mathf.Abs(songTimer - startTime);
        if (deltaOffset <= beatHitGreatMs){
            score = Score.Great;
        } else if (deltaOffset <= beatHitGoodMs){
            score = Score.Good;
        }

        return score;
    }

    private void ScoreBeat(Beat beat, Drum drum){
        beat.gameObject.SetActive(false);
        closestBeat += 1;

        Score gainedScore = CalculateScore(beat, drum);
        int gain = (int) gainedScore;

        if (gain > 0){
            combo += 1;

            if (highestCombo < combo){
                highestCombo = combo;
            }
        } else {
            combo = 0;
        }

        score += gain * combo;

        switch(gainedScore){
            case Score.Great:
                beatsHit.Great += 1;
                break;
            case Score.Good:
                beatsHit.Good += 1;
                break;
            case Score.Miss:
                beatsHit.Miss += 1;
                break;
        }

        accuracy = CalculateAccuracy();

        SpawnHitText(beat.transform.position, gainedScore.ToString());

        GameManager.UpdateAccuracyText((accuracy * 100).ToString() + "%");
        GameManager.UpdateScoreText(score.ToString());
        GameManager.UpdateComboText(combo.ToString());
    }

    // https://osu.ppy.sh/help/wiki/Accuracy#-osu!taiko
    private float CalculateAccuracy(){
        if (beatsHit.Great + beatsHit.Good + beatsHit.Miss == 0){
            return 0;
        }
        return (beatsHit.Great + 0.5f * (float) beatsHit.Good) / (beatsHit.Great + beatsHit.Good + beatsHit.Miss);
    }

    // https://osu.ppy.sh/help/wiki/FAQ#Grades
    private Grade CalculateGrade(){
        if (accuracy >= 1f){
            return Grade.SS;
        } else if (accuracy >= 0.95f){
            return Grade.S;
        } else if (accuracy >= 0.9f){
            return Grade.A;
        } else if (accuracy >= 0.8f){
            return Grade.B;
        } else {
            return Grade.F;
        }
    }

    private string GradeToString(Grade grade){
        switch(grade){
            case Grade.SS:
                return "SS";
            case Grade.S:
                return "S";
            case Grade.A:
                return "A";
            case Grade.B:
                return "B";
            case Grade.F:
                return "F";
        }

        return null;
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

    private IEnumerator SetMaterialEmissionColor(Material material, Color color){
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color);
        // gameLight.enabled = false;
        yield return new WaitForSeconds(0.1f);

        material.DisableKeyword("_EMISSION");
        // gameLight.enabled = true;
    }

    //length is how long the vibration should go for
    //strength is vibration strength from 0-1
    IEnumerator LongVibration(float length, float strength)
    {
        for (float i = 0; i < length; i += Time.deltaTime)
        {
            SteamVR_Controller.Input(0).TriggerHapticPulse((ushort)Mathf.Lerp(0, 3999, strength));
            yield return null;
        }
    }

    private void SpawnHitText(Vector3 position, string text){
        TextMeshProUGUI hitText = Instantiate(hitTextPrefab, hitTextHolder).GetComponent<TextMeshProUGUI>();
        hitText.text = text;

        Vector3 offset = Vector3.up * 1f;
        hitText.transform.position = position;

        Destroy(hitText.gameObject, 2f);
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

            DrumTransformPair drumTransformPair = GetDrumMarkerTransformPair(drum);

            StartCoroutine(LongVibration(0.1f, 1f));
            StartCoroutine(SetMaterialEmissionColor(drumTransformPair.drumTransform.GetComponent<MeshRenderer>().material, Color.white));

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

    private TaikoColorExtended ColorRandomize(OsuParsers.Enums.Beatmaps.TaikoColor taikoColor){
        // Takes in blue || red.
        int r = Random.Range(0, 100);

        // 30% chance to get to sides.
        if (r < 30){
            if (taikoColor == OsuParsers.Enums.Beatmaps.TaikoColor.Red){
                return TaikoColorExtended.White;
            } else {
                return TaikoColorExtended.Green;
            }
        }

        return (TaikoColorExtended) taikoColor;
    }

    private Beat GetPreviousBeat(){
        if (beats.Count <= 0){
            return null;
        }

        return beats[latestSpawnedBeat - 1];
    }

    private void SpawnBeat(OsuParsers.Beatmaps.Objects.HitObject hitObject, int delay = 0){
        Beat beat = Instantiate(beatPrefab, beatHolder);
        beat.hitObject = hitObject;
        beat.delay = delay;
        beat.offset = beat.hitObject.StartTime + delay;

        beat.name = beat.name + latestSpawnedBeat;

        Beat prevBeat = GetPreviousBeat();

        if (hitObject is OsuParsers.Beatmaps.Objects.Slider){
            beat.GetComponent<MeshRenderer>().material.color = Color.black;
        }

        OsuParsers.Enums.Beatmaps.TaikoColor originalColor = OsuParsers.Enums.Beatmaps.TaikoColor.Blue;

        if (hitObject is OsuParsers.Beatmaps.Objects.Taiko.TaikoHit){
            OsuParsers.Beatmaps.Objects.Taiko.TaikoHit taikoHit = (OsuParsers.Beatmaps.Objects.Taiko.TaikoHit) hitObject;
            originalColor = taikoHit.Color;
        } else {
            // Color randomizer logic here.

            int r = Random.Range(0, 2);
            if (r == 0){
                originalColor = OsuParsers.Enums.Beatmaps.TaikoColor.Blue;
            } else {
                originalColor = OsuParsers.Enums.Beatmaps.TaikoColor.Red;
            }
        }

        beat.color = ColorRandomize(originalColor);

        if (prevBeat){

            // If previous beat is within 50ms, force swap.
            if (beat.offset - 50 <= prevBeat.offset){

                // Bad algorithm... but randomize color every loop until it's not the same.
                while (beat.color == prevBeat.color){
                    beat.color = ColorRandomize(originalColor);
                }
            }
        }

        MeshRenderer meshRenderer = beat.GetComponent<MeshRenderer>();
        MeshRenderer lineMeshRenderer = beat.transform.Find("Line").GetComponent<MeshRenderer>();

        if (beat.color == TaikoColorExtended.Blue){
            meshRenderer.material.color = Color.blue;
            lineMeshRenderer.material.color = Color.blue;
            lineMeshRenderer.material.SetColor("_EmissionColor", Color.blue * 0.5f);
        } else if (beat.color == TaikoColorExtended.Red) {
            meshRenderer.material.color = Color.red;
            lineMeshRenderer.material.color = Color.red;
            lineMeshRenderer.material.SetColor("_EmissionColor", Color.red * 0.5f);
        } else if (beat.color == TaikoColorExtended.White) {
            meshRenderer.material.color = Color.white;
            lineMeshRenderer.material.color = Color.white;
            lineMeshRenderer.material.SetColor("_EmissionColor", Color.white * 0.5f);
        } else if (beat.color == TaikoColorExtended.Green) {
            meshRenderer.material.color = Color.green;
            lineMeshRenderer.material.color = Color.green;
            lineMeshRenderer.material.SetColor("_EmissionColor", Color.green * 0.5f);
        }

        // beat.transform.position = GetDrumTransformPair(ColorToDrum(beat.color)).drumMarkerTransform.position + Vector3.forward * ((float)(hitObject.StartTime + delay - songTimer)/(1000f / beatSpeed));
        beat.transform.position = GetDrumMarkerTransformPair(ColorToDrum(beat.color)).drumMarkerTransform.position + Vector3.forward * ((float) (beat.offset - songTimer) / 1000) * beatSpeed;

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

    private SongPlayRanking BuildRankObject(){
        SongPlayRanking ranking = new SongPlayRanking(){
            highestCombo = highestCombo,
            score = score,
            accuracy = accuracy,
            grade = GradeToString(grade),
            beatsHit = beatsHit
        };

        return ranking;
    }

    public void OnSongEnd(){
        print("OnSongEnd");
        grade = CalculateGrade();

        SongPlayRanking ranking = BuildRankObject();
        GameManager.SaveScore(ranking);
        GameManager.UpdateScoreboardText(ranking);

        GameManager.Stop();
        GameManager.ShowGameMenu(false);
        GameManager.ShowSongMenu(false);
        GameManager.ShowGradeMenu(true);
    }
}
