using UnityEngine;

namespace Hovl
{
    [ExecuteAlways]
    public class HS_ScreenEffect : MonoBehaviour
    {
        public ParticleSystem screenEffect;
        public Camera sourceCamera;

        [Header("Distance From Camera")]
        public float fallbackDistance = 0.5f;
        public float extraDistanceFromNearClip = 0.2f;

        [Header("Start Setup")]
        public bool snapOnStart = true;
        public bool parentToCameraOnStart = true;

        [Header("Play Settings")]
        public bool clearOnStop = true;
        public float warmupTime = 0.05f;

        private ParticleSystem[] allParticles;

        void Reset()
        {
            if (sourceCamera == null)
                sourceCamera = Camera.main;

            if (screenEffect == null)
                screenEffect = GetComponentInChildren<ParticleSystem>();
        }

        void Awake()
        {
            CacheParticles();
        }

        void OnEnable()
        {
            if (sourceCamera == null)
                sourceCamera = Camera.main;

            if (screenEffect == null)
                screenEffect = GetComponentInChildren<ParticleSystem>();

            CacheParticles();
            UpdateSize();
        }

        void Start()
        {
            if (!Application.isPlaying)
                return;

            if (!snapOnStart)
                return;

            Camera cam = sourceCamera != null ? sourceCamera : Camera.main;
            if (cam == null)
                return;

            float safeDistance = GetSafeDistance(cam);

            transform.position = cam.transform.position + cam.transform.forward * safeDistance;

            if (parentToCameraOnStart)
            {
                transform.SetParent(cam.transform, true);
                transform.localPosition = Vector3.forward * safeDistance;
                transform.localRotation = Quaternion.identity;
            }

            UpdateSize();
            StopEffect();
        }

        void LateUpdate()
        {
            Camera cam = sourceCamera != null ? sourceCamera : Camera.main;

            if (Application.isPlaying && cam != null && parentToCameraOnStart)
            {
                float safeDistance = GetSafeDistance(cam);
                transform.localPosition = Vector3.forward * safeDistance;
                transform.localRotation = Quaternion.identity;
            }

            UpdateSize();
        }

        void OnValidate()
        {
            if (sourceCamera == null)
                sourceCamera = Camera.main;

            if (screenEffect == null)
                screenEffect = GetComponentInChildren<ParticleSystem>();

            CacheParticles();
            UpdateSize();
        }

        void CacheParticles()
        {
            allParticles = GetComponentsInChildren<ParticleSystem>(true);
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

            // Do not disable this GameObject.
            // It must stay active so HS_ScreenEffect keeps following the camera.
        }

        void UpdateSize()
        {
            if (screenEffect == null)
                return;

            Camera cam = sourceCamera != null ? sourceCamera : Camera.main;
            if (cam == null)
                return;

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

            var main = screenEffect.main;
            main.startSize3D = true;
            main.startSizeX = new ParticleSystem.MinMaxCurve(width);
            main.startSizeY = new ParticleSystem.MinMaxCurve(height);
            main.startSizeZ = new ParticleSystem.MinMaxCurve(1f);

            var shape = screenEffect.shape;
            shape.scale = new Vector3(width, height, 1f);
        }
    }
}