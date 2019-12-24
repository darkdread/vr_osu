using UnityEngine;
public class AutoScaler : MonoBehaviour
{
    [SerializeField]
    private float defaultHeight = 1.8f;
    [SerializeField]
    private Camera camera;
    private void Resize()
    {
        float headHeight = camera.transform.localPosition.y;
        headHeight = 1f;
        float scale = defaultHeight / headHeight;

        print(scale);

        transform.localScale = Vector3.one * scale;
    }

    void OnEnable()
    {
        Resize();
    }
}