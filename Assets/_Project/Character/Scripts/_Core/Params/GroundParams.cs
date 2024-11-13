using UnityEngine;

namespace _Project.Characters.IngameCharacters.Core
{
    public class GroundParams : MonoBehaviour
    {
        public bool PrevIsGrounded {get;set;}
        public bool IsGrounded {get;set;}
        public Vector3 GroundNormal {get;set;}
        public Vector3 GroundPoint {get;set;}
        public Vector3 BelowGroundNormal {get;set;}
        public float GroundNormalDotWithGroundPlane => Vector3.Dot(GroundNormal, Vector3.up);
        public float HeightFromGround {get;set;}
        public float SpaceHeight {get;set;}
        public bool IsPlayerOnLedge {get;set;}
        public bool IsCeilingClose => SpaceHeight < 2f;
        
        public float SlopeAngleDeg { get; set; }
        public float SlopeAngleRad => SlopeAngleDeg * Mathf.Deg2Rad;
        
        public float RayLength { get; set; }
        
        public bool IsGroundedOnCharacter { get; set; }
    }
}