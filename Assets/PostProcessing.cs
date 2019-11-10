using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PostProcessing : MonoBehaviour
{
    public Camera cam;
    public Material material;

    private void OnRenderImage(RenderTexture src, RenderTexture dest){
        Graphics.Blit(src, dest, material);
    }

    private void Awake(){
        cam = GetComponent<Camera>();
        cam.depthTextureMode = cam.depthTextureMode | DepthTextureMode.Depth;
    }

}
