using UnityEngine;
using RosMessageTypes.Nav;
using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

namespace Sim.Sensors.Nav {
    public class Odom : ROSSensorBase<OdometryMsg> {
        private Rigidbody odomBody;

        protected override void SetSensorDefaults() {
            if (string.IsNullOrEmpty(topicName)) topicName = "odometry";
            if (string.IsNullOrEmpty(frameId)) frameId = "odom";
            if (Hz == 0.0f) Hz = 50.0f;
        }

        protected override void Start() {
            base.Start();
            odomBody = gameObject.GetComponent<Rigidbody>();
        }

        protected override OdometryMsg CreateSensorMessage() {
            OdometryMsg msg = new();
            Vector3 pos = transform.position;
            msg.pose.pose.position = new PointMsg() {
                x = pos.z,
                y = -pos.x,
                z = pos.y
            };

            msg.pose.pose.orientation = odomBody.transform.rotation.To<FLU>();

            Vector3 localVel = transform.InverseTransformDirection(odomBody.linearVelocity);
            msg.twist.twist.linear.x = localVel.z;
            msg.twist.twist.linear.y = -localVel.x;
            msg.twist.twist.linear.z = localVel.y;
            msg.twist.twist.angular.z = -odomBody.angularVelocity.y;

            msg.header = CreateHeader();

            return msg;
        }

        private void Update() {
            UpdatePublish();
        }
    }
}
