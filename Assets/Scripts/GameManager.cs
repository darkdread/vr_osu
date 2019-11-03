using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
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
    public static string beatmapRepositoryPath = @"Assets/Beatmaps/";
    public static string beatmapTempPath = @"Assets/Beatmaps/Temp/";

    private static Dictionary<string, List<OsuParsers.Beatmaps.Beatmap>> beatmapsDictionary = new Dictionary<string, List<OsuParsers.Beatmaps.Beatmap>>();
    
    void Awake(){
        if (instance == null){
            instance = this;

            // Clear out Temp folder.
            DirectoryInfo tempInfo = new DirectoryInfo(beatmapTempPath);
            foreach(FileInfo tempFile in tempInfo.GetFiles()){
                tempFile.Delete();
            }

            // Get all osz files.
            DirectoryInfo info = new DirectoryInfo(beatmapRepositoryPath);
            FileInfo[] fileInfo = info.GetFiles("*.osz", SearchOption.AllDirectories);

            // Open all osz files.
            foreach(FileInfo oszFile in fileInfo){
                ZipArchive zipArchive = ZipFile.OpenRead(oszFile.FullName);
                List<OsuParsers.Beatmaps.Beatmap> beatmaps = new List<OsuParsers.Beatmaps.Beatmap>();
                beatmapsDictionary.Add(oszFile.Name, beatmaps);

                // Loop through zipped content.
                foreach(ZipArchiveEntry entry in zipArchive.Entries){

                    // Osu file, parse to beatmap.
                    if (entry.FullName.EndsWith(".osu")){
                        string extractOsuPath = Path.Combine(beatmapTempPath + entry.FullName);

                        entry.ExtractToFile(extractOsuPath);

                        OsuParsers.Beatmaps.Beatmap beatmap = OsuParsers.Decoders.BeatmapDecoder.Decode(extractOsuPath);
                        beatmaps.Add(beatmap);
                    }

                    // Mp3 file, assume it's the song..
                    if (entry.FullName.EndsWith(".mp3")){
                        string extractOsuPath = Path.Combine(beatmapTempPath + entry.FullName);

                        entry.ExtractToFile(extractOsuPath);
                    }
                }
            }

            StartBeatmap(beatmapsDictionary["24664 DOES - Donten (TV Size).osz"][1]);
        } else {
            Destroy(gameObject);
        }
    }

    public static IEnumerator GetBeatmapAudioClip(OsuParsers.Beatmaps.Beatmap beatmap, System.Action<AudioClip> callback){
        DirectoryInfo info = new DirectoryInfo(beatmapTempPath);
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

    public void StartBeatmap(OsuParsers.Beatmaps.Beatmap beatmap){
        if ((gameState & GameState.Started) == GameState.Started){
            return;
        }

        BeatmapGame.instance.StartBeatmap(beatmap);
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
