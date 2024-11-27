using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace _Project
{
    public class BGMManager : MonoBehaviour
    {
        private AudioSource audioSource;  // AudioSource 컴포넌트
        public AudioClip[] bgmClips;      // 재생할 배경음악 리스트
        private int currentTrackIndex = 0; // 현재 재생 중인 트랙 인덱스

        private bool isPlaying = false;   // 재생 상태 확인
        private CancellationTokenSource cts; // CancellationTokenSource

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            cts = new CancellationTokenSource();
        }

        private void Start()
        {
            if (bgmClips.Length > 0)
            {
                PlayBGMSequentially(cts.Token).Forget(); // 비동기 실행
            }
        }

        private async UniTask PlayBGMSequentially(CancellationToken token)
        {
            isPlaying = true;

            while (isPlaying)
            {
                // 토큰 취소 확인
                token.ThrowIfCancellationRequested();

                // 현재 트랙 재생
                audioSource.clip = bgmClips[currentTrackIndex];
                audioSource.Play();

                try
                {
                    // 클립 길이만큼 대기, 도중에 취소 가능
                    await UniTask.Delay((int)(audioSource.clip.length * 1000), cancellationToken: token);
                }
                catch (OperationCanceledException)
                {
                    // 토큰이 취소된 경우 안전하게 종료
                    Debug.Log("BGM playback canceled.");
                    return;
                }

                // 다음 트랙으로 이동 (순환)
                currentTrackIndex = (currentTrackIndex + 1) % bgmClips.Length;
            }
        }

        public void StopBGM()
        {
            isPlaying = false;
            audioSource.Stop();
        }

        private void OnDestroy()
        {
            if (cts != null && !cts.IsCancellationRequested)
            {
                cts.Cancel(); // 모든 비동기 작업 취소
                cts.Dispose();
            }
        }
    }
}
