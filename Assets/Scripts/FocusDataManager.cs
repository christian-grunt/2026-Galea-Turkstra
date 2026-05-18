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

        [Header("Hardware Channel Map")]
        // F1, F2, C3, C4, P3, P4, CZ, PZ -> { 8, 9, 10, 11, 12, 13, 16, 17 }
        public int[] targetChannelIndices = new int[] { 8, 9, 10, 11, 12, 13, 16, 17 };

        [Header("State")]
        private bool isRecording = false;
        private List<float> trialDataBuffer = new List<float>();
        private List<float> baselineDataBuffer = new List<float>();

        [Header("Baseline Stats")]
        public float baselineMean = 0f;
        public float baselineStdDev = 1f;
        public bool hasBaseline = false;

        private string csvFilePath;

        void Start()
        {
            PrepareCSV();
            if (trialMarker != null) trialMarker.enabled = false;
            Debug.Log("<color=white><b>[FocusManager]</b> Initialized. Waiting for data on Port 12347.</color>");
        }

        public void StartTrialRecording(string targetName, int trialIndex)
        {
            trialDataBuffer.Clear();
            isRecording = true;
            if (trialMarker != null) trialMarker.enabled = true;
            Debug.Log($"<color=orange><b>[TRIAL START]</b></color> Trial {trialIndex} | Target: {targetName}");
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
            float currentRatio = CalculateInstantaneousRatio();
            trialDataBuffer.Add(currentRatio);
        }

        private float CalculateInstantaneousRatio()
        {
            float sumTheta = 0f, sumAlpha = 0f, sumBeta = 0f;
            int validCount = 0;

            // Raw Value Trace (Logs detailed channel data every 2 seconds during trial)
            bool shouldTrace = (Time.frameCount % 180 == 0);
            StringBuilder trace = new StringBuilder();
            if (shouldTrace) trace.AppendLine($"<color=cyan><b>[RAW TRACE]</b></color> Buffer Size: {trialDataBuffer.Count}");

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

            if (shouldTrace) Debug.Log(trace.ToString());

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

        public float GetTrialAverageFocus() => trialDataBuffer.Count > 0 ? trialDataBuffer.Average() : 0f;

        public float GetZScoredTrialFocus()
        {
            float avg = GetTrialAverageFocus();
            if (!hasBaseline || baselineStdDev < 0.0001f) return 0f;
            return (avg - baselineMean) / baselineStdDev;
        }

        public void AddTrialToBaseline() => baselineDataBuffer.AddRange(trialDataBuffer);

        public void ComputeBaselineMetrics()
        {
            if (baselineDataBuffer.Count < 5) return;
            baselineMean = baselineDataBuffer.Average();
            float sumSq = baselineDataBuffer.Sum(v => Mathf.Pow(v - baselineMean, 2));
            baselineStdDev = Mathf.Sqrt(sumSq / baselineDataBuffer.Count);
            hasBaseline = true;
            Debug.Log($"<color=green><b>[BASELINE SUCCESS]</b></color> M={baselineMean:F4}, SD={baselineStdDev:F4} (Samples: {baselineDataBuffer.Count})");
        }

        private void PrepareCSV()
        {
            string folder = Path.Combine(Application.persistentDataPath, "FocusData");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            csvFilePath = Path.Combine(folder, $"FocusLog_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv");
            File.WriteAllText(csvFilePath, "Timestamp,TrialIndex,SampleCount,RawFocusRatio,ZScoredFocus\n");
        }

        private void SaveTrialToCSV(int trialIndex)
        {
            string line = $"{System.DateTime.Now:HH:mm:ss.fff},{trialIndex},{trialDataBuffer.Count},{GetTrialAverageFocus():F4},{GetZScoredTrialFocus():F4}";
            File.AppendAllLines(csvFilePath, new[] { line });
        }

        public struct NeuroDataSummary
        {
            public float theta, alpha, beta, ratio, zScore;
        }

        public NeuroDataSummary GetTrialSummary()
        {
            float sumTheta = 0, sumAlpha = 0, sumBeta = 0;
            // We iterate through the raw stream to get the current averages
            // for just the target channels
            foreach (int i in targetChannelIndices)
            {
                if (i < galeaBandPowerStream.Channels.Length)
                {
                    sumTheta += galeaBandPowerStream.Channels[i].Theta;
                    sumAlpha += galeaBandPowerStream.Channels[i].Alpha;
                    sumBeta += galeaBandPowerStream.Channels[i].Beta;
                }
            }

            int count = targetChannelIndices.Length;
            return new NeuroDataSummary
            {
                theta = sumTheta / count,
                alpha = sumAlpha / count,
                beta = sumBeta / count,
                ratio = GetTrialAverageFocus(),
                zScore = GetZScoredTrialFocus()
            };
        }
    }
}
