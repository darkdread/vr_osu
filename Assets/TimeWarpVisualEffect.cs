// using UnityEngine;
// using System.Collections;
// using MorePPEffects;

// [ExecuteInEditMode]
// public class TimeWarpVisualEffect : MonoBehaviour
// {
// 	public Transform ScannerOrigin;
// 	public Material EffectMaterial;
// 	public float ScanDistance;
// 	public float effectTime;
// 	public float maxScanDistance;
// 	private float scanStartTime;

// 	public Wiggle wiggle;

// 	private Camera _camera;

// 	// Demo Code
// 	bool _scanning;

// 	void Update()
// 	{
// 		if (_scanning) {

// 			//ScanDistance = -1 * (Mathf.Pow(Mathf.Sqrt(maxScanDistance) * (Time.unscaledTime - scanStartTime) - Mathf.Sqrt(maxScanDistance),2f) - maxScanDistance);
// 			//ScanDistance = (Time.unscaledTime - scanStartTime < effectTime / 2) ? ((Time.unscaledTime - scanStartTime) / effectTime) * maxScanDistance * 2f : maxScanDistance - ((Time.unscaledTime - scanStartTime - (effectTime / 2)) / effectTime) * maxScanDistance * 2f;
// 			ScanDistance = maxScanDistance * Mathf.Pow(Mathf.Sin((Time.unscaledTime - scanStartTime) * Mathf.PI / effectTime), 4f);
// 			if (wiggle) {
// 				wiggle.amplitudeX = Mathf.Sin (Mathf.PI * (Time.unscaledTime - scanStartTime) / effectTime) * 6;
// 				wiggle.amplitudeY = Mathf.Sin (Mathf.PI * (Time.unscaledTime - scanStartTime) / effectTime) * 6;
// 				wiggle.distortionX = Mathf.Sin (Mathf.PI * (Time.unscaledTime - scanStartTime) / effectTime) * 2;
// 				wiggle.distortionY = Mathf.Sin (Mathf.PI * (Time.unscaledTime - scanStartTime) / effectTime) * 2;
// 			}
// 			if (ScanDistance < 0) {
// 				ScanDistance = 0;
// 			} 
// 			if (Time.unscaledTime - scanStartTime > effectTime) {
// 				_scanning = false;
// 			}
// 		} else if (wiggle) {
// 			wiggle.amplitudeX = 0f;
// 			wiggle.amplitudeY = 0f;
// 			wiggle.distortionX = 0f;
// 			wiggle.distortionY = 0f;
// 		}
// 	}
// 	// End Demo Code

// 	public void StartEffect(){
// 		_scanning = true;
// 		scanStartTime = Time.unscaledTime;
// 		ScanDistance = 0;
// 	}

// 	void OnEnable()
// 	{
// 		_camera = GetComponent<Camera>();
// 		_camera.depthTextureMode = DepthTextureMode.Depth;
// 	}

// 	[ImageEffectOpaque]
// 	void OnRenderImage(RenderTexture src, RenderTexture dst)
// 	{
// 		EffectMaterial.SetVector("_WorldSpaceScannerPos", ScannerOrigin.position);
// 		EffectMaterial.SetFloat("_ScanDistance", ScanDistance);
// 		RaycastCornerBlit(src, dst, EffectMaterial);
// 	}

// 	void RaycastCornerBlit(RenderTexture source, RenderTexture dest, Material mat)
// 	{
// 		// Compute Frustum Corners
// 		float camFar = _camera.farClipPlane;
// 		float camFov = _camera.fieldOfView;
// 		float camAspect = _camera.aspect;

// 		float fovWHalf = camFov * 0.5f;
// 		Vector3 toRight = new Vector3(), toTop = new Vector3();

// 		if (_camera.orthographic) {
// 			toRight = _camera.transform.right * camAspect;
// 			toRight = _camera.transform.up;
// 		} else {
// 			toRight = _camera.transform.right * Mathf.Tan (fovWHalf * Mathf.Deg2Rad) * camAspect;
// 			toTop = _camera.transform.up * Mathf.Tan (fovWHalf * Mathf.Deg2Rad);
// 		}

// 		Vector3 topLeft = (_camera.transform.forward - toRight + toTop);
// 		float camScale = topLeft.magnitude * camFar;

// 		topLeft.Normalize ();
// 		topLeft *= camScale;

// 		Vector3 topRight = (_camera.transform.forward + toRight + toTop);
// 		topRight.Normalize ();
// 		topRight *= camScale;

// 		Vector3 bottomRight = (_camera.transform.forward + toRight - toTop);
// 		bottomRight.Normalize ();
// 		bottomRight *= camScale;

// 		Vector3 bottomLeft = (_camera.transform.forward - toRight - toTop);
// 		bottomLeft.Normalize ();
// 		bottomLeft *= camScale;

// 		// Custom Blit, encoding Frustum Corners as additional Texture Coordinates
// 		RenderTexture.active = dest;

// 		mat.SetTexture ("_MainTex", source);

// 		GL.PushMatrix ();
// 		GL.LoadOrtho ();

// 		mat.SetPass (0);

// 		GL.Begin (GL.QUADS);

// 		GL.MultiTexCoord2 (0, 0.0f, 0.0f);
// 		GL.MultiTexCoord (1, bottomLeft);
// 		GL.Vertex3 (0.0f, 0.0f, 0.0f);

// 		GL.MultiTexCoord2 (0, 1.0f, 0.0f);
// 		GL.MultiTexCoord (1, bottomRight);
// 		GL.Vertex3 (1.0f, 0.0f, 0.0f);

// 		GL.MultiTexCoord2 (0, 1.0f, 1.0f);
// 		GL.MultiTexCoord (1, topRight);
// 		GL.Vertex3 (1.0f, 1.0f, 0.0f);

// 		GL.MultiTexCoord2 (0, 0.0f, 1.0f);
// 		GL.MultiTexCoord (1, topLeft);
// 		GL.Vertex3 (0.0f, 1.0f, 0.0f);

// 		GL.End ();
// 		GL.PopMatrix ();
// 	}
// }
