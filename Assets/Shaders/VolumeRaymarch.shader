Shader "Unlit/VolumeRaymarch"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Scale("Scale", Range(0, 100)) = 1
        _Power("Power", Range(0.01, 10)) = 1
        _Threshold ("Threshold", Float) = 0.3
        _RimPowers("Rim Powers RGB", Vector) = (1,1,1)
        _ScrollOffset ("Scroll Offset", Float) = 5
        _ScrollScale ("Scroll Scale", Float) = 5
    }
    SubShader
    {
        LOD 100
        ZWrite On

        Tags
        {
            "RenderType"="Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_fwdbase

            #include "VolumeRaymarch.cginc"

            ENDCG
        }

        // Pass
        // {
        //     Tags { "LightMode" = "ForwardAdd" }
        //     ZWrite Off 
        //     Blend One One

        //     CGPROGRAM
        //     #pragma target 3.0
        //     #pragma vertex vert
        //     #pragma fragment frag
        //     #pragma multi_compile_fog

        //     #include "VolumeRaymarch.cginc"

        //     ENDCG
        // }

        Pass
        {
            Tags { "LightMode" = "ShadowCaster" }

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma multi_compile_shadowcaster

            #include "VolumeRaymarch.cginc"

            ENDCG
        }
    }
    Fallback Off
}
