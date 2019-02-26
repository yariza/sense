Shader "Unlit/Sensel/Particle"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _Power("Power", Range(0.01, 10)) = 1
        _Scale("Scale", Range(0, 0.1)) = 0.01
        _VelocityScale("Velocity Scale", Range(0, 100)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend One One
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            StructuredBuffer<float4> _PositionBuffer;
            sampler2D _VelocityTex;

            float4 _Color;
            float _Power;
            float _Scale;
            float _VelocityScale;
            float4 _Bounds;

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 velocity : TEXCOORD1;
                float alpha : TEXCOORD2;
            };

            #define SQRT_3 1.73205080757

            static const float2 uvs[3] =
            {
                float2(SQRT_3, -1),
                float2(-SQRT_3, -1),
                float2(0, 2)
            };

            v2f vert (uint vid : SV_VertexID, uint iid : SV_InstanceID)
            {
                float4 p = _PositionBuffer[iid];
                float3 worldPos = p.xyz;
                float life = p.w + 0.5;


                float4 viewPos = mul(UNITY_MATRIX_V, float4(worldPos, 1.0));
                float2 uv = uvs[vid];
                viewPos.xy += uv * _Scale;

                float4 clipPos = mul(UNITY_MATRIX_P, viewPos);

                float2 posUV = worldPos.xy;
                posUV /= _Bounds.xy;
                posUV += (float2)0.5;
                // float2 v = _VelocityTex.SampleLevel(sampler_VelocityTex, posUV, 0);
                float2 v = tex2Dlod(_VelocityTex, float4(posUV, 0, 0)).xy;
                v *= _VelocityScale;
                v += (float2)0.5;
                // v = posUV;

                v2f o;
                o.vertex = clipPos;
                o.uv = uv;
                o.velocity = v;
                o.alpha = min(1.0, 5.0 - abs(5.0 - life * 10));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float d = 1 - dot(i.uv, i.uv);
                clip(d);
                d = pow(d, _Power);

                float2 v = i.velocity;
                fixed4 col = _Color;
                float alpha = saturate(i.alpha);
                alpha *= d;
                col *= alpha;
                return col;
            }
            ENDCG
        }
    }
}
