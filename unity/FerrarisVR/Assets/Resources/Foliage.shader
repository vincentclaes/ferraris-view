Shader "Ferraris/Foliage"
{
 Properties {_MainTex("Leaves",2D)="white"{} _AlphaTex("Silhouette",2D)="white"{} _Cutoff("Cutout",Range(0,1))=.45 _Wind("Wind",Float)=.1 }
 SubShader {
 Tags {"Queue"="AlphaTest" "RenderType"="TransparentCutout"} Cull Off
 CGPROGRAM
 #pragma surface surf Lambert vertex:vert alphatest:_Cutoff addshadow
 #pragma target 3.0
 #pragma multi_compile_instancing
 sampler2D _MainTex,_AlphaTex;float _Wind;
 struct Input {float2 uv_MainTex;float4 color:COLOR;};
 void vert(inout appdata_full v){float3 w=mul(unity_ObjectToWorld,v.vertex).xyz;v.vertex.x+=sin(_Time.y*1.1+w.x*.7+w.z*.5)*_Wind*v.texcoord.y;}
 void surf(Input i,inout SurfaceOutput o){o.Albedo=tex2D(_MainTex,i.uv_MainTex).rgb*i.color.rgb;o.Emission=o.Albedo*.10;o.Alpha=tex2D(_AlphaTex,i.uv_MainTex).r;}
 ENDCG
 }
 FallBack "Transparent/Cutout/Diffuse"
}
