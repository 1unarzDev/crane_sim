using RosMessageTypes.Sensor;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace Sim.Sensors.Vision {
    [RequireComponent(typeof(Camera))]
    public class ROSCameraAsync : ROSSensorBase<ImageMsg> {
        [SerializeField] private RenderTexture rgbRenderTexture;
        private Camera sensorCamera;
        private Texture2D rgbTexture2D;

        protected override void SetSensorDefaults() {
            if (string.IsNullOrEmpty(topicName)) topicName = "camera/image_raw";
            if (string.IsNullOrEmpty(frameId)) frameId = "camera_link_optical_frame";
            if (Hz == 0.0f) Hz = 15.0f;
        }

        protected override void Start() {
            base.Start();
            sensorCamera = gameObject.GetComponent<Camera>();
            sensorCamera.targetTexture = rgbRenderTexture;
        }

        protected override ImageMsg CreateSensorMessage() {
            return rgbTexture2D.ToImageMsg(CreateHeader());
        }

        private void Update() {
            timeSincePublish += Time.deltaTime;
            if (timeSincePublish > 1.0f / Hz) {
                RequestReadback(rgbRenderTexture);
                timeSincePublish = 0.0f;
            }
        }

        private void RequestReadback(RenderTexture targetTexture) {
            AsyncGPUReadback.Request(targetTexture, 0, TextureFormat.RGB24, OnReadbackComplete);
        }

        private void OnReadbackComplete(AsyncGPUReadbackRequest request) {
            if (request.hasError) {
                Debug.LogWarning("Failed to read back texture once");
                return;
            }

            if (rgbTexture2D == null || rgbTexture2D.width != sensorCamera.targetTexture.width || rgbTexture2D.height != sensorCamera.targetTexture.height) {
                rgbTexture2D = new Texture2D(sensorCamera.targetTexture.width, sensorCamera.targetTexture.height, TextureFormat.RGB24, false);
            }

            rgbTexture2D.LoadRawTextureData(request.GetData<byte>());
            rgbTexture2D.Apply();

            if (publishData) ros.Publish(topicName, CreateSensorMessage());
        }

    }
}
