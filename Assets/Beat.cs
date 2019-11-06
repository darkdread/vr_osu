using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Beat : MonoBehaviour
{
    public OsuParsers.Beatmaps.Objects.HitObject hitObject;
    public OsuParsers.Enums.Beatmaps.TaikoColor color = OsuParsers.Enums.Beatmaps.TaikoColor.Blue;
    public int delay = 0;
}
