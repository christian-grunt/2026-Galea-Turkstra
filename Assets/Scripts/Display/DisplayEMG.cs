using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using OpenBCI.Network.Streams;
using OpenBCI.Markers;

namespace onnx
{
    public class DisplayEMG : MonoBehaviour
    {
        [Header("Galea References")]
        public OpenBCI.Network.Streams.EMGStream galeaEMGStream;
        public ToggleMarker trialMarker;

        [Header("Hardware Channel Map")]
        // EMG Channels -> { 1,2,3,4,7,8 }
        public int[] targetChannelIndices = new int[] {1,2,3,4,7,8 };

        [Header("State")]
        private List<float> trialDataBuffer = new List<float>();
        private List<float> baselineDataBuffer = new List<float>();
        public float[] channelReadouts = new float[6];
        public bool drop = false;
        public bool recordingBaseline = false;
        public bool isRecording = true;

        [Header("Baseline Stats")]
        public float baselineMean = 0f;
        public float baselineStdDev = 1f;
        public bool hasBaseline = false;

        private string csvFilePath;

        void Start()
        {
            // How do we set port? I think it was a field in the prefab
            Debug.Log("<color=white><b>[DisplayEMG]</b> Initialized. Waiting for data on Port 12347.</color>");
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
            if (galeaEMGStream == null || galeaEMGStream.Channels == null || galeaEMGStream.Channels.Length == 0)
            {
                if (Time.frameCount % 60 == 0)
                    Debug.LogWarning("<color=red>[STREAM ERROR] : <b>[DisplayEMG]</b></color> Recording active but no data arriving from Galea!");
                return;
            }

            // 3. Data Collection
            int shouldDrop = 0;
            for (int j = 0; j < targetChannelIndices.Length; j++)
            {
                int i = targetChannelIndices[j];
                if (i < galeaEMGStream.Channels.Length)
                {
                    channelReadouts[j] = galeaEMGStream.Channels[i];
                    if (channelReadouts[j] > 0.9999) {
                        shouldDrop ++;
                    }
                }
            }
            drop = shouldDrop > 1;

            
        }

        private void CheckConnectionStatus()
        {
            if (galeaEMGStream == null)
                Debug.Log("<color=grey>[Status]</color> Galea Stream Reference Missing.");
            else if (galeaEMGStream.Channels == null || galeaEMGStream.Channels.Length == 0)
                Debug.Log("<color=grey>[Status]</color> Galea Offline (No UDP Packets).");
            else
                Debug.Log($"<color=green>[Status]</color> Galea Online. Receiving {galeaEMGStream.Channels.Length} channels.");
        }

        
    }
}