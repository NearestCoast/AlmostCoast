using System;
using UnityEngine;

namespace _Project.Managers.Scripts._Core.AudioManager
{
    public class AudioManager : MonoBehaviour
    {
        private AudioSource AudioSource { get; set; }

        private void Awake()
        {
            AudioSource = GetComponent<AudioSource>();
        }

        public void SetClip(AudioClip clip)
        {
            AudioSource.clip = clip;
        }

        public void Play()
        {
            AudioSource.Play();
        }
    }
}