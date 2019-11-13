Shader "Hidden/PostProcessingShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}

        [Header(Wave)]
        _DistanceFromCamera ("Distance from player", float) = 10
        _WaveTrail ("Length of the trail", Range(0,5)) = 1
        _WaveColor ("Wave Color", Color) = (1,0,0,1)

        [Header(Implode)]
        _ImplodeTimeToReachMax ("Implode time to reach entire screen", float) = 1
        _ImplodeColor ("Implode Color", Color) = (1,0,0,1)
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
            float _StartTime;

            float _DistanceFromCamera;
            float _WaveTrail;
            float4 _WaveColor;

            float _ImplodeTimeToReachMax;
            float4 _ImplodeColor;

            float BezierCurve(float4 p0, float4 p1, float4 p2, float t){
                float pA = lerp(p0, p1, t);
                float pB = lerp(p1, p2, t);
                return lerp(pA, pB, t);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // float randomness = tex2D(_NoiseTex, float2(sin(_Time[1]), sin(_Time[1]))).r;
                // i.uv = float2(i.uv.x * randomness, i.uv.y * randomness);

                float timeSinceEffect = _Time.y - _StartTime;
                fixed4 col = tex2D(_MainTex, i.uv);
                float2 centerOfScreen = _ScreenParams.xy / 2;
                float2 pixelOfScreen = float2(i.scrPos.x * _ScreenParams.x, i.scrPos.y * _ScreenParams.y);
                float distanceBetweenCenter = distance(pixelOfScreen, centerOfScreen);

                // 0 to 1.
                float distanceBetweenCenterNormalized = distance(i.uv.xy, float2(0.5, 0.5)) * 2;

                // if (_DistanceFromCamera <= 0){
                //     return col;
                // }

                // Zoom effect.
                float2 zoom = i.uv;
                float zoomPercentage = 1 + timeSinceEffect * (1 - distanceBetweenCenterNormalized) * 5;
                float zoomer = 1 - (1/zoomPercentage);

                // x -0.5 to 0.5, y -0.5 to 0.5.
                // Signs turn negative if < half.
                zoom += float2((0.5 - i.uv.x) * zoomer, 0);
                zoom += float2(0, (0.5 - i.uv.y) * zoomer);

                // if (i.uv.x > 0.5){
                //     // zoom += float2(-i.uv.x * (1 - i.uv.x), 0);
                //     zoom += float2((0.5 - i.uv.x)/2, 0);
                // } else {
                //     // zoom += float2(i.uv.x * (1 - i.uv.x), 0);
                //     zoom += float2((0.5 - i.uv.x)/2, 0);
                // }

                // if (i.uv.y > 0.5){
                //     // zoom += float2(0, -i.uv.y * (1 - i.uv.y));
                //     zoom += float2(0, (0.5 - i.uv.y)/2);
                // } else {
                //     // zoom += float2(0, i.uv.y * (1 - i.uv.y));
                //     zoom += float2(0, (0.5 - i.uv.y)/2);
                // }

                col = tex2D(_MainTex, zoom);
                float depth = tex2D(_CameraDepthTexture, zoom).r;

                if (distanceBetweenCenterNormalized > 1){
                    return col - 0.1;
                }

                return col;

                // Wave effect.
                float offset = BezierCurve(0, 1, 0, i.uv.x);

                // x(0.5) = _DistanceFromCamera * 2.
                _DistanceFromCamera += offset * _DistanceFromCamera;

                // Linear depth between camera and far clipping plane.
                float linearDepth = Linear01Depth(depth);

                depth = linearDepth;

                if (depth >= _ProjectionParams.z){
                    return col;
                }

                // Depth as distance from camera in units.
                depth = depth * _ProjectionParams.z;

                // 1 if _DistanceFromCamera >= depth, otherwise 0.
                float waveFront = step(depth, _DistanceFromCamera);

                // 1 if depth > _DistanceFromCamera, 0 if depth < _DistanceFromCamera - _WaveTrail, otherwise between 0 and 1 of min, max.
                float waveTrail = smoothstep(_DistanceFromCamera - _WaveTrail, _DistanceFromCamera, depth);

                // 1 - waveFront so the colors start from front > back.
                float wave = 1 - waveFront;
                
                col = lerp(col, _WaveColor, wave);


                // Implode effect.

                // If effect is outside of radius.
                if (distanceBetweenCenter > (timeSinceEffect / _ImplodeTimeToReachMax) * _ScreenParams.x/2){
                    return col;
                }

                col = col * _ImplodeColor * (1 - smoothstep(0, _ScreenParams.x, distanceBetweenCenter));

                return col;
            }
            ENDCG
        }
    }
}
