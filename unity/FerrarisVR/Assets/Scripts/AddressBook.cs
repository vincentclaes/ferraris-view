using System;
using UnityEngine;
namespace Ferraris
{
    [Serializable] public class CurrentAddress
    {
        public string id,street,number,locality;
        public float x,z;
        public string Label => $"{street} {(string.IsNullOrWhiteSpace(number)?"(huisnummer onbekend)":number)}, {locality}";
    }
    [Serializable] public class AddressBook
    {
        public string date,source;
        public CurrentAddress[] addresses;
        public CurrentAddress Nearest(float x,float z,float areaSize,out float distance)
        {
            distance=float.PositiveInfinity;
            if(!float.IsFinite(x)||!float.IsFinite(z)||Mathf.Abs(x)>areaSize/2||Mathf.Abs(z)>areaSize/2||addresses==null)return null;
            CurrentAddress nearest=null;
            // ponytail: 966 cached points at 4 Hz; use a spatial index for regional coverage.
            foreach(var a in addresses)
            {
                float d=(a.x-x)*(a.x-x)+(a.z-z)*(a.z-z);
                if(d<distance){distance=d;nearest=a;}
            }
            distance=Mathf.Sqrt(distance);return nearest;
        }
    }
    public class AddressDisplay : MonoBehaviour
    {
        public AddressBook Book { get; private set; }
        public CurrentAddress Current { get; private set; }
        public string Caption { get; private set; }="Adresgegevens laden…";
        FerrarisApp app;float nextUpdate;
        void Start()
        {
            app=GetComponent<FerrarisApp>();gameObject.AddComponent<VisitorUI>();
            var file=Resources.Load<TextAsset>("Discovery/addresses");
            if(file!=null)try{Book=JsonUtility.FromJson<AddressBook>(file.text);}catch(Exception e){Debug.LogWarning(e.Message);}
        }
        void Update()
        {
            if(app==null||!app.Ready)return;
            if(!app.InWorld||Time.unscaledTime<nextUpdate)return;
            nextUpdate=Time.unscaledTime+.25f;
            Current=Book?.Nearest(app.Player.position.x,app.Player.position.z,app.Area.size,out _);
            Caption=Current==null?"Geen huidig adres beschikbaar voor deze plek.":$"Dichtstbijzijnde huidige adres\n{Current.Label}\n{Vector2.Distance(new Vector2(Current.x,Current.z),new Vector2(app.Player.position.x,app.Player.position.z)):F0} m hemelsbreed • momentopname {Book.date}";
        }
    }
}
