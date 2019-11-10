Shader "Hidden/PostProcessingShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
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

            sampler2D _CameraDepthTexture;
            sampler2D _NoiseTex;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 scrPos : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                o.scrPos = ComputeScreenPos(o.vertex);
                return o;
            }

            sampler2D _MainTex;

            fixed4 frag (v2f i) : SV_Target
            {
                float randomness = tex2D(_NoiseTex, float2(sin(_Time[1]), sin(_Time[1]))).r;
                i.uv = float2(i.uv.x * randomness, i.uv.y * randomness);

                fixed4 col = tex2D(_MainTex, i.uv);
                float depth = LinearEyeDepth(tex2D(_CameraDepthTexture, i.scrPos.xy / i.scrPos.w));

                // depth *= sin(_Time.y * 50);
                // just invert the colors
                col.rgb = col.rgb * (1 - (depth / 500));
                return col;
            }
            ENDCG
        }
    }
}
