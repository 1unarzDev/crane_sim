using RosMessageTypes.Sensor;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace Sim.Sensors.Vision {
    public class CameraInfo : ROSSensorBase<CameraInfoMsg> {
        private Camera sensorCamera;

        protected override void SetSensorDefaults() {
            if (string.IsNullOrEmpty(topicName)) topicName = "camera/camera_info";
            if (string.IsNullOrEmpty(frameId)) frameId = "camera_link_optical_frame";
            if (Hz == 0.0f) Hz = 5.0f;
        }

        protected override void Start() {
            base.Start();
            sensorCamera = gameObject.GetComponent<Camera>();
        }

        protected override CameraInfoMsg CreateSensorMessage() {
            return CameraInfoGenerator.ConstructCameraInfoMessage
            (
                sensorCamera, CreateHeader(), 0f, 1.0f
            );
        }

        private void Update() {
            UpdatePublish();
        }
    }
}
