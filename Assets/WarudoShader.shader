// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Warudo/Warudo"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_DetailTex("Texture", 2D) = "white" {}
		_GradientTex("Texture",2D) = "white"{}
		_ScanDistance("Scan Distance", float) = 0
		_MinScanDistance("Min Scan Distance", float) = 10
		_LeadSharp("Leading Edge Sharpness", float) = 10
		_LeadColor("Leading Edge Color", Color) = (1, 1, 1, 0)
		_MidColor("Mid Color", Color) = (1, 1, 1, 0)
		_TrailColor("Trail Color", Color) = (1, 1, 1, 0)
		_HBarColor("Horizontal Bar Color", Color) = (0.5, 0.5, 0.5, 0)
		_Color0("FirstColor", Color) = (1,1,1,0)
		_Color1("SecondColor", Color) = (1,1,1,0)
		_Color2("ThirdColor", Color) = (1,1,1,0)
		_Color3("FourthColor", Color) = (1,1,1,0)
		_MaxDistance("Max Distance", float) = 0
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct VertIn
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 ray : TEXCOORD1;
			};

			struct VertOut
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float2 uv_depth : TEXCOORD1;
				float4 interpolatedRay : TEXCOORD2;
			};

			float4 _MainTex_TexelSize;
			float4 _CameraWS;

			VertOut vert(VertIn v)
			{
				VertOut o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv.xy;
				o.uv_depth = v.uv.xy;

				#if UNITY_UV_STARTS_AT_TOP
				if (_MainTex_TexelSize.y < 0)
					o.uv.y = 1 - o.uv.y;
				#endif

				o.interpolatedRay = v.ray;

				return o;
			}

			sampler2D _MainTex;
			sampler2D _DetailTex;
			sampler2D_float _CameraDepthTexture;
			float4 _WorldSpaceScannerPos;
			float _ScanDistance;
			float _MinScanDistance;
			float _LeadSharp;
			float4 _LeadColor;
			float4 _MidColor;
			float4 _TrailColor;
			float4 _HBarColor;
			float4 _Color0;
			float4 _Color1;
			float4 _Color2;
			float4 _Color3;
			float _MaxDistance;

			float4 horizBars(float2 p)
			{
				return 1 - saturate(round(abs(frac(p.y * 100) * 2)));
			}

			float4 horizTex(float2 p)
			{
				return tex2D(_DetailTex, float2(p.x * 30, p.y * 40));
			}

			half4 frag (VertOut i) : SV_Target
			{
				half4 col = tex2D(_MainTex, i.uv);

				float rawDepth = DecodeFloatRG(tex2D(_CameraDepthTexture, i.uv_depth));
				float linearDepth = Linear01Depth(rawDepth);

				float4 wsDir = linearDepth * i.interpolatedRay;
				float3 wsPos = _WorldSpaceCameraPos + wsDir;
				float dist = distance(wsPos, _WorldSpaceScannerPos);

				float colorEffectRatio = _ScanDistance / _MinScanDistance;
				if(colorEffectRatio > 1){
					colorEffectRatio = 1;
				}

				half4 scannerCol = half4(0,0,0,0);

				if(_ScanDistance - _MinScanDistance > 0){
					if(_ScanDistance - _MinScanDistance < _MaxDistance / 4){
						scannerCol = lerp(_Color0, _Color1, _ScanDistance / (_MaxDistance / 4));
					} else if (_ScanDistance - _MinScanDistance < _MaxDistance / 2){
						scannerCol = lerp(_Color1, _Color2, (_ScanDistance-(_MaxDistance / 4)) / (_MaxDistance / 4));
					} else if (_ScanDistance - _MinScanDistance < _MaxDistance * 3 / 4){
						scannerCol = lerp(_Color2, _Color3, (_ScanDistance-(_MaxDistance / 2)) / (_MaxDistance / 4));
					} else {
						scannerCol = _Color3;
					}
				} else {
					scannerCol = _Color0;
				}

				if(dist > _ScanDistance){
					return col + (scannerCol / 2) * colorEffectRatio;
				}else if (dist > _ScanDistance - _LeadSharp) {
					return col + (scannerCol / 2) * colorEffectRatio * lerp(0,1,(dist - (_ScanDistance - _LeadSharp)) / _LeadSharp);
				} else {
					return col;
				}
			}
			ENDCG
		}
	}
}
