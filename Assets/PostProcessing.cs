using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PostProcessing : MonoBehaviour
{
    public Camera cam;
    public Material material;

    public bool isZAWARUDO = false;

    [Header("Wave Effect")]
    public float _DistanceToCamera = 0f;
    public float _Speed = 100f;
    public float stopAtDistance = 100f;
    public Color _WaveColor = Color.blue;

    [Header("Implode Effect")]
    public float _ImplodeTimeToReachMax = 0.3f;
    public Color _ImplodeColor = Color.blue;

    private void OnRenderImage(RenderTexture src, RenderTexture dest){
        if (!isZAWARUDO){
            Graphics.Blit(src, dest, material);
            return;
        }
        
        material.SetColor("_WaveColor", _WaveColor);
        material.SetFloat("_DistanceFromCamera", _DistanceToCamera);

        material.SetColor("_ImplodeColor", _ImplodeColor);
        material.SetFloat("_ImplodeTimeToReachMax", _ImplodeTimeToReachMax);
        
        Graphics.Blit(src, dest, material);
    }

    private void Awake(){
        cam = GetComponent<Camera>();
        cam.depthTextureMode = cam.depthTextureMode | DepthTextureMode.Depth;
    }

    private void Update(){
        if (isZAWARUDO){
            _DistanceToCamera += Time.deltaTime * _Speed;

            if (_DistanceToCamera > stopAtDistance){
                isZAWARUDO = false;
            } else {
                return;
            }
        }

        if (Input.GetKeyDown(KeyCode.F)){
            ZAWARUDO();
        }
    }

    private void ZAWARUDO(){
        isZAWARUDO = true;

        material.SetFloat("_StartTime", Time.timeSinceLevelLoad);

        print(Time.timeSinceLevelLoad);
        _DistanceToCamera = 0f;
    }

}
