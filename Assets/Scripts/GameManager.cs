using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.IO;
using System.IO.Compression;

public enum GameState {
    Started = 0x01,
    Paused = 0x02
}

public class GameManager : MonoBehaviour {

    public static GameManager instance;
    public static GameState gameState; 
    
    // https://github.com/mrflashstudio/OsuParsers.
    public static string beatmapRepositoryPath = "Assets/Beatmaps/";
    public static string beatmapExtractedPath = "Assets/Beatmaps/Temp/";

    public static string currentBeatmapOszName;
    public static int currentBeatmapOsuId;
    public static bool production = false;

    private static Dictionary<string, List<OsuParsers.Beatmaps.Beatmap>> beatmapsDictionary = new Dictionary<string, List<OsuParsers.Beatmaps.Beatmap>>();

    [Header("Game UI")]
    public Transform gameMenu;
    public Text scoreText;
    public Text accuracyText;
    public Text comboText;
    public Slider songProgressSlider;

    [Header("Song Menu UI")]
    public Transform songMenu;
	public Transform songMenuContent;
	public SongButtonHeader songButtonPrefab;
    public GameObject songButtonContainerPrefab;
	public SongButtonBeatmap songButtonChildPrefab;

    [Header("Song Grade UI")]
    public Transform gradeMenu;
    public Text gradeText;
    public Button gradeBackButton;

    [Header("Skybox")]
    public Material skyboxMaterial;
    public float skyboxRotation = 0;
    public float skyboxRotationSpeed = 1;

	private void SetQuality(){
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 144;

        RenderSettings.skybox = skyboxMaterial;
    }
    
    void Awake(){
        if (instance == null){
            instance = this;

            SetQuality();
            gradeBackButton.onClick.AddListener(delegate{
                ShowGradeMenu(false);
                ShowSongMenu(true);
            });

            ShowSongMenu(true);
            ShowGameMenu(false);
            ShowGradeMenu(false);

            if (production){
                beatmapRepositoryPath = Path.Combine(Application.persistentDataPath, beatmapRepositoryPath);
                beatmapExtractedPath = Path.Combine(Application.persistentDataPath, beatmapExtractedPath);
            }

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
                beatmapsDictionary.Add(oszFile.Name, beatmaps);

                // Create folder for osz. Skip adding osz if directory already exist.
                DirectoryInfo oszDirectory = new DirectoryInfo(beatmapExtractedPath + oszFile.Name);

                if (!oszDirectory.Exists){
                    oszDirectory.Create();
                } else {
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

		} else {
            Destroy(gameObject);
        }
    }

    public static void ShowGameMenu(bool show){
        instance.gameMenu.gameObject.SetActive(show);
    }

    public static void ShowGradeMenu(bool show){
        instance.gradeMenu.gameObject.SetActive(show);
    }

    public static void ShowSongMenu(bool show){
        instance.songMenu.gameObject.SetActive(show);

        if (!show) {
            SongButtonHeader.HideAllList();
        }
    }

	public static void LoadAllBeatmapsIntoMenu() {

		foreach(KeyValuePair<string, List<OsuParsers.Beatmaps.Beatmap>> kvp in beatmapsDictionary) {
			string songName = kvp.Key;

            Transform container = Instantiate(instance.songButtonContainerPrefab, instance.songMenuContent).transform;

			// Create main song button containing all difficulties.
			SongButtonHeader songButton = Instantiate(instance.songButtonPrefab, container);
			songButton.UpdateSongButton(songName);

            int songId = 0;

            // Sort difficulty.
            // Difficulty isn't by approach rate =(
            // https://github.com/ppy/osu-difficulty-calculator
            kvp.Value.Sort((x, y) => x.DifficultySection.ApproachRate.CompareTo(y.DifficultySection.ApproachRate));

			foreach (OsuParsers.Beatmaps.Beatmap beatmap in kvp.Value) {
				string beatmapVersion = beatmap.MetadataSection.Version;
                string beatmapMode = beatmap.GeneralSection.Mode.ToString();
				float difficulty = beatmap.DifficultySection.ApproachRate;

                if (beatmap.GeneralSection.Mode == OsuParsers.Enums.Ruleset.Mania){
                    print(string.Format("Osz: {0}, version: {1}, mode: {2}", songName, beatmapVersion, beatmap.GeneralSection.Mode));
                    songId += 1;
                    continue;
                }

				SongButtonBeatmap songButtonChild = Instantiate(instance.songButtonChildPrefab, container);

				// By default, the child is inactive.
				songButtonChild.gameObject.SetActive(false);

                songButtonChild.SetBeatmapData(songName, songId);
				songButtonChild.UpdateButton(beatmapVersion, difficulty.ToString(), beatmapMode);
				songButton.AddSongButtonChild(songButtonChild);

                songId += 1;
			}
		}

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
        currentBeatmapOsuId = osuId;
        BeatmapGame.instance.StartBeatmap(beatmap);
    }

    public static void Stop(){
        gameState = gameState & ~GameState.Started;
    }

    public static void UpdateScoreText(string score){
        instance.scoreText.text = "Score: " + score.PadLeft(9, '0');
    }

    public static void UpdateComboText(string combo){
        instance.comboText.text = "Combo: " + combo;
    }

    public static void UpdateAccuracyText(string accuracy){
        instance.accuracyText.text = "Accuracy: " + accuracy;
    }

    public static void UpdateGradeText(string grade){
        instance.gradeText.text = "Grade: " + grade;
    }

    public static void UpdateSongProgressSliderColor(Color color){
        instance.songProgressSlider.fillRect.GetComponent<Image>().color = color;
    }

    public static void UpdateSongProgressSlider(float value){
        instance.songProgressSlider.value = value;
    }

    private void Update(){
        if ((gameState & GameState.Started) == GameState.Started){

            if (Input.GetKeyDown(KeyCode.Escape)){
                Pause((gameState & GameState.Paused) != GameState.Paused);
            }

            if (Input.GetKeyDown(KeyCode.R)){
                StartBeatmap(currentBeatmapOszName, currentBeatmapOsuId);
            }
        }

        // Rotate skybox.
        skyboxRotation += Time.deltaTime * skyboxRotationSpeed;
        skyboxMaterial.SetFloat("_Rotation", skyboxRotation);
    }

    public void Pause(bool toPause){
        // Game hasn't started, can't pause.
        if ((gameState & GameState.Started) != GameState.Started ){
            print("Can't pause an unstarted game");
            return;
        }

        if (toPause){
            gameState = gameState | GameState.Paused;
        } else {
            gameState = gameState ^ GameState.Paused;
        }

        if ((gameState & GameState.Paused) == GameState.Paused){
            print("Paused");
            ShowSongMenu(true);
            BeatmapGame.instance.musicSource.Pause();
        } else {
            print("Resumed");
            ShowSongMenu(false);
            BeatmapGame.instance.musicSource.UnPause();
        }
    }
}
