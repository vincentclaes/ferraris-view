Shader "Ferraris/ChurchPBR"
{
    Properties
    {
        _Color("Tint", Color) = (1,1,1,1)
        _MainTex("Base color", 2D) = "white" {}
        _BumpMap("Normal", 2D) = "bump" {}
        _BumpScale("Normal strength", Range(0,2)) = 0.65
        _RoughnessTex("Roughness", 2D) = "white" {}
        _Roughness("Roughness multiplier", Range(0,1)) = 0.9
        _Metallic("Metallic", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        #pragma multi_compile_instancing
        #include "UnityStandardUtils.cginc"
        sampler2D _MainTex, _BumpMap, _RoughnessTex;
        fixed4 _Color;
        half _BumpScale, _Roughness, _Metallic;
        struct Input { float2 uv_MainTex; };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb * _Color.rgb;
            o.Normal = UnpackScaleNormal(tex2D(_BumpMap, IN.uv_MainTex), _BumpScale);
            o.Smoothness = 1 - saturate(tex2D(_RoughnessTex, IN.uv_MainTex).r * _Roughness);
            o.Metallic = _Metallic;
            o.Occlusion = 1;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Standard"
}
