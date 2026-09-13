Shader "Ferraris/Foliage"
{
 Properties {_MainTex("Leaves",2D)="white"{} _AlphaTex("Silhouette",2D)="white"{} _Cutoff("Cutout",Range(0,1))=.45 _Wind("Wind",Float)=.1 _HeightWind("Rooted stem wind",Range(0,1))=0 _EarDetail("Grain rows",Range(0,1))=0 _FadeRange("Ground cover fade",Vector)=(0,0,0,0) _PlantEye("Viewer",Vector)=(0,0,0,0) }
 SubShader {
 Tags {"Queue"="AlphaTest" "RenderType"="TransparentCutout"} Cull Off
 CGPROGRAM
 #pragma surface surf Lambert vertex:vert alphatest:_Cutoff addshadow
 #pragma target 3.0
 #pragma multi_compile_instancing
 sampler2D _MainTex,_AlphaTex;float _Wind,_HeightWind,_EarDetail;
 float4 _FadeRange,_PlantEye;
 struct Input {float2 uv_MainTex;float4 color:COLOR;};
 void vert(inout appdata_full v){float fade=1;
  if(_FadeRange.y>0){float3 root=mul(unity_ObjectToWorld,float4(0,0,0,1)).xyz;fade=saturate((_FadeRange.y-distance(root.xz,_PlantEye.xz))/max(.01,_FadeRange.y-_FadeRange.x));v.vertex.xyz*=fade;}
  float3 w=mul(unity_ObjectToWorld,v.vertex).xyz;float bend=lerp(v.texcoord.y,max(0,v.vertex.y)*max(0,v.vertex.y),_HeightWind);v.vertex.x+=sin(_Time.y*1.1+w.x*.7+w.z*.5)*_Wind*bend*fade;}
 void surf(Input i,inout SurfaceOutput o){o.Albedo=tex2D(_MainTex,i.uv_MainTex).rgb*i.color.rgb;
  if(_EarDetail>0){float rows=i.uv_MainTex.y*9+sin(i.uv_MainTex.x*25.1327)*.22;
   float grain=1-smoothstep(.12,.48,abs(frac(rows)-.5));
   float detail=_EarDetail*i.color.a*saturate(1-fwidth(rows));o.Albedo*=lerp(1,.55+.45*grain,detail);}
  o.Emission=o.Albedo*.10;o.Alpha=tex2D(_AlphaTex,i.uv_MainTex).r;}
 ENDCG
 }
 FallBack "Transparent/Cutout/Diffuse"
}
