using UnityEngine;
using Cysharp.Threading.Tasks;

namespace _Project
{

    public class BGMManager : MonoBehaviour
    {
        public AudioSource audioSource;  // AudioSource 컴포넌트
        public AudioClip[] bgmClips;     // 재생할 배경음악 리스트
        private int currentTrackIndex = 0; // 현재 재생 중인 트랙 인덱스

        private bool isPlaying = false;  // 재생 상태 확인

        void Start()
        {
            if (bgmClips.Length > 0)
            {
                PlayBGMSequentially().Forget(); // 비동기 실행
            }
        }

        private async UniTask PlayBGMSequentially()
        {
            isPlaying = true;

            while (isPlaying)
            {
                // 현재 트랙 재생
                audioSource.clip = bgmClips[currentTrackIndex];
                audioSource.Play();

                // 클립 길이만큼 대기
                await UniTask.Delay((int)(audioSource.clip.length * 1000));

                // 다음 트랙으로 이동 (순환)
                currentTrackIndex = (currentTrackIndex + 1) % bgmClips.Length;
            }
        }

        public void StopBGM()
        {
            isPlaying = false;
            audioSource.Stop();
        }
    }


}