using UnityEngine;
using VRTK;

public class DrumStick : MonoBehaviour
{
    public bool isHittingDrum = false;

    private void OnTriggerEnter(Collider collider)
    {
        Lane d = BeatmapGame.GetLane(collider.transform);
        if (d != Lane.Empty)
        {
            if (isHittingDrum)
            {
                //return;
            }
            isHittingDrum = true;
            BeatmapGame.instance.HitDrum(d);
        } else
        {
            SkipSong skip = collider.GetComponent<SkipSong>();
            if (skip)
            {
                BeatmapGame.SkipAheadToFirstBeat();
            }
        }
        print(collider);
    }

    private void OnTriggerExit(Collider collider)
    {
        Lane d = BeatmapGame.GetLane(collider.transform);
        if (d == Lane.Empty)
        {
            isHittingDrum = false;
        }
    }
}