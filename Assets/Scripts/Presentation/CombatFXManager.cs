using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WarlordJanggi.Core;

namespace WarlordJanggi.Presentation
{
    /// <summary>
    /// Phase 3 전투 타격 VFX 총괄 매니저 (오브젝트 풀링 기반)
    /// - 2번째 콘셉트 아트의 '하늘에서 내리꽂히는 낙뢰(Lightning Strike)' 직격 연출
    /// - 폭발 파편 및 충격파 링(Shockwave)
    /// - 타격감 극대화를 위한 카메라 쉐이크 및 히트스톱(Hit Stop)
    /// </summary>
    public sealed class CombatFXManager : MonoBehaviour
    {
        private static CombatFXManager _instance;
        public static CombatFXManager Instance => _instance;

        [Header("이펙트 컬러")]
        [SerializeField] private Color choLightningColor = new Color(0.2f, 0.75f, 1f, 1f); // 청룡군 네온 청색 낙뢰
        [SerializeField] private Color hanLightningColor = new Color(1f, 0.3f, 0.2f, 1f);  // 홍마군 진홍색 낙뢰

        private readonly Queue<LineRenderer> _lightningPool = new Queue<LineRenderer>(8);
        private readonly Queue<ParticleSystem> _explosionPool = new Queue<ParticleSystem>(8);
        private Material _lightningMaterial;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            else Destroy(gameObject);

            InitMaterials();
            WarmUpPools();
        }

        private void InitMaterials()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            _lightningMaterial = new Material(shader)
            {
                color = Color.white
            };
        }

        private void WarmUpPools()
        {
            var poolRoot = new GameObject("[VFX_Pool]").transform;
            poolRoot.SetParent(transform);

            // 1. 낙뢰 LineRenderer 풀링
            for (int i = 0; i < 4; i++)
            {
                var lrObj = new GameObject($"LightningBolt_{i}");
                lrObj.transform.SetParent(poolRoot);
                var lr = lrObj.AddComponent<LineRenderer>();
                lr.material = _lightningMaterial;
                lr.startWidth = 0.15f;
                lr.endWidth = 0.05f;
                lr.useWorldSpace = true;
                lrObj.SetActive(false);
                _lightningPool.Enqueue(lr);
            }

            // 2. 폭발 및 파편 파티클 풀링
            for (int i = 0; i < 4; i++)
            {
                var psObj = CreateExplosionParticlePrefab($"Explosion_{i}", poolRoot);
                psObj.gameObject.SetActive(false);
                _explosionPool.Enqueue(psObj);
            }
        }

        private ParticleSystem CreateExplosionParticlePrefab(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.playOnAwake = false;
            main.duration = 0.6f;
            main.startLifetime = 0.45f;
            main.startSpeed = 6f;
            main.startSize = 0.18f;
            main.loop = false;
            main.stopAction = ParticleSystemStopAction.None;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 35) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.yellow, 0f), new GradientColorKey(Color.red, 0.5f), new GradientColorKey(Color.black, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = _lightningMaterial;

            return ps;
        }

        /// <summary>
        /// 2번째 콘셉트 아트 스타일의 낙뢰 직격 전투 타격 연출
        /// </summary>
        public void PlayCaptureStrike(Vector3 targetWorldPos, CampType attackerCamp, PieceType attackerType)
        {
            Color strikeColor = (attackerCamp == CampType.Cho) ? choLightningColor : hanLightningColor;
            StartCoroutine(CoLightningStrikeSequence(targetWorldPos, strikeColor));
        }

        private IEnumerator CoLightningStrikeSequence(Vector3 targetPos, Color strikeColor)
        {
            // 1. 낙뢰 소환 (하늘 높이 15m에서 타깃 지점으로 지그재그 내리꽂힘)
            LineRenderer bolt = GetLightning();
            bolt.startColor = Color.white;
            bolt.endColor = strikeColor;

            int segments = 8;
            bolt.positionCount = segments + 1;
            Vector3 skyPos = targetPos + new Vector3(0f, 14f, 0f);

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector3 pt = Vector3.Lerp(skyPos, targetPos, t);
                if (i > 0 && i < segments)
                {
                    // 지그재그 노이즈 오프셋
                    pt += new Vector3(Random.Range(-0.45f, 0.45f), 0f, Random.Range(-0.45f, 0.45f));
                }
                bolt.SetPosition(i, pt);
            }

            bolt.gameObject.SetActive(true);

            // 2. 타격 순간 히트스톱 & 폭발 파편 방출
            var explosion = GetExplosion();
            explosion.transform.position = targetPos + Vector3.up * 0.2f;
            explosion.gameObject.SetActive(true);
            explosion.Play();

            // 3. 카메라 쉐이크 호출
            if (CameraController.Instance != null)
            {
                CameraController.Instance.TriggerShake(0.35f, 0.35f);
            }

            // 번개 깜빡임 효과 (3프레임 동안 잔상)
            yield return new WaitForSeconds(0.06f);
            bolt.startWidth = 0.08f;
            bolt.endWidth = 0.02f;
            yield return new WaitForSeconds(0.06f);

            ReturnLightning(bolt);

            yield return new WaitForSeconds(0.4f);
            ReturnExplosion(explosion);
        }

        private LineRenderer GetLightning()
        {
            if (_lightningPool.Count > 0) return _lightningPool.Dequeue();
            var lrObj = new GameObject("LightningBolt_Extra");
            lrObj.transform.SetParent(transform);
            var lr = lrObj.AddComponent<LineRenderer>();
            lr.material = _lightningMaterial;
            lr.useWorldSpace = true;
            return lr;
        }

        private void ReturnLightning(LineRenderer lr)
        {
            lr.gameObject.SetActive(false);
            lr.startWidth = 0.15f;
            lr.endWidth = 0.05f;
            _lightningPool.Enqueue(lr);
        }

        private ParticleSystem GetExplosion()
        {
            if (_explosionPool.Count > 0) return _explosionPool.Dequeue();
            return CreateExplosionParticlePrefab("Explosion_Extra", transform);
        }

        private void ReturnExplosion(ParticleSystem ps)
        {
            ps.gameObject.SetActive(false);
            _explosionPool.Enqueue(ps);
        }
    }
}
