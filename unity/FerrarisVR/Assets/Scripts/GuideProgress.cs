namespace Ferraris
{
    public enum GuideState { Available, Speaking, Guiding, Waiting, Paused, Completed }

    // The checkpoint includes the journey phase, not just the destination index.
    public sealed class GuideProgress
    {
        public const float TalkDistance=3f, WaitDistance=7f, CatchUpDistance=3.5f;
        public GuideState State { get; private set; }=GuideState.Available;
        public int Step { get; private set; }
        public bool Running=>State is GuideState.Speaking or GuideState.Guiding or GuideState.Waiting;
        GuideState checkpoint;

        public bool Begin(float distance)
        {
            if(State!=GuideState.Available||distance>TalkDistance)return false;
            State=GuideState.Speaking;return true;
        }
        public bool Continue(float distance,int count)
        {
            if(State!=GuideState.Speaking||distance>TalkDistance)return false;
            Step++;
            State=Step>=count?GuideState.Completed:GuideState.Guiding;
            return true;
        }
        public void Tick(float distance,bool arrived)
        {
            if(State is not (GuideState.Guiding or GuideState.Waiting))return;
            if(arrived){State=distance<=TalkDistance?GuideState.Speaking:GuideState.Waiting;return;}
            if(State==GuideState.Guiding&&distance>WaitDistance)State=GuideState.Waiting;
            else if(State==GuideState.Waiting&&distance<=CatchUpDistance)State=GuideState.Guiding;
        }
        public void Pause()
        {
            if(!Running)return;
            checkpoint=State;State=GuideState.Paused;
        }
        public bool Resume(float distance)
        {
            if(State!=GuideState.Paused||distance>TalkDistance)return false;
            State=checkpoint;return true;
        }
        public void Reset(){Step=0;State=GuideState.Available;checkpoint=GuideState.Available;}
    }
}
