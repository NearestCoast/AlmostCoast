using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Project.UI.InGame
{
    public class CurtainUI : MonoBehaviour
    {
        private CanvasGroup fadeCanvasGroup;
        [SerializeField] private float fadeOutDuration = 1.0f; // 페이드 아웃 시간
        [SerializeField] private float fadeInDuration = 3f;    // 페이드 인 시간
        [SerializeField] private float waitFadeIn = 2f;        // 대기 시간

        private CancellationTokenSource cts;
        private bool isApplicationQuitting = false;

        private void Awake()
        {
            fadeCanvasGroup = GetComponent<CanvasGroup>();
            cts = new CancellationTokenSource();
        }

        public async UniTask FadeOut(CancellationToken cancellationToken = default)
        {
            float elapsed = 0f;

            try
            {
                while (elapsed < fadeOutDuration)
                {
                    if (isApplicationQuitting) return; // 종료 중단 체크
                    cancellationToken.ThrowIfCancellationRequested();

                    elapsed += Time.deltaTime;
                    fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeOutDuration);
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
                fadeCanvasGroup.alpha = 1f;
            }
            catch (OperationCanceledException)
            {
                Debug.Log("FadeOut cancelled.");
            }
        }

        public async UniTask FadeIn(CancellationToken cancellationToken = default)
        {
            float elapsed = 0f;

            try
            {
                if (isApplicationQuitting) return; // 종료 중단 체크
                await UniTask.Delay(TimeSpan.FromSeconds(waitFadeIn), cancellationToken: cancellationToken);

                while (elapsed < fadeInDuration)
                {
                    if (isApplicationQuitting) return; // 종료 중단 체크
                    cancellationToken.ThrowIfCancellationRequested();

                    elapsed += Time.deltaTime;
                    fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeInDuration);
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
                fadeCanvasGroup.alpha = 0f;
            }
            catch (OperationCanceledException)
            {
                Debug.Log("FadeIn cancelled.");
            }
        }

        private void OnApplicationQuit()
        {
            isApplicationQuitting = true;

            // 모든 비동기 작업 강제 취소
            if (cts != null && !cts.IsCancellationRequested)
            {
                cts.Cancel();
            }

            // 알파 값을 즉시 0으로 설정
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 0;
                Debug.Log("Application quitting: Alpha set to 0.");
            }
        }

        private void OnDestroy()
        {
            cts?.Dispose();
        }
    }
}