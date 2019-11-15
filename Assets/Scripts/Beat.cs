using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Beat : MonoBehaviour
{
    public OsuParsers.Beatmaps.Objects.HitObject hitObject;
    public TaikoColorExtended color = TaikoColorExtended.Blue;
    public int delay = 0;
    public int offset = 0;
}
