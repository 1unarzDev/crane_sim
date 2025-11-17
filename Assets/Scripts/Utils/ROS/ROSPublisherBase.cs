using System;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

namespace Sim.Utils.ROS {
    public abstract class ROSPublisherBase<T> : MonoBehaviour where T : Unity.Robotics.ROSTCPConnector.MessageGeneration.Message {
        [SerializeField] protected string topicName;
        [SerializeField] protected string frameId;
        [SerializeField] protected bool publishData = true;
        [SerializeField, Range(1.0f, 100.0f)] protected float Hz;

        protected ROSConnection ros;
        protected float timeSincePublish;

        protected abstract void SetDefaults();
        protected abstract T CreateMessage();

        protected virtual void OnValidate() {
            SetDefaults();
        }

        protected virtual void Start() {
            ros = ROSConnection.GetOrCreateInstance();
            ros.RegisterPublisher<T>(topicName);
            timeSincePublish = 0.0f;
        }

        protected virtual void UpdatePublish() {
            timeSincePublish += Time.deltaTime;
            if (publishData && timeSincePublish < 1.0f / Hz) return;
            ros.Publish(topicName, CreateMessage());
            timeSincePublish = 0.0f;
        }

        protected HeaderMsg CreateHeader() {
            var header = new HeaderMsg { frame_id = frameId };

            var publishTime = Clock.Now;
            var sec = publishTime;
            var nanosec = (publishTime - Math.Floor(publishTime)) * Clock.k_NanoSecondsInSeconds;
            header.stamp.sec = (int)sec;
            header.stamp.nanosec = (uint)nanosec;

            return header;
        }
    }
}
