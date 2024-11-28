using System;
using System.Threading;
using _Project.Managers.Scripts._Core.SaveManager;
using BehaviorDesigner.Runtime.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Project.Characters.IngameCharacters.Core
{
    public class MoveParams : MonoBehaviour, ISavable
    {
        private VerticalParams VerticalParams;
        private GroundParams GroundParams;
        private void Awake()
        {
            VerticalParams = GetComponent<VerticalParams>();
            GroundParams = GetComponent<GroundParams>();
        }
        public  Vector3 Gravity { get; set; }
        public  float GravityTime { get; set; }

        public  bool IsJumpButtonPerforming { get; private set; }
        public  int JumpCount { get; private set; }
        public  int WallJumpCount { get; private set; }
        public  int ClimbJumpCount { get; private set; }
        public  int KickCount { get; private set; }
        public  int SkillJumpCount { get; private set; }
        // public static bool IsJumpable => JumpCount > 0 && !IsJumpButtonPerforming && GroundParams.IsGrounded;
        public bool IsJumpable => JumpCount > 0;
        public bool IsWallJumpable => !GroundParams.IsGrounded && VerticalParams.IsWalled && WallJumpCount > 0;
        public bool IsClimbJumpable => IsWallJumpable && ClimbJumpCount > 0;

        public  bool IsJumping { get; private set; }
        public  bool IsLeaping { get; private set; }
        public  bool IsWallJumping { get; private set; }
        public  bool IsHeadJumping { get; private set; }
        
        public  bool IsSkillJumpUpEnded { get; set; }
        public  bool IsSkillJumpUpInterruptible { get; set; }
        public  float SkillJumpUpHeight { get; set; }
        
        public  bool IsClimbButtonPressed { get; private set; }
        public  bool IsClimbing { get; private set; }
        public  bool IsClimbable => ClimbStaminaTime > 0;
        public  float ClimbStaminaTime { get; private set; }
        
        public  bool IsJustLedgeMoveEnded { get; private set; }
        
        public  bool IsGroundPoundingEnded { get; private set; }
        
        public bool IsJustRollEnded { get; private set; }
        
        public bool IsUnderCrowdControl { get; private set; }
        
        public  Vector3 Acceleration { get; set; }
        
        public  bool HasMovingPlatform { get; set; }

        public bool IsStealthMove { get; private set; }
        [SerializeField] private float stealthMoveVelocity = 4;
        public float StealthMoveVelocity => stealthMoveVelocity;
        [SerializeField] private bool isSlideJumpPossible;
        public bool IsSlideJumpPossible => isSlideJumpPossible;

        public  Vector3 GetGroundProjectedDirection(Vector3 forward)
        {
            var groundNormal = GroundParams.GroundNormal;
            var projectedDirection = Vector3.ProjectOnPlane(forward, groundNormal);
            projectedDirection.Normalize();

            return projectedDirection;
        }
        
        public  float CalculateJumpForce(float maxJumpHeight)
        {
            // return Mathf.Pow((3 * (maxJumpHeight * 1.001f) * Mathf.Sqrt(-Physics.gravity.y)) / (2 * Mathf.Sqrt(2)), 2f / 3f);
            return Mathf.Pow((3 * maxJumpHeight * Mathf.Sqrt(-Physics.gravity.y)) / (2 * Mathf.Sqrt(2)), 2f / 3f);
        }

        public void ResetAcceleration() => Acceleration = Vector3.zero;

        public void StartJumping()
        {
            IsJumping = true;
            StartLeaping();
        }

        public void EndJumping()
        {
            IsJumping = false;
            EndLeaping();
        }

        public void StartLeaping() => IsLeaping = true;
        public void EndLeaping() => IsLeaping = false;

        public void SetIsWallJumping() => IsWallJumping = true;
        public void ResetWallJumping() => IsWallJumping = false;

        public void SetIsHeadJumping() => IsHeadJumping = true;
        public void ResetHeadJumping() => IsHeadJumping = false;

        public void ResetJumpCount() => JumpCount = 1;
        public void DecreaseJumpCount() => JumpCount -= 1;

        [SerializeField] private int maxWallJumpCount = 0;
        public int MaxWallJumpCount
        {
            get => maxWallJumpCount;
            set => maxWallJumpCount = value;
        }
        
        public  void ResetWallJumpCount() => WallJumpCount = maxWallJumpCount;
        public  void DecreaseWallJumpCount() => WallJumpCount -= 1;
        
        public  void ResetClimbJumpCount() => ClimbJumpCount = 1;
        public  void DecreaseClimbJumpCount() => ClimbJumpCount -= 1;

        public  void ResetKickCount() => KickCount = 1;
        public  void DecreaseKickCount() => KickCount -= 1;

        public  void ResetSkillJumpCount() => SkillJumpCount = 1;
        public  void DecreaseSkillJumpCount() => SkillJumpCount -= 1;

        public  void StartClimbing() => IsClimbing = true;
        public  void EndClimbing() => IsClimbing = false;
        public void SetClimbingButtonPressed() => IsClimbButtonPressed = true;
        public void ResetClimbingButtonPressed() => IsClimbButtonPressed = false;
        public void SetIsJumpButtonPerforming() => IsJumpButtonPerforming = true;
        public void ResetIsJumpButtonPerforming() => IsJumpButtonPerforming = false;

        [SerializeField] private float climbStaminaTime = 4;
        public  void ResetClimbStamina() => ClimbStaminaTime = climbStaminaTime;
        public  void DecreaseClimbStaminaPerFrame() => ClimbStaminaTime -= Time.deltaTime;
        public  void DecreaseClimbStaminaAmount(float amount) => ClimbStaminaTime -= amount;
        public  void DecreaseClimbStaminaPerJump() => ClimbStaminaTime -= 1;

        private static float IsJustLedgeMoveEndedTime { get; set; }

        public  void SetLedgeMove()
        {
            IsJustLedgeMoveEnded = true;
            IsJustLedgeMoveEndedTime = 0;
        }
        public  void ResetIsJustLedgeMoveEnded() // PlayerCharacter에서 한번씩만 실행되어야한다.
        {
            if (IsJustLedgeMoveEndedTime < 1)
            {
                IsJustLedgeMoveEndedTime += Time.deltaTime;
            }
            else
            {
                IsJustLedgeMoveEnded = false;
            }
        }

        private CancellationTokenSource cancellationTokenSource;
        public void SetRollJustEnded()
        {
            IsJustRollEnded = true;
            Wait(0.25f).Forget();
            
            async UniTaskVoid Wait(float seconds)
            {
                cancellationTokenSource = new CancellationTokenSource();

                try
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: cancellationTokenSource.Token);
                    IsJustRollEnded = false;
                }
                catch (OperationCanceledException)
                {
                    Debug.Log("Roll Canceled.");
                }
            }
        }

        public void CancelRollJustEnded() => cancellationTokenSource?.Cancel();

        public void SetCrowdControlled() => IsUnderCrowdControl = true;

        public void ResetCrowdControlled() => IsUnderCrowdControl = false;

        public void SetIsGroundPoundingEnded() => IsGroundPoundingEnded = true;

        public void ResetIsGroundPoundingEnded() => IsGroundPoundingEnded = false;

        public void SetStealthMove() => IsStealthMove = true;

        public void ResetStealthMove() => IsStealthMove = false;

        public void ToggleStealthMove()
        {
            if (IsStealthMove) ResetStealthMove();
            else SetStealthMove();
        }

        [SerializeField] private bool isPlayer;
        public bool EnrollToSaveManager => isPlayer;
        public bool Save(string saveFileName)
        {
            ES3.Save("MaxWallJumpCount", MaxWallJumpCount, saveFileName);
            Debug.Log($"MaxWallJumpCount saved: {MaxWallJumpCount}");
            
            ES3.Save("ClimbStaminaTime", climbStaminaTime, saveFileName);
            Debug.Log($"ClimbStaminaTime saved: {climbStaminaTime}");
            
            ES3.Save("IsSlideJumpPossible", isSlideJumpPossible, saveFileName);
            Debug.Log($"IsSlideJumpPossible saved: {isSlideJumpPossible}");
            
            return true;
        }

        public bool Load(string saveFileName)
        {
            if (ES3.KeyExists("MaxWallJumpCount", saveFileName))
            {
                MaxWallJumpCount = ES3.Load<int>("MaxWallJumpCount", saveFileName);
                Debug.Log($"MaxWallJumpCount loaded: {MaxWallJumpCount}");
            }
            else
            {
                Debug.LogWarning("No MAxWallJumpCount found.");
            }
            
            if (ES3.KeyExists("ClimbStaminaTime", saveFileName))
            {
                climbStaminaTime = ES3.Load<float>("ClimbStaminaTime", saveFileName);
                Debug.Log($"ClimbStaminaTime loaded: {climbStaminaTime}");
            }
            else
            {
                Debug.LogWarning("No ClimbStaminaTime found.");
            }
            
            
            if (ES3.KeyExists("IsSlideJumpPossible", saveFileName))
            {
                isSlideJumpPossible = ES3.Load<bool>("IsSlideJumpPossible", saveFileName);
                Debug.Log($"IsSlideJumpPossible loaded: {isSlideJumpPossible}");
            }
            else
            {
                Debug.LogWarning("No IsSlideJumpPossible found.");
            }
            
            return true;
        }

    }
}