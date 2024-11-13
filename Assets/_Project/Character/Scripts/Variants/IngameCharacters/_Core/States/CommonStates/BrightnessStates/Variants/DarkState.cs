using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace _Project.Characters.IngameCharacters.Core.States.CommonStates.BrightnessState
{
    public class DarkState : BrightnessState
    {
        public override StateType Type => StateType.Dark;

        [SerializeField] private float darknessThreshold = 1;
        [SerializeField] private float eliminationThreshold = 3;

        public bool IsEliminationPhase => StateTime > darknessThreshold + eliminationThreshold;

        public override bool CanExitState => NextState.Type != StateType.Dark;

        public override void OnExitState()
        {
            base.OnExitState();
            CancelPlaySound();
            StartDissolve(1);

            void CancelPlaySound()
            {
                // 재생 취소
                cancellationTokenSource?.Cancel();
            }
            
            void StartDissolve(float duration)
            {
                // 기존에 진행 중인 트윈이 있다면 중지
                audioSource.DOKill();

                // 오디오 볼륨을 서서히 줄이고, 완료 후 오디오 정지
                audioSource.DOFade(0f, duration).OnComplete(() =>
                {
                    audioSource.Stop();
                    audioSource.volume = 1f; // 볼륨을 원래 값으로 복원
                });
            }
        }

        private CancellationTokenSource cancellationTokenSource;

        protected override void PlaySound()
        {
            // 기존의 토큰이 있다면 취소
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();

            PlaySoundAsync(cancellationTokenSource.Token).Forget();
        }

        private async UniTaskVoid PlaySoundAsync(CancellationToken cancellationToken)
        {
            try
            {
                // 3초 대기, 취소 가능
                await UniTask.Delay(TimeSpan.FromSeconds(darknessThreshold), cancellationToken: cancellationToken);
        
                // 오디오 재생
                StartFadeIn(1);
            }
            catch (OperationCanceledException)
            {
                // 작업이 취소된 경우 처리
                // Debug.Log("PlaySound 작업이 취소되었습니다.");
            }
            
            void StartFadeIn(float duration)
            {
                // 기존에 진행 중인 트윈이 있다면 중지
                audioSource.DOKill();

                // 오디오 볼륨을 0으로 설정하고 재생 시작
                audioSource.volume = 0f;
                audioSource.Play();

                // 오디오 볼륨을 서서히 증가
                audioSource.DOFade(1f, duration);
            }
        }
    }
}