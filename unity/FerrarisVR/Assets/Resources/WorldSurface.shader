Shader "Ferraris/WorldSurface"
{
 Properties { _MainTex("Scanned surface",2D)="white"{} _BumpMap("Normal",2D)="bump"{} _RoughnessTex("Roughness",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) _Scale("Metres per repeat",Float)=0.5 _BumpScale("Relief",Float)=0.7 }
 SubShader {
 Tags { "RenderType"="Opaque" }
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows
 #pragma target 3.0
 #pragma multi_compile_instancing
 #include "UnityStandardUtils.cginc"
 sampler2D _MainTex,_BumpMap,_RoughnessTex;fixed4 _Color;float _Scale,_BumpScale;
 struct Input {float2 uv_MainTex;float4 color:COLOR;};
 void surf(Input i,inout SurfaceOutputStandard o){float2 uv=i.uv_MainTex*_Scale;o.Albedo=tex2D(_MainTex,uv).rgb*i.color.rgb*_Color.rgb;o.Normal=UnpackScaleNormal(tex2D(_BumpMap,uv),_BumpScale);o.Smoothness=(1-tex2D(_RoughnessTex,uv).r)*.4;o.Occlusion=1;o.Alpha=1;}
 ENDCG
 }
 FallBack "Diffuse"
}
