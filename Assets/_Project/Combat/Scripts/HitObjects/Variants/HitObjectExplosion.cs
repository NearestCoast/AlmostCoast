using System.Collections.Generic;
using _Project._Core;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.VFX;

namespace _Project.Combat.HitObjects
{
    public class HitObjectExplosion : HitObject
    {
        private enum HeightOrigin
        {
            Bottom,
            Center,
            Top
        }

        private void Awake()
        {
            CenterHeight = CalculateCenterOffset();
            hitSoundEffect = GetComponent<AudioSource>();

            return;
            float CalculateCenterOffset()
            {
                float offset = heightOrigin switch
                {
                    HeightOrigin.Bottom => 0f,
                    HeightOrigin.Center => 1,
                    HeightOrigin.Top => 2,
                    _=> 0,
                };

                return offset + heightRevision;
            }
        }

        public override void Invoke()
        {
            base.Invoke();
            PerformMeleeAttack(transform.position + Vector3.up * CenterHeight);
        }

        [SerializeField] private int damage = 10;
        [SerializeField] private float attackRange = 2.0f; // 공격 범위
        [SerializeField] private float sphereRadius = 0.5f;
        [SerializeField] private float horizontalAngle = 90f; // 수평 각도
        [SerializeField] private float verticalAngle; // 수직 각도
        [SerializeField] private int angleSteps = 10; // 체크할 각도 스텝
        [SerializeField] private HeightOrigin heightOrigin = HeightOrigin.Center; // 높이 기준, 기본은 Center
        [SerializeField] private float heightRevision;

        [PropertySpace(10)]
        [SerializeField] private LayerMask targetLayer; // 타겟이 속한 레이어
        [SerializeField] private GameObject hitEffectPrefab; // 검이 부딪힐 때 나올 이펙트 프리팹
        private AudioSource hitSoundEffect;
        [SerializeField] private bool allowMultiHit; // 멀티 히트 허용 여부

        private float AttackRange => attackRange;
        private float SphereRadius => sphereRadius;
        private float CenterHeight { get; set; } // 높이 오프셋 값만 저장

        private readonly HashSet<GameObject> hitTargets = new HashSet<GameObject>(); // 히트된 대상을 저장할 집합
        private Collider[] hitColliders;
        private void PerformMeleeAttack(Vector3 attackOrigin)
        {
            Vector3 baseDirection = transform.forward;
            Vector3 originWithCenterHeight = attackOrigin;

            hitTargets.Clear(); // 히트된 대상 추적을 초기화

            for (int i = 0; i <= angleSteps; i++)
            {
                float horizontalStep = Mathf.Lerp(horizontalAngle / 2, -horizontalAngle / 2, (float)i / angleSteps);
                float verticalStep = Mathf.Lerp(verticalAngle / 2, -verticalAngle / 2, (float)i / angleSteps);

                // 수평 및 수직 각도를 조합하여 회전 설정
                Quaternion rotation = Quaternion.AngleAxis(horizontalStep, Vector3.up) * Quaternion.AngleAxis(verticalStep, transform.right);
                Vector3 attackDirectionVector = rotation * baseDirection;

                var jLength = (int)(AttackRange / SphereRadius);
                for (var j = jLength; j > 0; j--)
                {
                    var dirLength = AttackRange - j * SphereRadius;
                    Physics.OverlapSphereNonAlloc(originWithCenterHeight + attackDirectionVector * dirLength, SphereRadius, hitColliders, targetLayer);

                    foreach (var hitCollider in hitColliders)
                    {
                        GameObject hitObject = hitCollider.gameObject;

                        // 동일한 대상에 한 번만 히트 적용
                        if (hitTargets.Contains(hitObject)) continue;
                        
                        Vector3 hitPoint = hitCollider.ClosestPoint(originWithCenterHeight + attackDirectionVector * dirLength);
                        if (hitEffectPrefab)
                        {
                            var fx = Instantiate(hitEffectPrefab, hitPoint, transform.rotation);
                            var vfx = fx.GetComponent<VisualEffect>();
                            fx.gameObject.SetActive(true);
                            if (vfx) vfx.Play();
                        }
                        PlaySound();

                        var damageReceiver = hitCollider.GetComponent<IDamageReceiver>();
                        if (damageReceiver != null)
                        {
                            damageReceiver.TakeDamage(new HittingInfo(this, hitPoint), damage, sideEffect);
                        }

                        // 히트된 대상 기록
                        hitTargets.Add(hitObject);

                        // 멀티 히트를 허용하지 않으면 리턴하여 한 번만 히트하도록 함
                        if (!allowMultiHit) return;
                    }
                }
            }

            return;
            void PlaySound()
            {
                hitSoundEffect?.Play();
                // await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
                // hitSoundEffect?.Stop();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position + Vector3.up * CenterHeight;
            Vector3 baseDirection = transform.forward;

            for (int i = 0; i <= angleSteps; i++)
            {
                float hue = Mathf.Lerp(0, 0.8f, (float)i / angleSteps);
                Gizmos.color = Color.HSVToRGB(hue, 1, 1);

                float horizontalStep = Mathf.Lerp(horizontalAngle / 2, -horizontalAngle / 2, (float)i / angleSteps);
                float verticalStep = Mathf.Lerp(verticalAngle / 2, -verticalAngle / 2, (float)i / angleSteps);

                // 수평 및 수직 각도를 조합하여 회전 설정
                Quaternion rotation = Quaternion.AngleAxis(horizontalStep, Vector3.up) * Quaternion.AngleAxis(verticalStep, transform.right);
                Vector3 direction = rotation * baseDirection;

                var jLength = (int)(AttackRange / SphereRadius);
                for (var j = jLength; j > 0; j--)
                {
                    float distance = AttackRange - j * SphereRadius;
                    Vector3 position = origin + direction * distance;
                    Gizmos.DrawWireSphere(position, SphereRadius);
                }
            }
        }
    }
}
