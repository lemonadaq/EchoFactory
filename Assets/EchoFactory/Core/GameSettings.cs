namespace EchoFactory.Core
{
    // Player settings that do not belong to a run: master volume in whole percents and fullscreen.
    public sealed class GameSettings
    {
        public const int DefaultVolumePercent=80;
        public const int VolumeStepPercent=10;

        public int VolumePercent=DefaultVolumePercent;
        public bool Fullscreen=true;

        public static int ClampVolume(int percent){return percent<0?0:percent>100?100:percent;}

        // Moves the volume by whole steps (negative = quieter), staying on multiples of the step within 0-100.
        public void ChangeVolume(int steps)
        {
            int snapped=(ClampVolume(VolumePercent)+VolumeStepPercent/2)/VolumeStepPercent*VolumeStepPercent;
            VolumePercent=ClampVolume(snapped+steps*VolumeStepPercent);
        }

        public float VolumeGain{get{return ClampVolume(VolumePercent)/100f;}}
    }
}
