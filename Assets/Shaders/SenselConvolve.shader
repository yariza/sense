Shader "Hidden/Sensel/Convolve"
{
    Properties
    {
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    sampler2D _MainTex;
    float4 _MainTex_TexelSize;
    sampler2D _RawInputTex;

    float _Interpolant;

    float4 frag_convolve(v2f_img input) : SV_Target
    {
        float2 uv = input.uv;
        float raw = tex2D(_RawInputTex, uv).r;
        return float4(raw, 0, 0, _Interpolant);
    }

    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            Color (0, 0, 0, 0)
        }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_convolve
            ENDCG
        }
        Pass
        {
            Blend One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_convolve
            ENDCG
        }
    }
}
