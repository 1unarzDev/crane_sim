using System;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;

namespace Sim.Utils.ROS {
    [Serializable]
    public class SubscriberBase<T> : MonoBehaviour where T : Unity.Robotics.ROSTCPConnector.MessageGeneration.Message {
    }
}
