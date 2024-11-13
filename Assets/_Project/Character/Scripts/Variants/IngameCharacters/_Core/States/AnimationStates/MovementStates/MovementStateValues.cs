using System;
using _Project.Maps.Climber.Objects;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Project.Characters.IngameCharacters.Core.MovementStates
{
    public static class ActionsStateParams
    {
        public static Bubble CurrentBubble { get; set; }

        public static void SetBubble(Bubble bubble)
        {
            CurrentBubble = bubble;
        }

        public static void ReleaseBubble()
        {
            CurrentBubble = null;
        }
    }
    
    public class MovementStateValues : MonoBehaviour
    {
        [SerializeField, TitleGroup("Velocity")] protected float gravityPow = 2;
        [SerializeField, TitleGroup("Velocity")] protected float accelerationFactor = 2;
        [SerializeField, TitleGroup("Velocity"), Range(0,1)] protected float stayHeightParameter = 1;
        [SerializeField, TitleGroup("Velocity"), Range(0,1)] protected float wallJumpSlowRate = 1;

        private MoveParams MoveParams;

        private void Awake()
        {
            MoveParams = GetComponentInParent<MoveParams>();
            OriginalGravityPow = gravityPow;
            OriginalAccelerationFactor = accelerationFactor;
        }

        private float OriginalGravityPow { get; set; }
        private float GravityPow => (MoveParams.IsWallJumping) ? OriginalGravityPow * wallJumpSlowRate : OriginalGravityPow;  
        public Vector3 GetGravity()
        {
            var yRevision = (Mathf.Pow(MoveParams.GravityTime + stayHeightParameter, GravityPow) * Mathf.Abs(Physics.gravity.y) * Time.deltaTime);
            // var yRevision = (Mathf.Pow(DownTime, 2) * gravitySpeed * pressingRevisio n) * Time.deltaTime;
            return Vector3.down * yRevision;
        }

        public float CurrentHeight { get; set; } // OnEnterState에서만 초기화 되어야함!!!!!!

        private float OriginalAccelerationFactor { get; set; }
        private float AccelerationFactor => (MoveParams.IsWallJumping) ? OriginalAccelerationFactor * wallJumpSlowRate : OriginalAccelerationFactor;
        public Vector3 GetGravity(float maxJumpHeight, float totalTime, out bool isFinished)
        {
            // 경과된 시간을 0에서 1로 정규화
            float t = Mathf.Clamp01(MoveParams.GravityTime / totalTime);
            // 비선형 가속도 적용 (t^2 또는 t^3 등 곡선 가속 적용)
            float nonLinearT = Mathf.Pow(t, AccelerationFactor);
            
            // 현재 높이를 계산 (Y축에서 시작 높이에서 0까지 비선형 가속으로 이동)
            float newHeight = Mathf.Lerp(maxJumpHeight, 0f, nonLinearT);
            

            // 이번 프레임에서 떨어진 거리 반환
            float fallDistance = CurrentHeight - newHeight;
            // Debug.Log(fallDistance);

            // 현재 높이를 업데이트
            CurrentHeight = newHeight;
            isFinished = CurrentHeight == 0;
            return Vector3.down * fallDistance;
        }
    }
}