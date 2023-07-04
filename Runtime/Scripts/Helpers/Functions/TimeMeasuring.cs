using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace ContextualAmbientOcclusion.Runtime
{
    public class TimeMeasuring
    {
        const bool PRINT = true;

        static Dictionary<string, Stopwatch> watches = new Dictionary<string, Stopwatch>();


        public static void Start(string name)
        {
            watches[name] = Stopwatch.StartNew();
        }

        public static void Pause(string name)
        {
            watches[name].Stop();
        }

        public static void Continue(string name)
        {
            watches[name].Start();
        }

        public static void End(string name)
        {
            double elapsedSecsDouble = ElapsedTime(name);

            if (PRINT)
            {
                UnityEngine.Debug.Log("Elapsed Time for \"" + name + "\": " + elapsedSecsDouble);
            }

            watches.Remove(name);
        }

        public static double ElapsedTime(string name)
        {
            if (watches.ContainsKey(name))
            {
                return watches[name].ElapsedTicks / (double)Stopwatch.Frequency;
            }

            return 0;
        }
    }
}