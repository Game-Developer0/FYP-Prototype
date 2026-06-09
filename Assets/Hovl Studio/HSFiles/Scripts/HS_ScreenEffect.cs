using UnityEngine;

namespace Hovl
{
    [ExecuteAlways]
    [DefaultExecutionOrder(10000)]
    public class HS_ScreenEffect : MonoBehaviour
    {
        public ParticleSystem screenEffect;
        public Camera sourceCamera;

        [Header("Distance From Camera")]
        public float fallbackDistance = 0.5f;
        public float extraDistanceFromNearClip = 0.2f;

        [Header("Screen Size")]
        public float screenCoverageMultiplier = 1.6f;

        [Header("Start Setup")]
        public bool snapOnStart = true;
        public bool parentToCameraOnStart = true;

        [Header("Screen Lock")]
        public bool lockToCameraEveryFrame = true;
        public bool forceLocalSimulationSpace = true;

        [Header("Play Settings")]
        public bool clearOnStop = true;
        public float warmupTime = 0.05f;

        private ParticleSystem[] allParticles;

        void Reset()
        {
            if (sourceCamera == null)
                sourceCamera = Camera.main;

            if (screenEffect == null)
                screenEffect = GetComponentInChildren<ParticleSystem>(true);
        }

        void Awake()
        {
            CacheParticles();
            ForceScreenParticleSettings();
        }

        void OnEnable()
        {
            if (sourceCamera == null)
                sourceCamera = Camera.main;

            if (screenEffect == null)
                screenEffect = GetComponentInChildren<ParticleSystem>(true);

            CacheParticles();
            ForceScreenParticleSettings();

            if (Application.isPlaying && snapOnStart)
            {
                SnapToCamera();
            }

            UpdateSize();
        }

        void Start()
        {
            if (!Application.isPlaying)
                return;

            if (snapOnStart)
            {
                SnapToCamera();
            }

            UpdateSize();
            StopEffect();
        }

        void LateUpdate()
        {
            if (sourceCamera == null)
                sourceCamera = Camera.main;

            if (Application.isPlaying && lockToCameraEveryFrame)
            {
                SnapToCamera();
            }

            UpdateSize();
        }

        void OnValidate()
        {
            if (sourceCamera == null)
                sourceCamera = Camera.main;

            if (screenEffect == null)
                screenEffect = GetComponentInChildren<ParticleSystem>(true);

            CacheParticles();
            ForceScreenParticleSettings();
            UpdateSize();
        }

        void CacheParticles()
        {
            allParticles = GetComponentsInChildren<ParticleSystem>(true);
        }

        void ForceScreenParticleSettings()
        {
            if (allParticles == null)
                CacheParticles();

            if (allParticles == null) return;

            foreach (ParticleSystem particle in allParticles)
            {
                if (particle == null) continue;

                var main = particle.main;

                if (forceLocalSimulationSpace)
                {
                    main.simulationSpace = ParticleSystemSimulationSpace.Local;
                }

                main.scalingMode = ParticleSystemScalingMode.Local;
            }
        }

        void SnapToCamera()
        {
            Camera cam = sourceCamera != null ? sourceCamera : Camera.main;
            if (cam == null) return;

            float safeDistance = GetSafeDistance(cam);

            if (parentToCameraOnStart)
            {
                if (transform.parent != cam.transform)
                {
                    transform.SetParent(cam.transform, false);
                }

                transform.localPosition = Vector3.forward * safeDistance;
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
            }
            else
            {
                transform.position = cam.transform.position + cam.transform.forward * safeDistance;
                transform.rotation = cam.transform.rotation;
            }
        }

        float GetSafeDistance(Camera cam)
        {
            float safeDistance = cam.nearClipPlane + extraDistanceFromNearClip;

            if (fallbackDistance > safeDistance)
            {
                safeDistance = fallbackDistance;
            }

            return safeDistance;
        }

        public void PlayEffect()
        {
            gameObject.SetActive(true);

            if (sourceCamera == null)
                sourceCamera = Camera.main;

            CacheParticles();
            ForceScreenParticleSettings();
            SnapToCamera();
            UpdateSize();

            if (allParticles == null || allParticles.Length == 0)
                return;

            foreach (ParticleSystem particle in allParticles)
            {
                if (particle == null) continue;

                particle.gameObject.SetActive(true);

                var emission = particle.emission;
                emission.enabled = true;

                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particle.Clear(true);

                particle.Simulate(0f, true, true, true);
                particle.Play(true);

                if (warmupTime > 0f)
                {
                    particle.Simulate(warmupTime, true, false, true);
                }
            }
        }

        public void StopEffect()
        {
            CacheParticles();

            if (allParticles == null || allParticles.Length == 0)
                return;

            foreach (ParticleSystem particle in allParticles)
            {
                if (particle == null) continue;

                var emission = particle.emission;
                emission.enabled = false;

                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                if (clearOnStop)
                {
                    particle.Clear(true);
                }
            }
        }

        void UpdateSize()
        {
            Camera cam = sourceCamera != null ? sourceCamera : Camera.main;
            if (cam == null) return;

            if (screenEffect == null)
                screenEffect = GetComponentInChildren<ParticleSystem>(true);

            if (screenEffect == null) return;

            float dist = cam.transform.InverseTransformPoint(transform.position).z;

            if (dist <= cam.nearClipPlane)
            {
                dist = GetSafeDistance(cam);
            }

            float height;

            if (cam.orthographic)
            {
                height = 2f * cam.orthographicSize;
            }
            else
            {
                float fovRad = cam.fieldOfView * Mathf.Deg2Rad;
                height = 2f * dist * Mathf.Tan(fovRad * 0.5f);
            }

            float width = height * cam.aspect;

            width *= screenCoverageMultiplier;
            height *= screenCoverageMultiplier;

            ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>(true);

            foreach (ParticleSystem particle in particles)
            {
                if (particle == null) continue;

                var main = particle.main;
                main.startSize3D = true;
                main.startSizeX = new ParticleSystem.MinMaxCurve(width);
                main.startSizeY = new ParticleSystem.MinMaxCurve(height);
                main.startSizeZ = new ParticleSystem.MinMaxCurve(1f);

                var shape = particle.shape;
                shape.scale = new Vector3(width, height, 1f);
            }
        }
    }
}