using UnityEngine;

namespace _Project.Characters.IngameCharacters.Core
{
    public class VerticalParams : MonoBehaviour
    {
        public bool IsWalled {get;set;}
        public bool PrevIsWalled {get;set;}
        public bool IsEdgeOfPlatform { get; set; }
        public float DistanceToTopEdge { get; set; }
        public bool IsSightOpened {get;set;}
        public bool IsRightSightOpened {get;set;}
        public bool IsLeftSightOpened {get;set;}
        public bool IsRightLedgeMovable {get;set;}
        public bool IsLeftLedgeMovable {get;set;}
        
        // public static bool IsPrevWalled {get;set;}
        public Vector3? WallNormal { get; set; }
        public Vector3? WallPoint { get; set; }

        public float? WallDotToUp
        {
            get
            {
                if (WallNormal is not null)
                {
                    return Vector3.Dot(-WallNormal.Value, Vector3.up);
                }
                else return null;
            }
        }

        public bool IsWallPerpendicularToGround
        {
            get
            {
                if (WallDotToUp is not null && WallNormal is not null)
                {
                    return Mathf.Abs(Vector3.Dot(-WallNormal.Value, Vector3.up)) < 0.001f;
                }

                return false;
            }
        }
        
        public bool IsHeadOpen {get;set;}
    }
}