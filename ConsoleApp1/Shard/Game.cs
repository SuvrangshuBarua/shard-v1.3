/*
*
*   The Game class is the entry way to the system, and it's set in the config file.  The overarching class
*       that drives your game (think of it as your main program) should extend from this.
*   @author Michael Heron
*   @version 1.0
*
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Shard
{
    internal abstract class Game
    {
        public AssetManagerBase assets;
        private List<IEnumerator> coroutines = new List<IEnumerator>();

        public AssetManagerBase getAssetManager()
        {
            if (assets == null)
            {
                assets = Bootstrap.getAssetManager();
            }

            return assets;
        }

        public abstract void initialize();

        public abstract void update();

        public IEnumerator StartCoroutine(IEnumerator routine)
        {
            coroutines.Add(routine);
            return routine;
        }

        public bool StopCoroutine(IEnumerator routine)
        {
            return coroutines.Remove(routine);
        }

        public void updateCoroutines()
        {
            // "&& coroutines.Contains(c)" ensures that coroutines can remove themselves while running
            coroutines = coroutines.Where((c) => c.MoveNext() == true && coroutines.Contains(c)).ToList();
        }

        public virtual bool isRunning()
        {
            return true;
        }

        // By default our games will run at the maximum speed possible, but
        // note that we have millisecond timing precision.  Any frame rate that
        // needs greater precision than that will start to go... weird.
        public virtual int getTargetFrameRate()
        {
            return Int32.MaxValue;
        }
    }
}