using System;
using System.IO;
using System.Text.Json;
using Ferraris;

class Program
{
    static void Main(string[] args)
    {
        var area=JsonSerializer.Deserialize<AreaData>(File.ReadAllText(args[0]),new JsonSerializerOptions{IncludeFields=true});
        double worst=0;
        for(int z=0;z<=32;z++)for(int x=0;x<=32;x++)
        {
            float px=(x/32f-.5f)*area.size,pz=(z/32f-.5f)*area.size;
            var geo=area.UnityToGeoCoordinate(px,pz);var p=area.GeoCoordinateToUnity(geo.lat,geo.lon);
            double error=Math.Sqrt(Math.Pow(p.x-px,2)+Math.Pow(p.z-pz,2));worst=Math.Max(worst,error);
            if(error>.01)throw new Exception($"Roundtrip failed at {px},{pz}: {error}m");
            if(!float.IsFinite(area.Height(px,pz)))throw new Exception("Nonfinite elevation");
        }
        var centre=area.GeoCoordinateToUnity(50.89795,4.64385);
        if(Math.Abs(centre.x)>.01||Math.Abs(centre.z)>.01)throw new Exception("Origin mismatch");
        var sw=area.MapUVToUnity(0,0);var ne=area.MapUVToUnity(1,1);
        if(sw.x!=-500||sw.z!=-500||ne.x!=500||ne.z!=500)throw new Exception("Map axis reversal");
        bool rejected=false;try{area.GeoCoordinateToUnity(51,5);}catch(ArgumentOutOfRangeException){rejected=true;}
        if(!rejected)throw new Exception("Out-of-area coordinate accepted");
        Console.WriteLine($"PASS actual Unity C# coordinate code: 1089 grid roundtrips including edges, origin, UV corners, bounds and elevation. Worst error: {worst:F6}m");
    }
}
