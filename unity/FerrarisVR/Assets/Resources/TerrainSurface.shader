Shader "Ferraris/TerrainSurface"
{
 Properties { _MainTex("Ferraris land use",2D)="white"{} _RoadMask("Mapped roads and wear",2D)="black"{} _Grass("Grass scan",2D)="white"{} _Soil("Soil scan",2D)="white"{} _BumpMap("Grass normal",2D)="bump"{} _SoilNormal("Soil normal",2D)="bump"{} _AreaSize("Area metres",Float)=1000 }
 SubShader {
 Tags { "RenderType"="Opaque" }
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows
 #pragma target 3.0
 #include "UnityStandardUtils.cginc"
 sampler2D _MainTex,_RoadMask,_Grass,_Soil,_BumpMap,_SoilNormal;float _AreaSize;
 struct Input {float3 worldPos;};
 void surf(Input i,inout SurfaceOutputStandard o){
 float2 p=i.worldPos.xz;float3 land=tex2D(_MainTex,saturate(p/_AreaSize+.5)).rgb;
 float soil=saturate((land.r-land.g)*18);float2 uv=p*.4;
 float3 detail=lerp(tex2D(_Grass,uv).rgb,tex2D(_Soil,uv).rgb,soil);
 float macro=.9+.1*sin(p.x*.031)*sin(p.y*.027);
 float rows=1-soil*.08*(.5+.5*sin(p.x*7+p.y*.8));
 float2 road=tex2D(_RoadMask,p/_AreaSize+.5).rg;
 road*=step(max(abs(p.x),abs(p.y)),_AreaSize*.5);
 float3 field=detail*lerp(float3(.85,.94,.77),float3(1,.9,.72),soil)*macro*rows;
 float3 track=tex2D(_Soil,uv).rgb*float3(.89,.83,.73)*lerp(1,.72,road.g)*macro;
 o.Albedo=lerp(field,track,road.r);
 o.Normal=normalize(lerp(UnpackScaleNormal(tex2D(_BumpMap,uv),.5),UnpackScaleNormal(tex2D(_SoilNormal,uv),.7),lerp(soil,1,road.r)));o.Smoothness=.06;o.Occlusion=1;o.Alpha=1;
 }
 ENDCG
 }
 FallBack "Diffuse"
}
