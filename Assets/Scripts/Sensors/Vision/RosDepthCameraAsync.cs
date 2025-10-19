using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using UnityEngine;
using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Sim.Sensors.Vision
{
    [RequireComponent(typeof(Camera))]
    public class RosDepthCameraAsync : IROSSensor<ImageMsg>
    {
        private static byte[] s_ScratchSpace;
        [SerializeField] private RenderTexture depthRenderTexture;

        private CustomPassVolume customPassVolume;
        private CameraDepthBake depthBakePass = new();
        private Camera cam;
        private Texture2D depthTex2D;

        protected override void SetSensorDefaults()
        {
            if (string.IsNullOrEmpty(topicName)) topicName = "camera/depth";
            if (string.IsNullOrEmpty(frameId)) frameId = "camera_link_optical_frame";
            if (Hz == 0.0f) Hz = 15.0f;
        }

        protected override void Start()
        {
            base.Start();

            cam = GetComponent<Camera>();
            customPassVolume = gameObject.AddComponent<CustomPassVolume>();
            customPassVolume.injectionPoint = CustomPassInjectionPoint.AfterPostProcess;
            customPassVolume.targetCamera = cam;
            depthBakePass.bakingCamera = cam;
            depthBakePass.depthTexture = depthRenderTexture;
            customPassVolume.customPasses.Add(depthBakePass);
        }

        protected override ImageMsg CreateMessage()
        {
            return GetDepthImageMsg(depthTex2D, CreateHeader());
        }

        private void Update()
        {
            timeSincePublish += Time.deltaTime;
            if (timeSincePublish > 1.0f / Hz)
            {
                RequestReadback(depthRenderTexture);
                timeSincePublish = 0.0f;
            }
        }

        private void RequestReadback(RenderTexture targetTexture)
        {
            AsyncGPUReadback.Request(targetTexture, 0, TextureFormat.RFloat, OnReadbackComplete);
        }

        private void OnReadbackComplete(AsyncGPUReadbackRequest request)
        {
            if (request.hasError)
            {
                Debug.LogError("Failed to read back texture once");
                return;
            }

            if (depthTex2D == null || depthTex2D.width != depthRenderTexture.width || depthTex2D.height != depthRenderTexture.height)
            {
                depthTex2D = new Texture2D(depthRenderTexture.width, depthRenderTexture.height, TextureFormat.RFloat, false);
            }

            depthTex2D.LoadRawTextureData(request.GetData<byte>());
            depthTex2D.Apply();
            if (publishData) ros.Publish(topicName, CreateMessage());
        }

        ImageMsg GetDepthImageMsg(Texture2D tex, HeaderMsg header)
        {
            byte[] data = null;
            string encoding = "32FC1";
            int step = 4 * tex.width;

            var floatData = tex.GetPixelData<float>(0).ToArray();
            data = new byte[floatData.Length * 4];
            Buffer.BlockCopy(floatData, 0, data, 0, data.Length);
            ReverseInBlocks(data, tex.width * 4, tex.height);
            return new ImageMsg(header, (uint)tex.height, (uint)tex.width, encoding, 0, (uint)step, data);
        }

        private void ReverseInBlocks(byte[] array, int blockSize, int numBlocks)
        {
            if (blockSize * numBlocks > array.Length)
            {
                Debug.LogError($"Invalid ReverseInBlocks, array length is {array.Length}, should be at least {blockSize * numBlocks}");
                return;
            }

            if (s_ScratchSpace == null || s_ScratchSpace.Length < blockSize)
                s_ScratchSpace = new byte[blockSize];

            int startBlockIndex = 0;
            int endBlockIndex = ((int)numBlocks - 1) * blockSize;

            while (startBlockIndex < endBlockIndex)
            {
                Buffer.BlockCopy(array, startBlockIndex, s_ScratchSpace, 0, blockSize);
                Buffer.BlockCopy(array, endBlockIndex, array, startBlockIndex, blockSize);
                Buffer.BlockCopy(s_ScratchSpace, 0, array, endBlockIndex, blockSize);
                startBlockIndex += blockSize;
                endBlockIndex -= blockSize;
            }
        }
    }
}
