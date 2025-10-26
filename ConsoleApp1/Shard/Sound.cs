/*
*
*   This class intentionally left blank.
*   @author Michael Heron
*   @version 1.0
*
*/

namespace Shard
{
    public abstract class Sound
    {
        public virtual void Initialize()
        { }

        public abstract void playSound(string file);

        public virtual void Stop()
        { }

        public virtual void SetVolume(float volume)
        { }

        public virtual void Cleanup()
        { }
    }
}