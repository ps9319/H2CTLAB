using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RayMarchingRenderFeature : ScriptableRendererFeature
{
    class RayMarchingPass : ScriptableRenderPass
    {
        public Material raymarchMat;
        public RenderTargetIdentifier source;
        public RenderTargetHandle tempTexture;
        public System.Action<Material> setParams;

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (raymarchMat == null) return;
            setParams?.Invoke(raymarchMat);

            CommandBuffer cmd = CommandBufferPool.Get("RayMarchingPass");
            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            opaqueDesc.depthBufferBits = 0;

            cmd.GetTemporaryRT(tempTexture.id, opaqueDesc, FilterMode.Bilinear);
            cmd.Blit(source, tempTexture.Identifier());
            cmd.Blit(tempTexture.Identifier(), source, raymarchMat);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    RayMarchingPass rayMarchingPass;
    public Material raymarchMaterial;

    public override void Create()
    {
        rayMarchingPass = new RayMarchingPass
        {
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents,
            raymarchMat = raymarchMaterial,
            tempTexture = new RenderTargetHandle()
        };
        rayMarchingPass.tempTexture.Init("_TemporaryColorTexture");
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // 씬에서 세터를 매번 찾아서 할당
        var setter = GameObject.FindObjectOfType<RayMarchingParamsSetter>();
        rayMarchingPass.setParams = setter != null ? (System.Action<Material>)setter.SetShaderParams : null;
        rayMarchingPass.source = renderer.cameraColorTarget;
        renderer.EnqueuePass(rayMarchingPass);
    }
}
