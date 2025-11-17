using UnityEngine;
using System;
using Sim.Utils;

namespace Sim.Actuators.Motors {
    [CreateAssetMenu(menuName = "Motors/ThrusterConfig")]
    public class ThrusterConfig : ScriptableObject, IMotorConfig {
        public float minAngle { get => float.NegativeInfinity; set => minAngle = float.NegativeInfinity; }
        public float maxAngle { get => float.PositiveInfinity; set => maxAngle = float.PositiveInfinity; }

        [field: SerializeField] public MotorControlMode controlMode { get; set; } = MotorControlMode.Torque;
        [field: SerializeField] public Axis rotationAxis { get; set; } = Axis.Y;
        [field: SerializeField, Tooltip("Pid controller for position control")] public Pid pid { get; set; } = new Pid(3, 0, 2);
        [field: SerializeField, Tooltip("Nm")] public float maxTorque { get; set; } = 1.3f;
        [field: SerializeField, Tooltip("rad/s, Caps angular velocity")] public float maxAngularVelocity { get; set; } = 200f;
        [field: SerializeField, Tooltip("Torque Responsiveness"), Range(0.0f, 1.0f)] public float torqueK { get; set; } = 1.0f;
    }
}
