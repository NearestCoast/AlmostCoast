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
        [SerializeField] private float fadeOutDuration = 0.3f; // 페이드 아웃 시간
        [SerializeField] private float fadeInDuration = 1;    // 페이드 인 시간
        [SerializeField] private float waitFadeIn = 1f;        // 대기 시간

        private CancellationTokenSource cts;
        private bool isApplicationQuitting = false;

        // 외부에서 Fade 상태를 확인할 수 있는 변수
        public bool IsFadingIn { get; private set; }
        public bool IsFadingOut { get; private set; }

        private void Awake()
        {
            fadeCanvasGroup = GetComponent<CanvasGroup>();
            cts = new CancellationTokenSource();
            fadeCanvasGroup.alpha = 1;
        }

        private void Start()
        {
            fadeCanvasGroup.alpha = 1;
            FadeIn().Forget();
        }

        public async UniTask FadeOut(CancellationToken cancellationToken = default)
        {
            float elapsed = 0f;
            IsFadingOut = true;

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
            finally
            {
                IsFadingOut = false;
            }
        }

        public async UniTask FadeIn(CancellationToken cancellationToken = default)
        {
            float elapsed = 0f;
            IsFadingIn = true;

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
            finally
            {
                IsFadingIn = false;
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
