using UnityEngine;

public class RayMarchingParamsSetter : MonoBehaviour
{
    public Seb.Fluid.Rendering.RayMarchingTest.EnvironmentSettings environmentSettings;
    public Seb.Fluid.Simulation.FluidSim sim;
    public Transform cubeTransform;
    public float densityOffset = 150;
    public int numRefractions = 4;
    public Vector3 extinctionCoefficients;
    public float densityMultiplier = 0.001f;
    public float stepSize = 0.02f;
    public float lightStepSize = 0.4f;
    public float indexOfRefraction = 1.33f;
    public Vector3 testParams;

    public void SetShaderParams(Material raymarchMat)
    {
        Seb.Fluid.Rendering.RayMarchingTest.SetEnvironmentParams(raymarchMat, environmentSettings);
        raymarchMat.SetTexture("DensityMap", sim.DensityMap);
        raymarchMat.SetVector("boundsSize", sim.Scale);
        raymarchMat.SetFloat("volumeValueOffset", densityOffset);
        raymarchMat.SetVector("testParams", testParams);
        raymarchMat.SetFloat("indexOfRefraction", indexOfRefraction);
        raymarchMat.SetFloat("densityMultiplier", densityMultiplier / 1000);
        raymarchMat.SetFloat("viewMarchStepSize", stepSize);
        raymarchMat.SetFloat("lightStepSize", lightStepSize);
        raymarchMat.SetInt("numRefractions", numRefractions);
        raymarchMat.SetVector("extinctionCoeff", extinctionCoefficients);

        raymarchMat.SetMatrix("cubeLocalToWorld", Matrix4x4.TRS(cubeTransform.position, cubeTransform.rotation, cubeTransform.localScale / 2));
        raymarchMat.SetMatrix("cubeWorldToLocal", Matrix4x4.TRS(cubeTransform.position, cubeTransform.rotation, cubeTransform.localScale / 2).inverse);

        Vector3 floorSize = new Vector3(30, 0.05f, 30);
        float floorHeight = -sim.Scale.y / 2 + sim.transform.position.y - floorSize.y / 2;
        raymarchMat.SetVector("floorPos", new Vector3(0, floorHeight, 0));
        raymarchMat.SetVector("floorSize", floorSize);
    }
}
