using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PostProcessing : MonoBehaviour
{
    public Camera cam;
    public Material material;

    public bool isZAWARUDO = false;

    public float _DistanceToCamera = 0f;
    public float _Speed = 5f;
    public float stopAtDistance = 10f;

    public Color _ZaWarudoColor = Color.blue;

    private float PingPong(float value, float length){
        if (value > length){
            return length - value % length;
        }

        return value;
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest){
        if (!isZAWARUDO){
            Graphics.Blit(src, dest, material);
            return;
        }
        float circle = Mathf.PingPong(_DistanceToCamera, stopAtDistance);
        material.SetColor("_ZaWarudoColor", _ZaWarudoColor);
        material.SetFloat("_DistanceFromCamera", _DistanceToCamera);
        material.SetFloat("_PingPong", circle);
        Graphics.Blit(src, dest, material);
    }

    private void Awake(){
        cam = GetComponent<Camera>();
        cam.depthTextureMode = cam.depthTextureMode | DepthTextureMode.Depth;
        // print(PingPong(0f, 0.5f));
        // print(PingPong(0.5f, 0.5f));
        // print(PingPong(1f, 0.5f));
        // print(PingPong(0.8f, 0.5f));
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

        _DistanceToCamera = 0f;
    }

}
