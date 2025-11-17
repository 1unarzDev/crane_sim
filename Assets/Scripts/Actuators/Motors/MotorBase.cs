using UnityEngine;
using Sim.Utils;
using Sim.Utils.ROS;
using System;

namespace Sim.Actuators.Motors {
    public enum MotorControlMode {
        Torque,
        Velocity,
        Position
    }

    public enum Axis {
        X,
        Y,
        Z
    }

    public abstract class MotorBase<TConfig> : MonoBehaviour where TConfig : IMotorConfig, new() {
        [SerializeReference] protected TConfig config;
        protected GameObject motorJoint;
        protected float maxTorque, maxAngularVelocity, torqueK, minAngle, maxAngle, maxAngularAcceleration, inertiaAlongAxis;
        protected MotorControlMode controlMode;
        protected Axis rotationAxis;
        protected Pid pid;
        protected Vector3 worldAxis;
        protected IPhysicsBody body;
        protected float command;

        public float Command {
            get => command;
            set => command = value;
        }

        protected virtual void SetMotorDefaults() {
            motorJoint = gameObject;

            controlMode = config.controlMode;
            rotationAxis = config.rotationAxis;
            pid = config.pid;
            maxTorque = config.maxTorque;
            torqueK = config.torqueK;
            maxAngularVelocity = config.maxAngularVelocity;
            minAngle = config.minAngle;
            maxAngle = config.maxAngle;
        }

        protected virtual void OnValidate() {
            if (GetComponent<Rigidbody>() == null && GetComponent<ArticulationBody>() == null)
                Debug.LogWarning($"{name} should have either a Rigidbody or an ArticulationBody attached.");

            if (minAngle > maxAngle) {
                (maxAngle, minAngle) = (minAngle, maxAngle);
                Debug.LogWarning($"{name}: Swapped min/max angles because min > max");
            }

            config ??= Activator.CreateInstance<TConfig>();

            SetMotorDefaults();
        }

        protected virtual void Awake() {
            var rb = GetComponent<Rigidbody>();
            var ab = GetComponent<ArticulationBody>();

            if (rb != null) body = new RigidbodyAdapter(rb);
            else if (ab != null) body = new ArticulationBodyAdapter(ab);
            else throw new MissingComponentException($"{name} requires a Rigidbody or ArticulationBody!");
        }

        protected virtual void Start() {
            if (motorJoint == null)
                throw new MissingReferenceException($"{name}: motorJoint is not assigned.");

            Vector3 axisVector = rotationAxis switch {
                Axis.X => Vector3.right,
                Axis.Y => Vector3.up,
                Axis.Z => Vector3.forward,
                _ => Vector3.up
            };

            inertiaAlongAxis = Vector3.Dot(axisVector, body.inertiaTensorRotation * Vector3.Scale(body.inertiaTensor, axisVector));
            maxAngularAcceleration = maxTorque / inertiaAlongAxis;

            body.maxAngularVelocity = maxAngularVelocity;
            worldAxis = motorJoint.transform.TransformDirection(axisVector);
        }

        protected virtual void FixedUpdate() {
            switch (controlMode) {
                case MotorControlMode.Torque:
                    ApplyTorqueControl(command);
                    break;

                case MotorControlMode.Velocity:
                    ApplyVelocityControl(command);
                    break;

                case MotorControlMode.Position:
                    ApplyPositionControl(command);
                    break;
            }
        }

        protected virtual void ApplyTorqueControl(float torque) {
            // Smooth torque application using torqueK to simplify external factors slowing torque (near instant)
            torque = Mathf.Clamp(torque, -maxTorque, maxTorque);

            float torqueOutput = Mathf.MoveTowards(
                GetTorque(),
                torque,
                maxTorque * torqueK * Time.deltaTime
            );

            if (controlMode != MotorControlMode.Position)
                IsAtLimit(ref torque);

            body.AddTorque(worldAxis * torqueOutput);
        }

        protected virtual void ApplyVelocityControl(float targetVelocity) {
            float torque = inertiaAlongAxis * (targetVelocity - GetVelocity()) / Time.fixedDeltaTime;

            if (controlMode != MotorControlMode.Position)
                IsAtLimit(ref targetVelocity);

            ApplyTorqueControl(torque);
        }

        // Uses Pid controller for manual control (would have to be implemented in our code normally)
        protected virtual void ApplyPositionControl(float targetAngle) {
            targetAngle = Mathf.Clamp(targetAngle, minAngle, maxAngle);
            float currentAngle = GetAngle();
            float angleError = Mathf.DeltaAngle(currentAngle, targetAngle);
            float desiredVelocity = pid.Run(angleError, Time.fixedDeltaTime);
            desiredVelocity = Mathf.Clamp(desiredVelocity, -maxAngularVelocity, maxAngularVelocity);

            ApplyVelocityControl(desiredVelocity);
        }

        protected virtual float GetAlongAxis(Vector3 v, Axis axis) =>
            axis switch {
                Axis.X => v.x,
                Axis.Y => v.y,
                Axis.Z => v.z,
                _ => v.y
            };

        protected virtual float GetVelocity() => GetAlongAxis(body.angularVelocity, rotationAxis);
        protected virtual float GetAngle() => GetAlongAxis(motorJoint.transform.localEulerAngles, rotationAxis);
        protected virtual float GetTorque() => GetAlongAxis(body.GetAccumulatedTorque(), rotationAxis);

        protected virtual bool IsAtLimit(ref float command) {
            float currentAngle = GetAngle();
            if ((currentAngle <= minAngle && command < 0f) ||
                (currentAngle >= maxAngle && command > 0f)) {
                command = 0f;
                body.angularVelocity = Vector3.zero;
                return true;
            }
            return false;
        }
    }
}
