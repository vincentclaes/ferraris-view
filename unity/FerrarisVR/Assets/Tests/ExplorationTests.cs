using NUnit.Framework;
using UnityEngine;
namespace Ferraris.Tests
{
    public class ExplorationTests
    {
        [Test] public void MapLocationUsesNorthUpAndCurrentCoordinates()
        {
            Assert.That(ExplorationUI.MapUV(Vector3.zero,1000),Is.EqualTo(new Vector2(.5f,.5f)));
            Assert.That(ExplorationUI.MapUV(new Vector3(250,80,-250),1000),Is.EqualTo(new Vector2(.75f,.25f)));
            Assert.That(ExplorationUI.MapUV(new Vector3(-500,0,500),1000),Is.EqualTo(new Vector2(0,1)));
        }
        [Test] public void HeadingNamesFollowCameraDirectionInDutch()
        {
            Assert.That(ExplorationUI.Compass(Vector3.forward),Is.EqualTo("noorden"));
            Assert.That(ExplorationUI.Compass(Vector3.right),Is.EqualTo("oosten"));
            Assert.That(ExplorationUI.Compass(Vector3.back),Is.EqualTo("zuiden"));
            Assert.That(ExplorationUI.Compass(Vector3.left),Is.EqualTo("westen"));
            Assert.That(ExplorationUI.Compass(new Vector3(-1,0,1)),Is.EqualTo("noordwesten"));
        }
        [Test] public void ModalExcludesButtonsGroupedInTheNavigationBar()
        {
            var go=new GameObject("Modal navigation test");
            try
            {
                var ui=go.AddComponent<VisitorUI>();int clicks=0;
                var toolbar=ui.Box(new Rect(24,24,650,126));
                ui.Button(new Rect(40,88,186,46),"Kaart",()=>clicks++,toolbar.transform);
                var panel=ui.Box(new Rect(28,180,760,680));ui.PanelRoot=panel.transform;ui.PanelOpen=true;
                ui.Button(new Rect(48,200,100,40),"Sluiten",()=>clicks+=10,panel.transform);
                ui.MoveFocus(1);Assert.That(ui.FocusedLabel,Is.EqualTo("Sluiten"));
                ui.MoveFocus(1);Assert.That(ui.FocusedLabel,Is.EqualTo("Sluiten"));
                ui.ActivateFocused();Assert.That(clicks,Is.EqualTo(10));
            }
            finally{Object.DestroyImmediate(go);}
        }
    }
}
