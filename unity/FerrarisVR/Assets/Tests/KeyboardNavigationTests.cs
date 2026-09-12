using NUnit.Framework;
using UnityEngine;
namespace Ferraris.Tests
{
    public class KeyboardNavigationTests
    {
        [Test] public void TabOrderWrapsAndActivatesOnlyTheVisiblePanel()
        {
            var go=new GameObject("Keyboard navigation test");
            try
            {
                var ui=go.AddComponent<VisitorUI>();int menuClicks=0,panelClicks=0;
                ui.Button(new Rect(20,20,100,40),"Menu",()=>menuClicks++);
                ui.MoveFocus(1);Assert.That(ui.FocusedLabel,Is.EqualTo("Menu"));ui.ActivateFocused();
                var panel=ui.Box(new Rect(28,180,760,680));ui.PanelOpen=true;
                ui.Button(new Rect(40,220,100,40),"Eerste",()=>panelClicks++,panel.transform);
                ui.Button(new Rect(40,280,100,40),"Tweede",()=>panelClicks+=10,panel.transform);
                ui.MoveFocus(1);Assert.That(ui.FocusedLabel,Is.EqualTo("Eerste"));
                ui.MoveFocus(-1);Assert.That(ui.FocusedLabel,Is.EqualTo("Tweede"));ui.ActivateFocused();
                ui.MoveFocus(1);Assert.That(ui.FocusedLabel,Is.EqualTo("Eerste"));ui.ActivateFocused();
                Assert.That(menuClicks,Is.EqualTo(1));Assert.That(panelClicks,Is.EqualTo(11));
                ui.RemovePanel(panel);Assert.That(ui.ActivateFocused(),Is.False,"A removed panel must never keep clickable keyboard controls");
            }
            finally{Object.DestroyImmediate(go);}
        }
        [Test] public void RebuildingAPanelKeepsTheSameActionFocused()
        {
            var go=new GameObject("Rebuilt panel test");
            try
            {
                var ui=go.AddComponent<VisitorUI>();ui.PanelOpen=true;int value=0;var rect=new Rect(40,220,150,40);
                var panel=ui.Box(new Rect(28,180,760,680));ui.Button(rect,"Luider",()=>value++,panel.transform);
                ui.MoveFocus(1);ui.ActivateFocused();ui.RemovePanel(panel);
                panel=ui.Box(new Rect(28,180,760,680));ui.Button(rect,"Luider",()=>value++,panel.transform);
                Assert.That(ui.FocusedLabel,Is.EqualTo("Luider"));ui.ActivateFocused();Assert.That(value,Is.EqualTo(2));
                ui.ClearKeyboardFocus();Assert.That(ui.ActivateFocused(),Is.False);
            }
            finally{Object.DestroyImmediate(go);}
        }
    }
}
