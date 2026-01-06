using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using UnityEngine;
using Sim.Utils;
using Sim.Controllers;

namespace Sim.Sensors.Nav {
    [Serializable]
    public class SITLCommsJsonIMUData {
        public float[] gyro = new float[] { 0.0f, 0.0f, 0.0f };
        public float[] accel_body = new float[] { 0.0f, -Constants.gravity, 0.0f };
    }

    [Serializable]
    public class SITLCommsJsonOutputPacket {
        public float timestamp = 0;
        public SITLCommsJsonIMUData imu = new();
        public float[] position = new float[] { 0.0f, 0.0f, 0.0f };
        public float[] attitude = new float[] { 0.0f, 0.0f, 0.0f };
        public float[] velocity = new float[] { 0.0f, 0.0f, 0.0f };
    }

    public class MAVROSConnection : MonoBehaviour {
        [SerializeField] private Imu imu;
        [SerializeField] private int localPort = 9002;
        [SerializeField] private OmniXController controller;
        [SerializeField] private float pwmMin;
        [SerializeField] private float pwmMax;

        private UdpClient socketReceive;
        private UdpClient socketSend;
        private Thread receiveThread;
        private bool hasRemoteConnection = false;
        private IPEndPoint remoteEndpoint = new(IPAddress.Any, 0);

        private SITLCommsJsonOutputPacket data = new();
        private long startTime;

        void Start() {
            Debug.Log($"Starting MAVROS UDP thread on port {localPort}");

            socketReceive = new UdpClient(localPort);
            socketSend = new UdpClient();
            receiveThread = new Thread(ReceiveDataLoop);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        void OnDisable() {
            receiveThread?.Abort();
            socketReceive?.Close();
            socketSend?.Close();
            Debug.Log("Stopping MAVROS UDP thread");
        }

        void Update() {
            // Update telemetry
            data.timestamp = (DateTimeOffset.Now.ToUnixTimeMilliseconds() - startTime) / 1000f;

            data.imu.gyro = new float[] {
                -imu.body.angularVelocity.x * Mathf.Deg2Rad,
                -imu.body.angularVelocity.z * Mathf.Deg2Rad,
                imu.body.angularVelocity.y * Mathf.Deg2Rad
            };

            data.imu.accel_body = new float[] { 0.0f, 0.0f, -Constants.gravity };

            data.position = new float[] {
                -imu.body.position.x,
                imu.body.position.z,
                imu.body.position.y
            };

            data.attitude = new float[] {
                imu.body.transform.eulerAngles.x * Mathf.Deg2Rad,
                -imu.body.transform.eulerAngles.z * Mathf.Deg2Rad,
                imu.body.transform.eulerAngles.y * Mathf.Deg2Rad
            };

            data.velocity = new float[] {
                -imu.body.linearVelocity.x,
                imu.body.linearVelocity.z,
                imu.body.linearVelocity.y
            };
        }

        private float MapPWM(float pwm) {
            pwm = Math.Clamp(pwm, pwmMin, pwmMax);
            return controller.config.GetMinCommand() + (pwm - pwmMin) * (controller.config.GetMaxCommand() - controller.config.GetMinCommand()) / (pwmMax - pwmMin);
        }

        private void ReceiveDataLoop() {
            while (true) {
                try {
                    byte[] received = socketReceive.Receive(ref remoteEndpoint);

                    if (!hasRemoteConnection) {
                        hasRemoteConnection = true;
                        Debug.Log($"New SITL connection from {remoteEndpoint}");
                    }

                    using var reader = new BinaryReader(new MemoryStream(received), Encoding.UTF8, false);
                    UInt16 magic = reader.ReadUInt16();
                    UInt16 frameRate = reader.ReadUInt16();
                    UInt32 frameCount = reader.ReadUInt32();
                    UInt16[] pwm = new UInt16[16];
                    for (int i = 0; i < 16; i++)
                        pwm[i] = reader.ReadUInt16();

                    // Debug.Log($"Received frame {frameCount}, magic {magic}");
                    // Debug.Log(pwm[0] + " " + pwm[1] + " " + pwm[2] + " " + pwm[3]);
                    // TODO: Create a customizable mapping system to match various controllers with MAVROS expected outputs
                    if (!controller.movementOverride) {
                        controller.frontLeft.SetCommand(MapPWM(pwm[1]));
                        controller.frontRight.SetCommand(MapPWM(pwm[2]));
                        controller.rearRight.SetCommand(MapPWM(pwm[3]));
                        controller.rearLeft.SetCommand(MapPWM(pwm[0]));
                    }
                }
                catch (Exception ex) {
                    Debug.LogWarning(ex);
                }

                if (hasRemoteConnection) {
                    try {
                        string jsonStr = JsonUtility.ToJson(data) + "\n";
                        byte[] bytes = Encoding.UTF8.GetBytes(jsonStr);
                        socketSend.Send(bytes, bytes.Length, remoteEndpoint);
                    } catch (Exception ex) {
                        Debug.LogWarning($"Error sending telemetry: {ex.Message}");
                        hasRemoteConnection = false;
                    }
                }
            }
        }
    }
}
