using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace WaterSystem
{
    public class WaterSystemFeature : ScriptableRendererFeature
    {
        #region Water Effects Pass

        class WaterFxPass : ScriptableRenderPass
        {
            private const string k_RenderWaterFXTag = "Render Water FX";

            private readonly ProfilingSampler m_WaterFX_Profile = new ProfilingSampler(k_RenderWaterFXTag);
            private readonly ShaderTagId m_WaterFXShaderTag = new ShaderTagId("WaterFX");

            // r = foam mask, g = normal.x, b = normal.z, a = displacement
            private readonly Color m_ClearColor = new Color(0.0f, 0.5f, 0.5f, 0.5f);

            private FilteringSettings m_FilteringSettings;

            private RTHandle m_WaterFX;
            private static readonly int WaterFXMapID = Shader.PropertyToID("_WaterFXMap");

            public WaterFxPass()
            {
                // Only render transparent objects
                m_FilteringSettings = new FilteringSettings(RenderQueueRange.transparent);
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                // RTHandle color targets must not have depth.
                cameraTextureDescriptor.depthBufferBits = 0;
                cameraTextureDescriptor.msaaSamples = 1;

                // Half resolution
                cameraTextureDescriptor.width = Mathf.Max(1, cameraTextureDescriptor.width / 2);
                cameraTextureDescriptor.height = Mathf.Max(1, cameraTextureDescriptor.height / 2);

                cameraTextureDescriptor.colorFormat = RenderTextureFormat.Default;

                RenderingUtils.ReAllocateIfNeeded(
                    ref m_WaterFX,
                    cameraTextureDescriptor,
                    FilterMode.Bilinear,
                    TextureWrapMode.Clamp,
                    name: "_WaterFXMap"
                );

                ConfigureTarget(m_WaterFX);
                ConfigureClear(ClearFlag.Color, m_ClearColor);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (m_WaterFX == null)
                    return;

                CommandBuffer cmd = CommandBufferPool.Get(k_RenderWaterFXTag);

                using (new ProfilingScope(cmd, m_WaterFX_Profile))
                {
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    var drawSettings = CreateDrawingSettings(
                        m_WaterFXShaderTag,
                        ref renderingData,
                        SortingCriteria.CommonTransparent
                    );

                    context.DrawRenderers(
                        renderingData.cullResults,
                        ref drawSettings,
                        ref m_FilteringSettings
                    );

                    // Make the texture available to shaders as _WaterFXMap
                    cmd.SetGlobalTexture(WaterFXMapID, m_WaterFX);
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public void Dispose()
            {
                m_WaterFX?.Release();
                m_WaterFX = null;
            }
        }

        #endregion

        #region Caustics Pass

        class WaterCausticsPass : ScriptableRenderPass
        {
            private const string k_RenderWaterCausticsTag = "Render Water Caustics";

            private readonly ProfilingSampler m_WaterCaustics_Profile =
                new ProfilingSampler(k_RenderWaterCausticsTag);

            public Material WaterCausticMaterial;

            private static Mesh m_mesh;

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                Camera cam = renderingData.cameraData.camera;

                if (cam.cameraType == CameraType.Preview || WaterCausticMaterial == null)
                    return;

                CommandBuffer cmd = CommandBufferPool.Get(k_RenderWaterCausticsTag);

                using (new ProfilingScope(cmd, m_WaterCaustics_Profile))
                {
                    Matrix4x4 sunMatrix = RenderSettings.sun != null
                        ? RenderSettings.sun.transform.localToWorldMatrix
                        : Matrix4x4.TRS(
                            Vector3.zero,
                            Quaternion.Euler(-45f, 45f, 0f),
                            Vector3.one
                        );

                    WaterCausticMaterial.SetMatrix("_MainLightDir", sunMatrix);

                    if (m_mesh == null)
                        m_mesh = GenerateCausticsMesh(1000f);

                    Vector3 position = cam.transform.position;
                    position.y = 0f;

                    Matrix4x4 matrix = Matrix4x4.TRS(
                        position,
                        Quaternion.identity,
                        Vector3.one
                    );

                    cmd.DrawMesh(m_mesh, matrix, WaterCausticMaterial, 0, 0);
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }

        #endregion

        private WaterFxPass m_WaterFxPass;
        private WaterCausticsPass m_CausticsPass;

        public WaterSystemSettings settings = new WaterSystemSettings();

        [HideInInspector][SerializeField] private Shader causticShader;
        [HideInInspector][SerializeField] private Texture2D causticTexture;

        private Material _causticMaterial;

        private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
        private static readonly int Size = Shader.PropertyToID("_Size");
        private static readonly int CausticTexture = Shader.PropertyToID("_CausticMap");

        public override void Create()
        {
            m_WaterFxPass = new WaterFxPass
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingOpaques
            };

            m_CausticsPass = new WaterCausticsPass();

            causticShader = causticShader != null
                ? causticShader
                : Shader.Find("Hidden/BoatAttack/Caustics");

            if (causticShader == null)
            {
                Debug.LogWarning("WaterSystemFeature: Caustic shader not found.");
                return;
            }

            if (_causticMaterial != null)
            {
                CoreUtils.Destroy(_causticMaterial);
            }

            _causticMaterial = CoreUtils.CreateEngineMaterial(causticShader);
            _causticMaterial.SetFloat("_BlendDistance", settings.causticBlendDistance);

            if (causticTexture == null)
            {
                Debug.Log("Caustics Texture missing, attempting to load.");

#if UNITY_EDITOR
                causticTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(
                    "Packages/com.verasl.water-system/Textures/WaterSurface_single.tif"
                );
#endif
            }

            _causticMaterial.SetTexture(CausticTexture, causticTexture);

            switch (settings.debug)
            {
                case WaterSystemSettings.DebugMode.Caustics:
                    _causticMaterial.SetFloat(SrcBlend, 1f);
                    _causticMaterial.SetFloat(DstBlend, 0f);
                    _causticMaterial.EnableKeyword("_DEBUG");
                    m_CausticsPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
                    break;

                case WaterSystemSettings.DebugMode.WaterEffects:
                    break;

                case WaterSystemSettings.DebugMode.Disabled:
                    _causticMaterial.SetFloat(SrcBlend, 2f);
                    _causticMaterial.SetFloat(DstBlend, 0f);
                    _causticMaterial.DisableKeyword("_DEBUG");
                    m_CausticsPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox + 1;
                    break;
            }

            _causticMaterial.SetFloat(Size, settings.causticScale);
            m_CausticsPass.WaterCausticMaterial = _causticMaterial;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (m_WaterFxPass != null)
                renderer.EnqueuePass(m_WaterFxPass);

            if (m_CausticsPass != null)
                renderer.EnqueuePass(m_CausticsPass);
        }

        protected override void Dispose(bool disposing)
        {
            m_WaterFxPass?.Dispose();

            if (_causticMaterial != null)
            {
                CoreUtils.Destroy(_causticMaterial);
                _causticMaterial = null;
            }
        }

        private static Mesh GenerateCausticsMesh(float size)
        {
            Mesh m = new Mesh();
            size *= 0.5f;

            Vector3[] verts =
            {
                new Vector3(-size, 0f, -size),
                new Vector3(size, 0f, -size),
                new Vector3(-size, 0f, size),
                new Vector3(size, 0f, size)
            };

            m.vertices = verts;

            int[] tris =
            {
                0, 2, 1,
                2, 3, 1
            };

            m.triangles = tris;
            m.RecalculateBounds();

            return m;
        }

        [System.Serializable]
        public class WaterSystemSettings
        {
            [Header("Caustics Settings")]
            [Range(0.1f, 1f)]
            public float causticScale = 0.25f;

            public float causticBlendDistance = 3f;

            [Header("Advanced Settings")]
            public DebugMode debug = DebugMode.Disabled;

            public enum DebugMode
            {
                Disabled,
                WaterEffects,
                Caustics
            }
        }
    }
}