using System;
using System.Collections.Generic;
using _Project.Combat.HitObjects;
using _Project.Effect;
using _Project.Utils;
using Animancer;
using Animancer.FSM;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.VFX;

namespace _Project.Characters._Core.States
{
    [Serializable]
    public class StateEvent
    {
        private GameObject stateBehaviour; // AttackState 참조를 저장합니다.

        public void Initialize(GameObject state)
        {
            stateBehaviour = state;
            Push();
        }
        
        public AnimancerEvent.Sequence InitializeEvents()
        {
            var events = new AnimancerEvent.Sequence();

            // VFX 이벤트 등록
            for (int i = 0; i < normalizedTimesToPlayVFX.Length && i < VFXs.Length; i++)
            {
                float normalizedTime = normalizedTimesToPlayVFX[i];
                VisualEffect vfx = VFXs[i];
                if (vfx != null)
                {
                    events.Add(normalizedTime, () =>
                    {
                        vfx.gameObject.SetActive(true);
                        vfx.Play();
                    });
                }
            }

            // SFX 이벤트 등록
            for (int i = 0; i < normalizedTimesToPlaySFX.Length && i < SFXs.Length; i++)
            {
                float normalizedTime = normalizedTimesToPlaySFX[i];
                AudioSource sfx = SFXs[i];
                if (sfx != null)
                {
                    events.Add(normalizedTime, () => sfx.Play());
                }
            }

            // Hit 이벤트 등록
            for (int i = 0; i < normalizedTimesToPlayHit.Length && i < hitObjects.Length; i++)
            {
                float normalizedTime = normalizedTimesToPlayHit[i];
                HitObject hitObj = hitObjects[i];
                if (hitObj != null)
                {
                    events.Add(normalizedTime, () => hitObj.Invoke());
                }
            }

            return events;
        }

        [BoxGroup("Normalized Timings")]
        [VerticalGroup("Normalized Timings/VerticalLayout")]

        // VFX 박스
        [BoxGroup("Normalized Timings/VerticalLayout/VFX Settings")]
        [HorizontalGroup("Normalized Timings/VerticalLayout/VFX Settings/InnerHorizontal")]
        [LabelText("Normalized Times to VFX")]
        [SerializeField, ListDrawerSettings(Expanded = true)]
        private float[] normalizedTimesToPlayVFX = new[] { 0f };

        [HorizontalGroup("Normalized Timings/VerticalLayout/VFX Settings/InnerHorizontal")]
        [LabelText("VFXs")]
        [SerializeField, ListDrawerSettings(Expanded = true)]
        private VisualEffect[] VFXs = new VisualEffect[0];

        // SFX 박스
        [BoxGroup("Normalized Timings/VerticalLayout/SFX Settings")]
        [HorizontalGroup("Normalized Timings/VerticalLayout/SFX Settings/InnerHorizontal")]
        [LabelText("Normalized Times to SFX")]
        [SerializeField, ListDrawerSettings(Expanded = true)]
        private float[] normalizedTimesToPlaySFX = new[] { 0f };

        [HorizontalGroup("Normalized Timings/VerticalLayout/SFX Settings/InnerHorizontal")]
        [LabelText("SFXs")]
        [SerializeField, ListDrawerSettings(Expanded = true)]
        private AudioSource[] SFXs = new AudioSource[0];

        // Hit 박스
        [BoxGroup("Normalized Timings/VerticalLayout/Hit Settings")]
        [HorizontalGroup("Normalized Timings/VerticalLayout/Hit Settings/InnerHorizontal")]
        [LabelText("Normalized Times to Hit")]
        [SerializeField, ListDrawerSettings(Expanded = true)]
        private float[] normalizedTimesToPlayHit = new[] { 0f };

        [HorizontalGroup("Normalized Timings/VerticalLayout/Hit Settings/InnerHorizontal")]
        [LabelText("Hit Objects")]
        [SerializeField, ListDrawerSettings(Expanded = true)]
        private HitObject[] hitObjects = new HitObject[0];

        [PropertySpace(SpaceBefore = 10)]
        [HorizontalGroup("ButtonGroup")]
        [Button("Push"), GUIColor(0f, 0.5f, 1f)]
        private void Push()
        {
            if (stateBehaviour == null)
            {
                Debug.LogWarning("AttackState 참조가 없습니다.");
                return;
            }

            // GetComponentsInChildren을 사용하여 자식 오브젝트에서 컴포넌트들을 찾습니다.
            var visualEffectHolder = stateBehaviour.GetComponentInDirectChildren<VisualEffectHolder>();
            if (visualEffectHolder) VFXs = visualEffectHolder.GetComponentsInChildren<VisualEffect>(true);
            
            var soundEffectHolder = stateBehaviour.GetComponentInDirectChildren<SoundEffectHolder>();
            if (soundEffectHolder) SFXs = soundEffectHolder.GetComponentsInChildren<AudioSource>(true);
            
            var hitObjectHolder = stateBehaviour.GetComponentInDirectChildren<HitObjectHolder>();
            if (hitObjectHolder) hitObjects = hitObjectHolder.GetComponentsInChildren<HitObject>(true);
        }

        [PropertySpace(SpaceBefore = 10)]
        [HorizontalGroup("ButtonGroup")]
        [Button("Synchronize"), GUIColor(1f, 0f, 0f)]
        private void Synchronize()
        {
            // normalizedTimesToPlayVFX 배열의 길이에 맞춰 다른 배열의 길이를 조정합니다.
            normalizedTimesToPlaySFX = new float[normalizedTimesToPlayVFX.Length];
            normalizedTimesToPlayHit = new float[normalizedTimesToPlayVFX.Length];
            VFXs = EnsureArrayLength(VFXs, normalizedTimesToPlayVFX.Length);
            SFXs = EnsureArrayLength(SFXs, normalizedTimesToPlayVFX.Length);
            hitObjects = EnsureArrayLength(hitObjects, normalizedTimesToPlayVFX.Length);

            // 값을 복사합니다.
            for (int i = 0; i < normalizedTimesToPlayVFX.Length; i++)
            {
                normalizedTimesToPlaySFX[i] = normalizedTimesToPlayVFX[i];
                normalizedTimesToPlayHit[i] = normalizedTimesToPlayVFX[i];
            }
        }

        // 배열의 길이를 보장하고 필요하면 null로 채웁니다.
        private T[] EnsureArrayLength<T>(T[] array, int length)
        {
            if (array == null || array.Length != length)
            {
                T[] newArray = new T[length];
                if (array != null)
                {
                    Array.Copy(array, newArray, Mathf.Min(array.Length, length));
                }

                return newArray;
            }

            return array;
        }
    }
}