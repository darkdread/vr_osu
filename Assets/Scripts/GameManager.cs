using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;
using System.IO.Compression;
using System.Text;

public enum GameState {
    Started = 0x01,
    Paused = 0x02,
    Resuming = 0x04,
}

public class GameManager : MonoBehaviour {

    public static GameManager instance;
    public static GameState gameState; 
    
    // https://github.com/mrflashstudio/OsuParsers.
    public static string beatmapRepositoryPath = "Assets/Resources/Beatmaps/";
    public static string beatmapExtractedPath = "Assets/Resources/Beatmaps/Temp/";
    public static string beatmapRankingPath = "Assets/Resources/Beatmaps/Rankings/";

    public static string currentBeatmapOszName;
    public static string currentBeatmapSongTitle;
    public static string currentBeatmapSongVersion;
    public static int currentBeatmapOsuId;
    public static bool production = true;

    private static SongVersionRanking currentSongVersionRanking;

    private static Dictionary<string, List<OsuParsers.Beatmaps.Beatmap>> beatmapsDictionary = new Dictionary<string, List<OsuParsers.Beatmaps.Beatmap>>();
    private static Dictionary<string, AudioClip> beatmapsAudioClipDictionary = new Dictionary<string, AudioClip>();

    private GameObject lastSelectedGameObject;

    [Header("Game UI")]
    public Transform gameMenu;
    public Text scoreText;
    public Text accuracyText;
    public Text comboText;
    public Slider songProgressSlider;
    public Transform gameMenuHitTextTransform;

    [Header("Song Menu UI")]
    public Transform songMenu;
    public Transform songMenuScrollRect;
	public Transform songMenuContent;
	public SongButtonHeader songButtonPrefab;
    public GameObject songButtonContainerPrefab;
	public SongButtonBeatmap songButtonChildPrefab;

    public Transform songButtonRankingContent;
    public SongButtonRanking songButtonRankingPrefab;
    public GameObject currentSelectedSongHeader;
    public GameObject currentSelectedSongBeatmap;
    public GameObject currentSelectedSongButtonRankingButton;
    public AudioSource previewAudioSource;

    [Header("Song Menu Pause UI")]
    public Transform songMenuPauseMenu;
    public Button songMenuPauseContinueButton;
    public Button songMenuPauseRetryButton;
    public Button songMenuPauseOptionsButton;
    public Button songMenuPauseMainMenuButton;

    [Header("Song Menu Options UI")]
    public Transform songMenuOptionsMenu;
    public Slider songMenuOptionsSfxSlider;
    public Slider songMenuOptionsMusicSlider;
    public Toggle songMenuOptionsVfxToggle;
    public Button songMenuOptionsBackButton;
    public AudioSource optionsAudioSource;

    [Header("Song Grade UI")]
    public Transform gradeMenu;
    public TMPro.TextMeshProUGUI gradeText;
    public TMPro.TextMeshProUGUI greatText;
    public TMPro.TextMeshProUGUI goodText;
    public TMPro.TextMeshProUGUI missText;
    public TMPro.TextMeshProUGUI gradeComboText;
    public TMPro.TextMeshProUGUI gradeAccuracyText;
    public TMPro.TextMeshProUGUI gradeScoreText;
    public Button gradeBackButton;
    public XCharts.LineChart lineChart;

    public static bool backToSelectRank = false;

    [Header("Skybox")]
    public Material skyboxMaterial;
    public float skyboxRotation = 0;
    public float skyboxRotationSpeed = 1;

	private void SetQuality(){
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 144;

        RenderSettings.skybox = skyboxMaterial;
    }

    public static void PreviewSong(AudioClip clip){
        if (clip == instance.previewAudioSource.clip){
            return;
        }

        BeatmapGame.instance.musicSource.Stop();
        instance.previewAudioSource.clip = clip;
		instance.previewAudioSource.Play();
    }

    public static void StopPreview(){
        instance.previewAudioSource.Stop();
        instance.previewAudioSource.clip = null;
    }

    public static void PlayVolumeTestSfx(float volume){
        instance.optionsAudioSource.volume = volume;
        instance.optionsAudioSource.PlayOneShot(BeatmapGame.instance.drumSfx);
    }

    public static void UpdateMusicVolume(float volume){
        BeatmapGame.instance.musicSource.volume = volume;
        instance.previewAudioSource.volume = volume;

        PlayVolumeTestSfx(volume);

        PlayerPrefs.SetFloat("musicVolume", volume);
    }

    public static void UpdateSfxVolume(float volume){
        BeatmapGame.instance.sfxSource.volume = volume;

        PlayVolumeTestSfx(volume);

        PlayerPrefs.SetFloat("sfxVolume", volume);
    }

    public static void UpdateVfx(bool active){
        BeatmapGame.instance.showVisualEffects = active;

        int isActive = active ? 1 : 0;
        PlayerPrefs.SetInt("vfxActive", isActive);
    }

    private IEnumerator DelayResumeSong(int ms){
        gameState = gameState | GameState.Resuming;
        ShowPauseMenu(false);

        UpdateSongProgressSliderColor(Color.red);
        UpdateSongProgressSlider((float) 1f);

        for(int i = ms; i > 0; i -= (int) (Time.deltaTime * 1000f)){
            GameManager.UpdateSongProgressSlider((float) i/ms);
            yield return new WaitForEndOfFrame();
        }

        gameState = gameState & ~GameState.Resuming;
        GameManager.UpdateSongProgressSliderColor(Color.green);

        Pause(false);
    }
    
    private void Awake(){
        if (instance == null){
            instance = this;

            #if UNITY_EDITOR
                production = false;
            #endif

            // Initialize start of game.
            SetQuality();

            ShowSongMenu(true);
            ShowPauseMenu(false);
            ShowOptionsMenu(false);
            ShowGameMenu(false);
            ShowGradeMenu(false);

            // Ranking menu.
            gradeBackButton.onClick.AddListener(delegate{
                ShowGradeMenu(false);
                ShowSongMenu(true, !backToSelectRank);
            });

            // Game Pause Menu.
            songMenuPauseContinueButton.onClick.AddListener(delegate{
                StartCoroutine(DelayResumeSong(1000));
            });

            songMenuPauseRetryButton.onClick.AddListener(delegate{
                Pause(false);
                StartBeatmap(currentBeatmapOszName, currentBeatmapOsuId);
            });

            songMenuPauseOptionsButton.onClick.AddListener(delegate{
                ShowPauseMenu(false);
                ShowOptionsMenu(true);
            });

            songMenuPauseMainMenuButton.onClick.AddListener(delegate{
                Pause(false);
                Stop();
                PreviewSong(GetBeatmapAudioClip(currentSelectedSongHeader.GetComponent<SongButtonHeader>().songBeatmap, currentBeatmapOszName));
                ShowGameMenu(false);
                ShowSongMenu(true);
            });

            // Game Options Menu.
            songMenuOptionsBackButton.onClick.AddListener(delegate{
                ShowOptionsMenu(false);
                ShowPauseMenu(true);
            });

            songMenuOptionsMusicSlider.onValueChanged.AddListener(delegate{
                UpdateMusicVolume(songMenuOptionsMusicSlider.value);
            });

            songMenuOptionsSfxSlider.onValueChanged.AddListener(delegate{
                UpdateSfxVolume(songMenuOptionsSfxSlider.value);
            });

            songMenuOptionsVfxToggle.onValueChanged.AddListener(delegate{
                UpdateVfx(songMenuOptionsVfxToggle.isOn);
            });

            // Player prefs.
            songMenuOptionsMusicSlider.value = PlayerPrefs.GetFloat("musicVolume", 1f);
            songMenuOptionsSfxSlider.value = PlayerPrefs.GetFloat("sfxVolume", 1f);

            bool vfxActive = PlayerPrefs.GetInt("vfxActive", 1) == 1 ? true : false;
            songMenuOptionsVfxToggle.isOn = vfxActive;

            // For all builds other than UNITY_EDITOR. Modifies path to get files.
            if (production){
                beatmapRepositoryPath = Path.Combine(Application.persistentDataPath, beatmapRepositoryPath);
                beatmapExtractedPath = Path.Combine(Application.persistentDataPath, beatmapExtractedPath);
                beatmapRankingPath = Path.Combine(Application.persistentDataPath, beatmapRankingPath);
            }

            print(new DirectoryInfo(beatmapExtractedPath).FullName);
            print(new DirectoryInfo(beatmapRepositoryPath).FullName);
            print(new DirectoryInfo(beatmapRankingPath).FullName);

            DirectoryInfo tempInfo = new DirectoryInfo(beatmapExtractedPath);

            // Create Temp folder if doesn't exist.
            if (!tempInfo.Exists){
                tempInfo.Create();
            }

            // Clear out Temp folder.
            if (!production){
                // foreach(FileInfo tempFile in tempInfo.GetFiles()){
                //     tempFile.Delete();
                // }
            }

            // Get all osz files.
            DirectoryInfo info = new DirectoryInfo(beatmapRepositoryPath);
            FileInfo[] fileInfo = info.GetFiles("*.osz", SearchOption.AllDirectories);

            // Open all osz files.
            foreach(FileInfo oszFile in fileInfo){
                ZipArchive zipArchive = ZipFile.OpenRead(oszFile.FullName);
                List<OsuParsers.Beatmaps.Beatmap> beatmaps = new List<OsuParsers.Beatmaps.Beatmap>();

                // Create a dictionary containing all oszFileName and their respective beatmaps.
                beatmapsDictionary.Add(oszFile.Name, beatmaps);

                // Create folder for osz. Skip adding osz if directory already exist.
                DirectoryInfo oszDirectory = new DirectoryInfo(beatmapExtractedPath + oszFile.Name);

                if (!oszDirectory.Exists){
                    oszDirectory.Create();
                } else {
                    // Load beatmaps from folder since it exist already.
                    beatmaps = LoadBeatmapsFromFolder(oszDirectory.FullName);
                    beatmapsDictionary[oszFile.Name] = beatmaps;
                    continue;
                }

                // Loop through zipped content.
                foreach(ZipArchiveEntry entry in zipArchive.Entries){
                    string extractOsuPath = Path.Combine(oszDirectory.FullName, entry.FullName);
                    // print(extractOsuPath);

                    // Osu file, parse to beatmap.
                    if (entry.FullName.EndsWith(".osu")){
                        entry.ExtractToFile(extractOsuPath);

                        OsuParsers.Beatmaps.Beatmap beatmap = OsuParsers.Decoders.BeatmapDecoder.Decode(extractOsuPath);
                        beatmaps.Add(beatmap);
                    }

                    // Mp3 file, assume it's the song..
                    if (entry.FullName.ToLower().EndsWith(".mp3")){
                        entry.ExtractToFile(extractOsuPath);
                    }
                }
            }

			// StartBeatmap("411894 Remo Prototype[CV_ Hanamori Yumiri] - Sendan Life.osz", 2);
			// StartBeatmap("554568 CHiCO with HoneyWorks - Pride Kakumei.osz", 2);
			// StartBeatmap("46862 UVERworld - CORE PRIDE (TV Size).osz", 3);
			// StartBeatmap("978759 L. V. Beethoven - Moonlight Sonata (Cranky Remix).osz", 1);
			// StartBeatmap("27509 Hanazawa Kana - Renai Circulation (Short Ver.).osz", 5);
			// StartBeatmap("16893 Banya - Beethoven Virus (Full ver.).osz", 5);
			// StartBeatmap("203734 JerryC - Canon Rock.osz", 5);
			// StartBeatmap("30768 Joe Inoue - CLOSER (TV Size).osz", 5);
			// StartBeatmap("109852 Foster The People - Pumped Up Kicks.osz", 1);

			LoadAllBeatmapsIntoMenu();
            PreloadAllBeatmapSong();
            //StartBeatmap("30768 Joe Inoue - CLOSER (TV Size).osz", 5);

		} else {
            Destroy(gameObject);
        }
    }

    public static void ShowGameMenu(bool show){
        instance.gameMenu.gameObject.SetActive(show);
    }

    private static SongVersionRanking GetSongVersionRanking(SongRanking sr, string version){
        for(int j = 0; j < sr.songVersionRankings.Length; j++){
            SongVersionRanking sdr = sr.songVersionRankings[j];

            if (sdr.songVersion == version){
                return sdr;
            }
        }

        SongVersionRanking songVersionRanking = new SongVersionRanking(){
            songVersion = version,
            songPlayRankings = new SongPlayRanking[0]
        };

        sr.songVersionRankings = sr.songVersionRankings.Append(songVersionRanking).ToArray();
        return songVersionRanking;
    }

    public static SongRanking LoadScore(string songTitle){
        // If file exist, load as string and return as SongRanking. We assume the file is in proper-json format.
        if (File.Exists(Path.Combine(beatmapRankingPath, $"{songTitle}.json"))){
            string jsonString = File.ReadAllText(Path.Combine(beatmapRankingPath, $"{songTitle}.json"));

            return JsonUtility.FromJson<SongRanking>(jsonString);
        }

        return null;
    }

    public static void SaveScore(SongPlayRanking ranking){
        SongRanking sr = LoadScore(currentBeatmapSongTitle);

        if (sr == null){
            sr = new SongRanking(){
                songTitle = currentBeatmapSongTitle,
                songVersionRankings = new SongVersionRanking[0]
            };
        }

        SongVersionRanking sdr = GetSongVersionRanking(sr, currentBeatmapSongVersion);
        sdr.songPlayRankings = sdr.songPlayRankings.Append(ranking).ToArray();

        string myJson = JsonUtility.ToJson(sr);

        if (!Directory.Exists(beatmapRankingPath)){
            Directory.CreateDirectory(beatmapRankingPath);
        }

        FileStream fs = File.Create(Path.Combine(beatmapRankingPath, $"{currentBeatmapSongTitle}.json"));
        byte[] jsonToBytes = new UTF8Encoding(true).GetBytes(myJson);

        fs.Write(jsonToBytes, 0, jsonToBytes.Length);
        fs.Close();
    }

    public static void CalculateLineChart(float[] accuracyChart){
        instance.lineChart.series.ClearData();
        for(int i = 0; i < accuracyChart.Length; i++){
            instance.lineChart.series.AddData("PerformanceChart", accuracyChart[i] * 100);
        }
    }

    public static void ShowGradeMenu(bool show){
        instance.gradeMenu.gameObject.SetActive(show);

        if (show){
            EventSystem.current.SetSelectedGameObject(instance.gradeBackButton.gameObject);
        }
    }

    public static void ShowSongMenu(bool show, bool selectSong = true){
        instance.songMenu.gameObject.SetActive(show);

        if (!show) {
            SongButtonHeader.HideAllList();
        } else {
            // Select song header in song menu.
            if (selectSong && instance.currentSelectedSongHeader){
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(instance.currentSelectedSongHeader);
            } else {
                // If song ranking button was selected, select it again.
                if (instance.currentSelectedSongButtonRankingButton){
                    SongButtonBeatmap isBeatmap = instance.currentSelectedSongBeatmap.GetComponent<SongButtonBeatmap>();

                    // Since we are selecting the song ranking button, we need to toggle
                    // open the menu of the selected song header.
                    if (isBeatmap){
                        instance.StartCoroutine(isBeatmap.GetSongButtonHeader().AnimateOpenList(0f));
                    }

                    EventSystem.current.SetSelectedGameObject(null);
                    EventSystem.current.SetSelectedGameObject(instance.currentSelectedSongButtonRankingButton);
                    
                    // State for grade menu back button to return to ranking button. (This current logic)
                    backToSelectRank = false;
                }
            }
        }
    }

    public static void ShowPauseMenu(bool show){
        instance.songMenuPauseMenu.gameObject.SetActive(show);

        if (show){
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(instance.songMenuPauseContinueButton.gameObject);
        }
    }

    public static void ShowOptionsMenu(bool show){
        instance.songMenuOptionsMenu.gameObject.SetActive(show);

        if (show){
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(instance.songMenuOptionsMusicSlider.gameObject);
        }
    }

    public static void CreateRankingUiFromSongVersionRanking(SongVersionRanking songVersionRanking){
        print("CreateRankingUiFromSongVersionRanking()");

        instance.currentSelectedSongButtonRankingButton = null;

        List<Button> buttons = new List<Button>();

        songVersionRanking.songPlayRankings = songVersionRanking.songPlayRankings.OrderByDescending(songPlayRanking => songPlayRanking.score).ToArray();

        for(int i = 0; i < songVersionRanking.songPlayRankings.Length; i++){
            SongPlayRanking songPlayRanking = songVersionRanking.songPlayRankings[i];
            SongButtonRanking songButtonRanking = Instantiate(instance.songButtonRankingPrefab, instance.songButtonRankingContent);

            if (i == 0){
                instance.currentSelectedSongButtonRankingButton = songButtonRanking.gameObject;
            }

            EventTrigger eventTrigger = (EventTrigger) songButtonRanking.gameObject.AddComponent(typeof (EventTrigger));
            EventTrigger.Entry eventTriggerEntry = new EventTrigger.Entry();
            eventTriggerEntry.eventID = EventTriggerType.Select;
            eventTriggerEntry.callback.AddListener((data) => {
                OnSongButtonRankingSelect(data);
            });

            eventTrigger.triggers.Add(eventTriggerEntry);

            songButtonRanking.SetRanking(songPlayRanking);
            buttons.Add(songButtonRanking.GetComponent<Button>());
        }

        for(int i = 0; i < songVersionRanking.songPlayRankings.Length; i++){
            Button b = buttons[i];
            Navigation n = b.navigation;

            n.mode = Navigation.Mode.Explicit;
            if (i > 0){
                n.selectOnUp = buttons[i - 1];
            }
            if (i < buttons.Count - 1){
                n.selectOnDown = buttons[i + 1];
            }

            b.navigation = n;
        }
    }

    public static void OnSongButtonBeatmapSelect(BaseEventData data){
        SongButtonBeatmap songButtonBeatmap = data.selectedObject.GetComponent<SongButtonBeatmap>();

        // Don't create ranking ui again if it's the same button.
        if (instance.currentSelectedSongBeatmap == songButtonBeatmap.gameObject){
            // return;
        }

        SongRanking songRanking = LoadScore(songButtonBeatmap.beatmapTitle);

        if (songRanking == null){
            return;
        }

        for(int i = 0; i < songRanking.songVersionRankings.Length; i++){
            SongVersionRanking songVersionRanking = songRanking.songVersionRankings[i];
            if (songVersionRanking.songVersion == songButtonBeatmap.songVersionText.text){
                // print(JsonUtility.ToJson(songVersionRanking));
                CreateRankingUiFromSongVersionRanking(songVersionRanking);
            }
        }

        // Last selected button beatmap. For switching between rank/beatmap.
        instance.currentSelectedSongBeatmap = data.selectedObject;
    }

    public static void OnSongButtonSelect(BaseEventData data){
        foreach(Transform t in instance.songButtonRankingContent){
            Destroy(t.gameObject);
        }

        RectTransform contentRt = instance.songMenuContent.GetComponent<RectTransform>();

        RectTransform containerRt = data.selectedObject.transform.parent.GetComponent<RectTransform>();
        RectTransform currentRt = data.selectedObject.GetComponent<RectTransform>();

        float x = contentRt.anchoredPosition.x;
        contentRt.anchoredPosition =
            (Vector2)instance.songMenuScrollRect.transform.InverseTransformPoint(contentRt.position)
            - (Vector2)instance.songMenuScrollRect.transform.InverseTransformPoint(currentRt.position);

        // 350f = 100 (height of header/2) + 200 (height of header) + 50 (spacing)
        contentRt.anchoredPosition = new Vector2(x, contentRt.anchoredPosition.y - 350f);
    }

    public static void OnSongButtonRankingSelect(BaseEventData data){
        instance.currentSelectedSongButtonRankingButton = data.selectedObject;
    }

    public static void PreloadAllBeatmapSong() {

		foreach(KeyValuePair<string, List<OsuParsers.Beatmaps.Beatmap>> kvp in beatmapsDictionary) {
			string oszName = kvp.Key;

            AudioClip clip = GetBeatmapAudioClip(kvp.Value[0], oszName);
            beatmapsAudioClipDictionary.Add(oszName, clip);
		}
	}

    public static int CalculateBeatsPerMinute(OsuParsers.Beatmaps.Beatmap beatmap){
        OsuParsers.Beatmaps.Objects.HitObject[] hitObjects = beatmap.HitObjects.ToArray();
        int beatCount = 0;

        foreach(OsuParsers.Beatmaps.Objects.HitObject hitObject in hitObjects){
            if (hitObject is OsuParsers.Beatmaps.Objects.Slider || hitObject is OsuParsers.Beatmaps.Objects.Taiko.TaikoDrumroll){
                OsuParsers.Beatmaps.Objects.Slider slider = (OsuParsers.Beatmaps.Objects.Slider) hitObject;
                beatCount += slider.Repeats + 1;
            } else {
                beatCount += 1;
            }
        }

        int duration = (hitObjects[hitObjects.Length - 1].EndTime - hitObjects[0].StartTime) / 1000;
        int bpm = (int) (((float) beatCount / duration) * 60);

        // beatCount = hitObjects.Length;
        return bpm;
    }

	public static void LoadAllBeatmapsIntoMenu() {
        // Destroy all existing beatmaps.
        foreach(Transform t in instance.songMenuContent){
            Destroy(t.gameObject);
        }

        instance.currentSelectedSongHeader = null;

        // Load all beatmaps in dictionary into buttons for song menu.
		foreach(KeyValuePair<string, List<OsuParsers.Beatmaps.Beatmap>> kvp in beatmapsDictionary) {
			string oszName = kvp.Key;

            Transform container = Instantiate(instance.songButtonContainerPrefab, instance.songMenuContent).transform;

			// Create main song button containing all difficulties.
			SongButtonHeader songButton = Instantiate(instance.songButtonPrefab, container);
			songButton.UpdateSongButton(oszName);
            songButton.UpdateSongBeatmap(kvp.Value[0], oszName);

            if (instance.currentSelectedSongHeader == null){
                instance.currentSelectedSongHeader = songButton.gameObject;
            }

            // Event trigger to enable auto-height panning on select.
            EventTrigger eventTrigger = (EventTrigger) songButton.gameObject.AddComponent(typeof (EventTrigger));
            EventTrigger.Entry eventTriggerEntry = new EventTrigger.Entry();
            eventTriggerEntry.eventID = EventTriggerType.Select;
            eventTriggerEntry.callback.AddListener((data) => {
                OnSongButtonSelect(data);
            });

            eventTrigger.triggers.Add(eventTriggerEntry);

            // Sort difficulty.
            // Difficulty isn't by approach rate =(
            // https://github.com/ppy/osu-difficulty-calculator
            // kvp.Value.Sort((x, y) => x.DifficultySection.ApproachRate.CompareTo(y.DifficultySection.ApproachRate));

            Dictionary<OsuParsers.Beatmaps.Beatmap, int> bpms = new Dictionary<OsuParsers.Beatmaps.Beatmap, int>();
            foreach (OsuParsers.Beatmaps.Beatmap beatmap in kvp.Value) {
                int bpm = CalculateBeatsPerMinute(beatmap);
                bpms.Add(beatmap, bpm);
            }

            bpms = bpms.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            List<OsuParsers.Beatmaps.Beatmap> orderedByBpms = bpms.Keys.ToList();

            print($"{kvp.Key}");

			foreach (OsuParsers.Beatmaps.Beatmap beatmap in kvp.Value.OrderBy(x => orderedByBpms.IndexOf(x))) {
                int songId = kvp.Value.IndexOf(beatmap);
				string beatmapVersion = beatmap.MetadataSection.Version;
                string beatmapMode = beatmap.GeneralSection.Mode.ToString();
				float difficulty = beatmap.DifficultySection.ApproachRate;
                int bpm = bpms[beatmap];

                print($"{beatmapVersion}: songId:{songId}: bpm:{bpm}");

                if (beatmap.GeneralSection.Mode == OsuParsers.Enums.Ruleset.Mania){
                    print(string.Format("Skipping parsing of: Osz: {0}, version: {1}, mode: {2}", oszName, beatmapVersion, beatmap.GeneralSection.Mode));
                    continue;
                }

				SongButtonBeatmap songButtonChild = Instantiate(instance.songButtonChildPrefab, container);

                // Event trigger to enable auto-height panning on select.
                EventTrigger eventTrigger2 = (EventTrigger) songButtonChild.gameObject.AddComponent(typeof (EventTrigger));
                EventTrigger.Entry eventTriggerEntry2 = new EventTrigger.Entry();
                eventTriggerEntry2.eventID = EventTriggerType.Select;
                eventTriggerEntry2.callback.AddListener((data) => {
                    OnSongButtonSelect(data);
                    OnSongButtonBeatmapSelect(data);
                });

                eventTrigger2.triggers.Add(eventTriggerEntry2);

				// By default, the child is inactive.
				songButtonChild.gameObject.SetActive(false);

                songButtonChild.beatmapTitle = beatmap.MetadataSection.Title;
                songButtonChild.SetBeatmapData(oszName, songId);
				// songButtonChild.UpdateButton(beatmapVersion, difficulty.ToString(), beatmapMode);
                songButtonChild.UpdateButton(beatmapVersion, bpm.ToString(), beatmapMode);
				songButton.AddSongButtonChild(songButtonChild);
			}
		}

        EventSystem.current.SetSelectedGameObject(instance.currentSelectedSongHeader);
	}

    public static List<OsuParsers.Beatmaps.Beatmap> LoadBeatmapsFromFolder(string path){
        List<OsuParsers.Beatmaps.Beatmap> beatmaps = new List<OsuParsers.Beatmaps.Beatmap>();
        DirectoryInfo directoryInfo = new DirectoryInfo(path);

        foreach(FileInfo fileInfo in directoryInfo.GetFiles()){
            // Osu file, parse to beatmap.
            if (fileInfo.FullName.EndsWith(".osu")){
                OsuParsers.Beatmaps.Beatmap beatmap = OsuParsers.Decoders.BeatmapDecoder.Decode(fileInfo.FullName);
                beatmaps.Add(beatmap);
            }
        }

        return beatmaps;
    }

    public static AudioClip GetBeatmapAudioClip(OsuParsers.Beatmaps.Beatmap beatmap, string oszName){
        // Check if dictionary already contains AudioClip for the beatmap. If true, returns it.
        if (beatmapsAudioClipDictionary.ContainsKey(oszName) && beatmapsAudioClipDictionary[oszName] is AudioClip){
            return beatmapsAudioClipDictionary[oszName];
        }

        // Get .mp3 from path and loads it using Resources API into memory.
        DirectoryInfo info = new DirectoryInfo(Path.Combine(beatmapExtractedPath, oszName));
        FileInfo audioFile = info.GetFiles(beatmap.GeneralSection.AudioFilename)[0];

        // C:\Users\Rey\Desktop\vr_osu\Assets\Resources\Beatmaps\Temp\30768 Joe Inoue - CLOSER (TV Size).osz\CLOSE BONUS.mp3
        print("file://" + audioFile.FullName);

        // "Beatmaps\\Temp\\30768 Joe Inoue - CLOSER (TV Size).osz\\"
        string resourcesPath = audioFile.DirectoryName.Split(new string[] {"Assets\\Resources\\"}, System.StringSplitOptions.None)[1];
        string path = Path.Combine(resourcesPath, Path.GetFileNameWithoutExtension(audioFile.Name));
        print("file://" + path);

        AudioClip clip = Resources.Load<AudioClip>(path);
        return clip;
    }

    // DEPRECATED: This uses MP3Sharp.
    public static IEnumerator GetBeatmapAudioClip(OsuParsers.Beatmaps.Beatmap beatmap, System.Action<AudioClip> callback){
        DirectoryInfo info = new DirectoryInfo(Path.Combine(beatmapExtractedPath, currentBeatmapOszName));
        FileInfo audioFile = info.GetFiles(beatmap.GeneralSection.AudioFilename)[0];

        print("file://" + audioFile.FullName);

        // Licensing issues.
        // UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + audioFile.FullName, AudioType.MPEG);

        UnityWebRequest www = UnityWebRequest.Get("file://" + audioFile.FullName);
        yield return www.SendWebRequest();

        if (www.isNetworkError){
            print(www.error);
        } else {
            // AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
            
            AudioClip audioClip = GetAudioClipFromMP3ByteArray(www.downloadHandler.data);
            callback(audioClip);
        }
    }

    // http://answers.unity.com/answers/632260/view.html
    // https://github.com/ZaneDubya/MP3Sharp
    // DEPRECATED: This uses MP3Sharp.
    private static AudioClip GetAudioClipFromMP3ByteArray( byte[] in_aMP3Data ){
        AudioClip l_oAudioClip = null;
        Stream l_oByteStream = new MemoryStream( in_aMP3Data );
        MP3Sharp.MP3Stream l_oMP3Stream = new MP3Sharp.MP3Stream( l_oByteStream );

        //Get the converted stream data
        MemoryStream l_oConvertedAudioData = new MemoryStream();
        byte[] l_aBuffer = new byte[ 2048 ];
        int l_nBytesReturned = -1;
        int l_nTotalBytesReturned = 0;

        while( l_nBytesReturned != 0 )
        {
            l_nBytesReturned = l_oMP3Stream.Read( l_aBuffer, 0, l_aBuffer.Length );
            l_oConvertedAudioData.Write( l_aBuffer, 0, l_nBytesReturned );
            l_nTotalBytesReturned += l_nBytesReturned;
        }

        Debug.Log( "MP3 file has " + l_oMP3Stream.ChannelCount + " channels with a frequency of " + l_oMP3Stream.Frequency );

        byte[] l_aConvertedAudioData = l_oConvertedAudioData.ToArray();
        Debug.Log( "Converted Data has " + l_aConvertedAudioData.Length + " bytes of data" );

        //Convert the byte converted byte data into float form in the range of 0.0-1.0
        float[] l_aFloatArray = new float[ l_aConvertedAudioData.Length / 2 ];

        for( int i = 0; i < l_aFloatArray.Length; i++ )
        {
            if( System.BitConverter.IsLittleEndian )
            {
                //Evaluate earlier when pulling from server and/or local filesystem - not needed here
                //Array.Reverse( l_aConvertedAudioData, i * 2, 2 );
            }
            
            //Yikes, remember that it is SIGNED Int16, not unsigned (spent a bit of time before realizing I screwed this up...)
            l_aFloatArray[ i ] = (float)( System.BitConverter.ToInt16( l_aConvertedAudioData, i * 2 ) / 32768.0f );
        }

        //  l_oAudioClip = AudioClip.Create( "MySound", l_aFloatArray.Length, 2, l_oMP3Stream.Frequency, false, false );
        l_oAudioClip = AudioClip.Create("MySound", l_aFloatArray.Length, 2, l_oMP3Stream.Frequency, false);
        l_oAudioClip.SetData( l_aFloatArray, 0 );

        return l_oAudioClip;
    }

    public void StartBeatmap(string oszName, int osuId){
        OsuParsers.Beatmaps.Beatmap beatmap = beatmapsDictionary[oszName][osuId];
        
        if ((gameState & GameState.Started) == GameState.Started){
            // Reset gamestate.
            gameState = 0;
        }

        ShowSongMenu(false);
        ShowGameMenu(true);
        currentBeatmapOszName = oszName;
        currentBeatmapSongTitle = beatmap.MetadataSection.Title;
        currentBeatmapSongVersion = beatmap.MetadataSection.Version;
        currentBeatmapOsuId = osuId;
        currentSelectedSongHeader = SongButtonHeader.GetSongButtonHeader(oszName).gameObject;
        print(currentSelectedSongHeader.GetComponent<SongButtonHeader>().songTitle.text);

        BeatmapGame.instance.StartBeatmap(beatmap);
    }

    public static void Stop(){
        gameState = gameState & ~GameState.Started;

        BeatmapGame.instance.RemoveAllBeats();
    }

    public static void UpdateScoreText(string score){
        instance.scoreText.text = "Score: " + score.PadLeft(10, '0');
    }

    public static void UpdateComboText(string combo){
        instance.comboText.text = $"Combo: {combo}x";
    }

    public static void UpdateAccuracyText(string accuracy){
        instance.accuracyText.text = $"Accuracy: {accuracy}";
    }

    public static void UpdateGradeText(string grade){
        instance.gradeText.text = grade;
    }

    public static float CalcAccuracy(float accuracy){
        return Mathf.Floor(accuracy * 10000) / 100f;
    }

    public static void UpdateScoreboardText(SongPlayRanking ranking){
        instance.gradeScoreText.text = ranking.score.ToString().PadLeft(10, '0');

        instance.greatText.text = ranking.beatsHit.Great.ToString() + "x";
        instance.goodText.text = ranking.beatsHit.Good.ToString() + "x";
        instance.missText.text = ranking.beatsHit.Miss.ToString() + "x";
        instance.gradeComboText.text = ranking.highestCombo.ToString() + "x";

        // Round to 2 sig fig.
        float accuracy = CalcAccuracy(ranking.accuracy);
        instance.gradeAccuracyText.text = accuracy.ToString() + "%";

        instance.gradeText.text = ranking.grade;

        CalculateLineChart(ranking.accuracyChart);
    }

    public static void UpdateSongProgressSliderColor(Color color){
        instance.songProgressSlider.fillRect.GetComponent<Image>().color = color;
    }

    public static void UpdateSongProgressSlider(float value){
        instance.songProgressSlider.value = value;
    }

    private void Update(){
        if ((gameState & GameState.Started) == GameState.Started){
            if (Input.GetButtonDown("Pause")){
                Pause((gameState & (GameState.Paused)) != GameState.Paused);
            }

            if (Input.GetKeyDown(KeyCode.R)){
                StartBeatmap(currentBeatmapOszName, currentBeatmapOsuId);
            }
        }

        // Enables toggling from Song Highscore menu to Song Menu.
        if (Input.GetButtonDown("Left Drum") && instance.songMenu.gameObject.activeSelf){
            SongButtonRanking sbr = lastSelectedGameObject.GetComponent<SongButtonRanking>();

            if (sbr){
                EventSystem.current.SetSelectedGameObject(instance.currentSelectedSongBeatmap);
            }
        }

        if (EventSystem.current.currentSelectedGameObject != lastSelectedGameObject){
            lastSelectedGameObject = EventSystem.current.currentSelectedGameObject;
        }

        // Rotate skybox.
        skyboxRotation += Time.deltaTime * skyboxRotationSpeed;
        skyboxMaterial.SetFloat("_Rotation", skyboxRotation);
    }

    public void Pause(bool toPause){
        // Game hasn't started or is resuming, can't pause.
        if ((gameState & (GameState.Started | GameState.Resuming)) != GameState.Started){
            print($"Can't pause because of current state: {gameState}");
            return;
        }

        if (toPause){
            gameState = gameState | GameState.Paused;
        } else {
            gameState = gameState & ~GameState.Paused;
        }

        if ((gameState & GameState.Paused) == GameState.Paused){
            ShowPauseMenu(true);
            BeatmapGame.instance.musicSource.Pause();
        } else {
            ShowPauseMenu(false);
            BeatmapGame.instance.musicSource.UnPause();
        }
    }
}
