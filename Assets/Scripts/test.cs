using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OsuParsers;

public class test : MonoBehaviour
{
    //https://github.com/mrflashstudio/OsuParsers
    
    void Start()
    {
        OsuParsers.Beatmaps.Beatmap beatmap = OsuParsers.Decoders.BeatmapDecoder.Decode("Assets/Songs/HoneyWorks feat. sana - Ima Suki ni Naru. (Serizawa Haruki) [Infatuation].osu");

        for(int i = 0; i < beatmap.HitObjects.Count; i++) {
            OsuParsers.Beatmaps.Objects.HitObject hitObject = beatmap.HitObjects[i];
            print(hitObject.StartTime);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
