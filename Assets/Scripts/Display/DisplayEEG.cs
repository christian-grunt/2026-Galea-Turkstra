using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using OpenBCI.Network.Streams;
using OpenBCI.Markers;
using OpenBCI.EyeTracking;

namespace onnx
{
    public class DisplayEEG : MonoBehaviour
    {
        [Header("Galea References")]
        public OpenBCI.Network.Streams.BandPowerStream galeaBandPowerStream;
        public EyeGazeManager eyeTracker;
        public ToggleMarker trialMarker;
        public DisplayEMG emgStream;

        [Header("Hardware Channel Map")]
        // O1, O2 -> { 15, 16 }
        public int[] targetChannelIndices = new int[] { 15, 16 };

        [Header("State")]
        public bool isRecording = false;
        public bool recordingBaseline = false;
        private List<float> trialDataBuffer = new List<float>();
        private List<float> baselineDataBuffer = new List<float>();
        public float currentRatio = 0f;
        public float currentAlpha = 0f;

        [Header("Baseline Stats")]
        public float baselineMean = 0f;
        public float baselineStdDev = 1f;
        public bool hasBaseline = false;
        public float blinkThreshold = 0.1f;
        public bool isEyeTracking = true;

        private string csvFilePath;

        void Start()
        {
            
            Debug.Log("<color=white><b>[FocusManager]</b> Initialized. Waiting for data on Port 12347.</color>");
        }

        void Update()
        {
            // 1. Connection Heartbeat (Logs status every 5 seconds when idle)
            if (Time.frameCount % 450 == 0 && !isRecording)
            {
                CheckConnectionStatus();
            }

            if (!isRecording) return;

            // 2. Stream Validation
            if (galeaBandPowerStream == null || galeaBandPowerStream.Channels == null || galeaBandPowerStream.Channels.Length == 0)
            {
                if (Time.frameCount % 60 == 0)
                    Debug.LogWarning("<color=red>[STREAM ERROR]</color> Recording active but no data arriving from Galea!");
                return;
            }

            // 3. Data Collection
          
            if (!emgStream.drop && !(isEyeTracking && (eyeTracker.Openness.x > blinkThreshold || eyeTracker.Openness.y > blinkThreshold)))
            {
                currentAlpha = CalculateInstantaneousAlpha();
            }


            //trialDataBuffer.Add(currentRatio);
        }

        private float CalculateInstantaneousAlpha()
        {
            float sumAlpha = 0f;
            int validCount = 0;

            // Raw Value Trace (Logs detailed channel data every 2 seconds during trial)
            //bool shouldTrace = (Time.frameCount % 180 == 0);
            //StringBuilder trace = new StringBuilder();
            //if (shouldTrace) trace.AppendLine($"<color=cyan><b>[RAW TRACE]</b></color> Buffer Size: {trialDataBuffer.Count}");

            for (int j = 0; j < targetChannelIndices.Length; j++)
            {
                int i = targetChannelIndices[j];
                if (i < galeaBandPowerStream.Channels.Length)
                {
                    var ch = galeaBandPowerStream.Channels[i];
                    sumAlpha += ch.Alpha;
                    validCount++;

                }
            }
            
            //if (shouldTrace) Debug.Log(trace.ToString());

            if (validCount == 0) return 0f;

            float avgAlpha = sumAlpha / validCount;
            
            return avgAlpha;
        }

        private void CheckConnectionStatus()
        {
            if (galeaBandPowerStream == null)
                Debug.Log("<color=grey>[Status]</color> Galea Stream Reference Missing.");
            else if (galeaBandPowerStream.Channels == null || galeaBandPowerStream.Channels.Length == 0)
                Debug.Log("<color=grey>[Status]</color> Galea Offline (No UDP Packets).");
            else
                Debug.Log($"<color=green>[Status]</color> Galea Online. Receiving {galeaBandPowerStream.Channels.Length} channels.");
        }

        public IEnumerator RecordBaseline(float duration = 10f, float frequency = 30f)
        {
            baselineDataBuffer.Clear();
            float startTime = Time.time;

            while (Time.time - startTime < duration)
            {
                if (!recordingBaseline) break;

                float currentAlpha = CalculateInstantaneousAlpha();

                baselineDataBuffer.Add(currentAlpha);

                yield return new WaitForSeconds(1f / frequency);
            }

            ComputeBaseline();

            BaseLineRecorded();
            
        }

        private void ComputeBaseline()
        {
            if (baselineDataBuffer.Count < 5) return;
            baselineMean = baselineDataBuffer.Average();
            float variance = 0f;

            foreach (float sample in baselineDataBuffer)
            {
                variance += Mathf.Pow(sample - baselineMean, 2);
            }
            variance /= baselineDataBuffer.Count();
            baselineStdDev = Mathf.Sqrt(variance);
            

        }

        private void BaseLineRecorded()
        {
            recordingBaseline = false;
            // isRecording = true;

        }
    }
}