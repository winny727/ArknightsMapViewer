using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ArknightsMapViewer
{
    public class TimelineSimulator
    {
        public List<float> waveMaxTimes = new List<float>();
        public ReadOnlyCollection<float> WaveMaxTimes => waveMaxTimes.AsReadOnly();

        public float Time { get; private set; } = 0f;
        public int WaveIndex { get; private set; } = 0;

        public float MaxTime => WaveIndex >= 0 && WaveIndex < waveMaxTimes.Count ? waveMaxTimes[WaveIndex] : 0;

        public TimelineSimulator(LevelData levelData)
        {
            var ran = new Random((int)DateTime.Now.Ticks);
            for (int i = 0; i < ran.Next(1,5); i++)
            {
                waveMaxTimes.Add(ran.Next(100, 200));
            }
        }

        public void SetWaveIndex(int waveIndex)
        {
            if (waveIndex >= 0 && waveIndex < waveMaxTimes.Count)
            {
                WaveIndex = waveIndex;
                UpdateTimeline(0f);
            }
        }

        public void UpdateTimeline(float time)
        {
            if (time >= 0 && time < MaxTime)
            {
                Time = time;
            }
        }
    }
}
