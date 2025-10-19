using System;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Rosgraph;
using Unity.Robotics.Core;

public class ROSClockPublisher : MonoBehaviour
{
    [SerializeField] private Clock.ClockMode m_ClockMode;

    [SerializeField, HideInInspector] private Clock.ClockMode m_LastSetClockMode;

    [SerializeField] private double m_PublishRateHz = 100f;

    private double m_LastPublishTimeSeconds;

    ROSConnection m_ROS;

    private double PublishPeriodSeconds => 1.0f / m_PublishRateHz;

    private bool ShouldPublishMessage => Clock.FrameStartTimeInSeconds - PublishPeriodSeconds > m_LastPublishTimeSeconds;

    private void OnValidate()
    {
        var clocks = FindObjectsByType<ROSClockPublisher>(FindObjectsSortMode.None);
        if (clocks.Length > 1)
        {
            Debug.LogWarning("Found too many clock publishers in the scene, there should only be one!");
        }

        if (Application.isPlaying && m_LastSetClockMode != m_ClockMode)
        {
            Debug.LogWarning("Can't change ClockMode during simulation! Setting it back...");
            m_ClockMode = m_LastSetClockMode;
        }

        SetClockMode(m_ClockMode);
    }

    private void SetClockMode(Clock.ClockMode mode)
    {
        Clock.Mode = mode;
        m_LastSetClockMode = mode;
    }

    // Start is called before the first frame update
    private void Start()
    {
        SetClockMode(m_ClockMode);
        m_ROS = ROSConnection.GetOrCreateInstance();
        m_ROS.RegisterPublisher<ClockMsg>("clock");
    }

    private void PublishMessage()
    {
        var publishTime = Clock.time;
        var clockMsg = new TimeMsg
        {
            sec = (int)publishTime,
            nanosec = (uint)((publishTime - Math.Floor(publishTime)) * Clock.k_NanoSecondsInSeconds)
        };
        m_LastPublishTimeSeconds = publishTime;
        m_ROS.Publish("clock", clockMsg);
    }

    private void Update()
    {
        if (ShouldPublishMessage)
        {
            PublishMessage();
        }
    }
}
