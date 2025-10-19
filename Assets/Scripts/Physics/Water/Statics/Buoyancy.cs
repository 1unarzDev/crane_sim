using System.Diagnostics;
using System.IO;
using WaterInteraction;
using UnityEngine;

[RequireComponent(typeof(Submersion))]
public class Buoyancy : MonoBehaviour
{
    public bool buoyancyForceActive = true;
    public bool useArticulationBody = false;
    private Vector3 buoyancyCenter = new Vector3();
    private Submersion submersion;
    private Rigidbody rigidBody;
    private ArticulationBody articulationBody;

    private void Start()
    {
        if (useArticulationBody) articulationBody = GetComponent<ArticulationBody>();
        else rigidBody = GetComponent<Rigidbody>();
        submersion = GetComponent<Submersion>();
    }


    private void FixedUpdate()
    {
        if (!buoyancyForceActive) return;
        ApplyBuoyancyVolume();
    }


    private void ApplyBuoyancyVolume()
    {
        buoyancyCenter = submersion.submerged.data.centroid;
        float displacedVolume = submersion.submerged.data.volume;
        float buoyancyForce = Constants.waterDensity * Constants.gravity * displacedVolume;
        Vector3 forceVector = new Vector3(0f, buoyancyForce, 0f);
        if (useArticulationBody) articulationBody.AddForceAtPosition(forceVector, buoyancyCenter);
        else rigidBody.AddForceAtPosition(forceVector, buoyancyCenter);
    }
}
