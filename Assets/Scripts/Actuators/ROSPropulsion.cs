using System;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

public class ROSPropulsion : MonoBehaviour
{

    ROSConnection ros;
    [SerializeField, Tooltip("The transform that is rotated by sending angle commands")]
    private GameObject engineJoint;
    [SerializeField, Tooltip("The transform that spins the propeller (visual only)")]
    private GameObject propellerJoint;
    [SerializeField] private string topicName;
    private float angleSetpoint = 0.0f;
    private float angleCurr = 0.0f;

    [SerializeField, Range(0.00001f, 1.0f)]
    [Tooltip("Controls the responsiveness of the angle servo")]
    private float angleK = 0.005f;

    private float thrustCommand = 0.0f;
    [SerializeField, Range(0.001f, 1.0f)]
    [Tooltip("Controls the responsiveness of the thrust force")]
    private float thrustK = 1.0f;
    [SerializeField] private float maxThrust = 250.0f;
    [SerializeField, Range(0.0f, 3000.0f)]
    private float maxRpmVisual = 500.0f;
    private float thrustCurr;
    [SerializeField, Tooltip("The rigid body to apply the forces to (the boat)")]
    private Rigidbody rigidBody;
    [SerializeField] private bool debug;


    private void Start()
    {
        ROSConnection.GetOrCreateInstance().Subscribe<Float32Msg>(topicName + "/thrust", thrustCallback);
        ROSConnection.GetOrCreateInstance().Subscribe<Float32Msg>(topicName + "/angle", angleCallback);

        thrustCurr = 0.0f;
        angleCurr = NormalizeAngle(engineJoint.transform.rotation.eulerAngles.y);
    }

    private void FixedUpdate()
    {
        // engine angle control
        float angleError = AngleDifference(angleSetpoint, angleCurr);
        float angleCorrection = angleK * angleError;
        engineJoint.transform.localEulerAngles = new Vector3(0.0f, angleCurr + angleCorrection, 0.0f); ;
        angleCurr = NormalizeAngle(engineJoint.transform.localEulerAngles.y);


        // force control
        float thrustSetpoint = thrustCommand * maxThrust;
        float thrustError = thrustSetpoint - thrustCurr;
        thrustCurr += Mathf.Clamp(thrustError * thrustK, -maxThrust, maxThrust);
        Vector3 thrustDirLocal = new Vector3(Mathf.Sin(Mathf.Deg2Rad * angleCurr), 0.0f, Mathf.Cos(Mathf.Deg2Rad * angleCurr));
        Vector3 thrustDir = transform.TransformDirection(thrustDirLocal);
        Vector3 thrustForce = thrustCurr * thrustDir;
        rigidBody.AddForceAtPosition(thrustForce, propellerJoint.transform.position);

        if (debug) Debug.DrawRay(propellerJoint.transform.position, thrustForce / maxThrust);

        // propeller visual control
        float propAngleTurnRate = (thrustCurr / maxThrust) * (maxRpmVisual / 60);
        Quaternion rotation = Quaternion.Euler(0.0f, 0.0f, -propAngleTurnRate * 360.0f * Time.deltaTime);
        propellerJoint.transform.localRotation *= rotation;
    }

    private void thrustCallback(Float32Msg msg)
    {
        thrustCommand = Math.Clamp(msg.data, -1.0f, 1.0f);
    }
    private void angleCallback(Float32Msg msg)
    {
        angleSetpoint = msg.data;
    }

    // This function normalizes an angle to [0, 360) degrees
    float NormalizeAngle(float angle)
    {
        while (angle < 0.0f) angle += 360.0f;
        while (angle >= 360.0f) angle -= 360.0f;
        return angle;
    }

    // Computes the shortest difference between two angles
    float AngleDifference(float a, float b)
    {
        float difference = NormalizeAngle(a - b);
        if (difference > 180.0f)
            difference -= 360.0f;
        return difference;
    }
}
