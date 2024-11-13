using _Project.Characters.IngameCharacters.Core;
using Animancer;
using UnityEngine;

namespace _Project.Characters._Core.States
{
    public class FootstepEvents : MonoBehaviour
    {
        [SerializeField] private AnimancerComponent _Animancer;
        [SerializeField] private StringAsset _EventName;
        [SerializeField, Range(0, 1)] private float _PitchRandomization = 0.2f;
        [SerializeField] private AudioClip[] _Sounds;
        [SerializeField] private float volume = 1;

        private MoveParams moveParams;

        protected virtual void Awake()
        {
            moveParams = GetComponentInParent<MoveParams>();
            
            _Animancer.Events.AddTo<AudioSource>(_EventName, PlaySound);
        }
        
        private void PlaySound(AudioSource source)
        {
            source.clip = _Sounds[Random.Range(0, _Sounds.Length)];
            source.volume = moveParams.IsStealthMove ? 0.5f : 1 * volume;
            source.pitch = Random.Range(1 - _PitchRandomization, 1 + _PitchRandomization);
            source.Play();
        
            // GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            // Transform transform = sphere.transform;
            // transform.parent = source.transform;
            // transform.localPosition = Vector3.zero;
            // transform.localScale = Vector3.one * 0.2f;
            // Destroy(sphere, 0.1f);
        }
    }
}