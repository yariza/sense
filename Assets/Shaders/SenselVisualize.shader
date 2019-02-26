Shader "Unlit/SenselVisualize"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
        _Scale("Scale", Range(0, 1)) = 1
        _Power("Power", Range(0.01, 10)) = 1
        _Extrude("Extrude Amount", Range(0, 1)) = 0
        _Threshold("Threshold", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _Scale;
            float _Power;
            float _Threshold;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
#if UNITY_UV_STARTS_AT_TOP
                o.uv.y = 1 - o.uv.y;
#endif
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float a = tex2D(_MainTex, float2(i.uv.x, 1.0 - i.uv.y)).r;
                a = pow(a, _Power) * _Scale;
                a = a < _Threshold ? 0 : a;

                fixed4 col = fixed4(a, a, a, 1.0);

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
