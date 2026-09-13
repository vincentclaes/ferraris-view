using NUnit.Framework;
namespace Ferraris.Tests
{
    public class GuideTests
    {
        [Test] public void InvitationRequiresApproachAndExplicitAcceptance()
        {
            var guide=new GuideProgress();guide.Tick(1,true);
            Assert.That(guide.State,Is.EqualTo(GuideState.Available));
            Assert.That(guide.Begin(8),Is.False);Assert.That(guide.Begin(2),Is.True);
            Assert.That(guide.Continue(8,5),Is.False);
        }
        [Test] public void WaitingUsesSeparateStopAndCatchUpDistances()
        {
            var guide=new GuideProgress();guide.Begin(2);guide.Continue(2,5);
            guide.Tick(8,false);Assert.That(guide.State,Is.EqualTo(GuideState.Waiting));
            guide.Tick(6,false);Assert.That(guide.State,Is.EqualTo(GuideState.Waiting));
            guide.Tick(3,false);Assert.That(guide.State,Is.EqualTo(GuideState.Guiding));
            guide.Tick(8,true);Assert.That(guide.State,Is.EqualTo(GuideState.Waiting));
            guide.Tick(2,true);Assert.That(guide.State,Is.EqualTo(GuideState.Speaking));
            Assert.That(guide.Step,Is.EqualTo(1));
        }
        [Test] public void WalkingAheadDoesNotRequireReturningToTheGuide()
        {
            var guide=new GuideProgress();guide.Begin(2);guide.Continue(2,5);
            guide.Tick(12,false,true);Assert.That(guide.State,Is.EqualTo(GuideState.Guiding));
            guide.Tick(12,true,true);Assert.That(guide.State,Is.EqualTo(GuideState.Waiting));
            guide.Tick(2,true,true);Assert.That(guide.State,Is.EqualTo(GuideState.Speaking));
        }
        [TestCase(GuideState.Speaking)] [TestCase(GuideState.Guiding)] [TestCase(GuideState.Waiting)]
        public void LeavingStopsLateEventsAndRestoresTheSamePhase(GuideState phase)
        {
            var guide=new GuideProgress();guide.Begin(2);
            if(phase!=GuideState.Speaking)guide.Continue(2,5);
            if(phase==GuideState.Waiting)guide.Tick(8,false);
            int step=guide.Step;guide.Pause();guide.Pause();guide.Tick(1,true);
            Assert.That(guide.Continue(1,5),Is.False);Assert.That(guide.Running,Is.False);
            Assert.That(guide.Step,Is.EqualTo(step));Assert.That(guide.Resume(20),Is.False);
            Assert.That(guide.Resume(2),Is.True);Assert.That(guide.State,Is.EqualTo(phase));
        }
        [Test] public void CompleteStoryRequiresEachArrivalAndCanReset()
        {
            var guide=new GuideProgress();guide.Begin(2);
            for(int i=0;i<5;i++)
            {
                Assert.That(guide.Continue(2,5),Is.True);
                if(i<4){Assert.That(guide.Continue(2,5),Is.False);guide.Tick(2,true);}
            }
            Assert.That(guide.State,Is.EqualTo(GuideState.Completed));Assert.That(guide.Running,Is.False);
            guide.Pause();Assert.That(guide.Resume(2),Is.False);
            guide.Reset();Assert.That(guide.Step,Is.Zero);Assert.That(guide.State,Is.EqualTo(GuideState.Available));
        }
    }
}
