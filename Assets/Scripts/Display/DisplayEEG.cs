using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using OpenBCI.Network.Streams;
using OpenBCI.Markers;

namespace onnx
{
    public class FocusDataManager : MonoBehaviour
    {
        [Header("Galea References")]
        public BandPowerStream galeaBandPowerStream;
        public ToggleMarker trialMarker;
        public DisplayEMG emgStream;

        [Header("Hardware Channel Map")]
        // F1, F2, C3, C4, P3, P4, CZ, PZ -> { 8, 9, 10, 11, 12, 13, 16, 17 }
        public int[] targetChannelIndices = new int[] { 8, 9, 10, 11, 12, 13, 16, 17 };

        [Header("State")]
        private bool isRecording = false;
        private List<float> trialDataBuffer = new List<float>();
        private List<float> baselineDataBuffer = new List<float>();
        public float currentRatio = 0f;

        [Header("Baseline Stats")]
        public float baselineMean = 0f;
        public float baselineStdDev = 1f;
        public bool hasBaseline = false;

        private string csvFilePath;

        void Start()
        {
            
            Debug.Log("<color=white><b>[FocusManager]</b> Initialized. Waiting for data on Port 12347.</color>");
        }

        public void StopTrialRecording(int trialIndex)
        {
            isRecording = false;
            if (trialMarker != null) trialMarker.enabled = false;

            float avg = GetTrialAverageFocus();
            Debug.Log($"<color=orange><b>[TRIAL END]</b></color> Trial {trialIndex} | Samples: {trialDataBuffer.Count} | Avg Ratio: {avg:F4}");

            SaveTrialToCSV(trialIndex);
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
            if (!emgStream.clip) currentRatio = CalculateInstantaneousRatio();
            //trialDataBuffer.Add(currentRatio);
        }

        private float CalculateInstantaneousRatio()
        {
            float sumTheta = 0f, sumAlpha = 0f, sumBeta = 0f;
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
                    sumTheta += ch.Theta;
                    sumAlpha += ch.Alpha;
                    sumBeta += ch.Beta;
                    validCount++;

                    if (shouldTrace)
                    {
                        // Identify channel name for the log (F1, F2, etc based on your index list)
                        trace.AppendLine($"  Idx {i}: Th={ch.Theta:F2}, Al={ch.Alpha:F2}, Be={ch.Beta:F2}");
                    }
                }
            }

            //if (shouldTrace) Debug.Log(trace.ToString());

            if (validCount == 0) return 0f;

            float avgTheta = sumTheta / validCount;
            float avgAlpha = sumAlpha / validCount;
            float avgBeta = sumBeta / validCount;

            float denom = avgAlpha + avgTheta;
            return (denom > 0.0001f) ? (avgBeta / denom) : 0f;
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

        
    }
}