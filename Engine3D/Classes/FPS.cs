using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public class FPS
    {
        public int fps;

        public int minFps = int.MaxValue;
        public int maxFps = 0;
        public double totalTime;

        private const int SAMPLE_SIZE = 30;
        private Queue<double> sampleTimes = new Queue<double>(SAMPLE_SIZE);

        private Stopwatch maxminStopwatch;

        //private bool limitFps = false;
        private const double TargetDeltaTime = 1.0 / 60.0; // for 60 FPS

        public const int fpsLength = 5;

        public FPS()
        {
            maxminStopwatch = new Stopwatch();
            maxminStopwatch.Start();
        }

        public void Update(float delta)
        {
            if (sampleTimes.Count >= SAMPLE_SIZE)
            {
                totalTime -= sampleTimes.Dequeue();
            }

            sampleTimes.Enqueue(delta);
            totalTime += delta;

            double averageDeltaTime = totalTime / sampleTimes.Count;
            double fps = 1.0 / averageDeltaTime;
            this.fps = (int)fps;

            if (!maxminStopwatch.IsRunning)
            {
                if (fps > maxFps)
                    maxFps = (int)fps;
                if (fps < minFps)
                    minFps = (int)fps;
            }
            else
            {
                if (maxminStopwatch.ElapsedMilliseconds > 3000)
                    maxminStopwatch.Stop();
            }
        }

        public string GetFpsString()
        {
            string fpsStr = fps.ToString();
            string maxFpsStr = maxFps.ToString();
            string minFpsStr = minFps.ToString();
            if (fpsStr.Length < fpsLength)
            {
                for (int i = 0; i < (fpsLength + 1 - fpsStr.Length); i++)
                    fpsStr = " " + fpsStr;
            }
            if (maxFpsStr.Length < fpsLength)
            {
                for (int i = 0; i < (fpsLength + 1 - maxFpsStr.Length); i++)
                    maxFpsStr = " " + maxFpsStr;
            }
            if (minFpsStr.Length < fpsLength)
            {
                for (int i = 0; i < (fpsLength + 1 - minFpsStr.Length); i++)
                    minFpsStr = " " + minFpsStr;
            }

            string fpsFullStr = "FPS: " + fpsStr + "    |    MaxFPS: " + maxFpsStr + "    |    MinFPS: " + minFpsStr;

            return fpsFullStr;
        }
    }
}
