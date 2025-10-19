using UnityEngine;
using RosMessageTypes.Sensor;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

namespace Sim.Sensors.Nav
{
    public class Imu : IROSSensor<ImuMsg>
    {
        private Rigidbody imuBody;

        protected override void SetSensorDefaults()
        {
            if (string.IsNullOrEmpty(topicName)) topicName = "imu/raw";
            if (string.IsNullOrEmpty(frameId)) frameId = "imu_link";
            if (Hz == 0.0f) Hz = 50.0f;
        }

        protected override void Start()
        {
            base.Start();
            imuBody = gameObject.GetComponent<Rigidbody>();
        }

        protected override ImuMsg CreateMessage()
        {
            return new ImuMsg
            {
                orientation = imuBody.transform.rotation.To<FLU>(),
                angular_velocity = imuBody.angularVelocity.To<FLU>(),
                header = CreateHeader()
            };
        }

        private void Update()
        {
            UpdatePublish();
        }
    }
}
