using UnityEngine;
using VRTK;

public class DrumStick : MonoBehaviour
{
    private void OnTriggerEnter(Collider collider)
    {
        Drum d = BeatmapGame.GetDrum(collider.transform);
        BeatmapGame.instance.HitDrum(d);
        print(collider);
    }
}