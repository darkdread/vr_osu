Shader "Hidden/PostProcessingShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}

        [Header(Wave)]
        _DistanceFromCamera ("Distance from player", float) = 10
        _WaveTrail ("Length of the trail", Range(0,5)) = 1
        _ZaWarudoColor ("Color", Color) = (1,0,0,1)
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma enable_d3d11_debug_symbols
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float PingPong(float value, float length){
                if (value > length){
                    return length - fmod(value, length);
                }

                return value;
            }

            sampler2D _CameraDepthTexture;
            sampler2D _NoiseTex;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 ray : TEXCOORD1;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 scrPos : TEXCOORD1;
                float4 interpolatedRay : TEXCOORD2;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                o.scrPos = ComputeScreenPos(o.vertex);
                o.interpolatedRay = v.ray;
                return o;
            }

            static const float pi = 3.141592653589793238462;

            sampler2D _MainTex;
            float _DistanceFromCamera;
            float4 _ZaWarudoColor;
            float _PingPong;
            
            float _WaveTrail;

            fixed4 frag (v2f i) : SV_Target
            {
                // float randomness = tex2D(_NoiseTex, float2(sin(_Time[1]), sin(_Time[1]))).r;
                // i.uv = float2(i.uv.x * randomness, i.uv.y * randomness);

                float mySin = sin(_Time[1]);
                fixed4 col = tex2D(_MainTex, i.uv);
                
                float offset = PingPong(i.uv.x, 0.5);
                offset = sin(i.uv.x);
                
                float depth = tex2D(_CameraDepthTexture, i.scrPos.xy / i.scrPos.w).r;

                // if (_DistanceFromCamera <= 0){
                //     return col;
                // }

                if (depth >= _ProjectionParams.z){
                    return col;
                }

                // Middle + 10%.
                // _DistanceFromCamera += offset * _DistanceFromCamera;

                // Linear depth between camera and far clipping plane.
                float linearDepth = Linear01Depth(depth);
                float linearEyeDepth = LinearEyeDepth(depth);

                depth = linearDepth;

                // Depth as distance from camera in units.
                depth = depth * _ProjectionParams.z;

                // 1 if _DistanceFromCamera >= depth, otherwise 0.
                float waveFront = step(depth, _DistanceFromCamera);

                // 1 if depth > _DistanceFromCamera, 0 if depth < _DistanceFromCamera - _WaveTrail, otherwise between 0 and 1 of min, max.
                float waveTrail = smoothstep(_DistanceFromCamera - _WaveTrail, _DistanceFromCamera, depth);
                float wave = waveFront * waveTrail;
                
                col = lerp(col, _ZaWarudoColor, wave);

                return col;
            }
            ENDCG
        }
    }
}
