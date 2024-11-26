#if UNITY_EDITOR


using _Project.Characters.IngameCharacters.Core.MovementStates;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Project.Maps.Climber
{
    public class TestMapGenerator : MonoBehaviour
    {
        [SerializeField] private GameObject plane;
        [SerializeField] private GameObject climbPlane;
         
        [SerializeField, TitleGroup("Jump")] private JumpState jumpState;
        [FormerlySerializedAs("kickState")] [SerializeField, TitleGroup("Kick")] private AirDashState airDashState;
        
        private float HorizontalReach => jumpState.MaxLength;
        private float MinVerticalReach => jumpState.MinJumpHeight;
        private float MaxVerticalReach => jumpState.MaxJumpHeight;


        [SerializeField] private GameObject objMade;
        [SerializeField] private int planesNumber = 5;
        [Button]
        private void Generate()
        {
            if (objMade) DestroyImmediate(objMade);
            var obj = new GameObject("TestMap");
            obj.transform.parent = transform.parent;
            obj.AddComponent<MapGenerator>();
            objMade = obj;

            var firstPlane = Instantiate(plane, objMade.transform);

            
            var prevPlane = firstPlane;
            Random.InitState(0);
            for (var i = 0; i < planesNumber - 1; i++)
            {
                var randomValue = Random.Range(0f, 1f);
                if (randomValue < 0.5f) // jump & kick
                {
                    var horizontalLength = Random.Range(2, HorizontalReach + 1);

                    var reach = Vector3.forward;
                    if (i != 0)
                    {
                        if (horizontalLength > HorizontalReach * 0.5f + 1)
                        {
                            var randomHeight = Random.Range(0, MaxVerticalReach);
                            var randomDir2 = new Vector2(Random.Range(-1f, 1f), 1);

                            horizontalLength += Random.Range(0f, 1f) > 0.7 ? airDashState.MaxLength : 0;
                            randomDir2 = randomDir2.normalized * horizontalLength;
                            reach = new Vector3(randomDir2.x, randomHeight, randomDir2.y);
                        }
                        else
                        {
                            var horiMinusHalf = horizontalLength - HorizontalReach * 0.5f;
                            var powHoriMinusHalf = Mathf.Pow(horiMinusHalf, 2);
                            var randomHeight = Random.Range(0, MaxVerticalReach - powHoriMinusHalf);
                            var randomDir2 = new Vector2(Random.Range(-1f, 1f), 1);
                            randomDir2 = randomDir2.normalized * horizontalLength;
                            reach = new Vector3(randomDir2.x, randomHeight, randomDir2.y);
                        }
                    }

                    prevPlane = CreateNextPlane(prevPlane.transform, reach);
                }
                else // climb
                {
                    
                }
            }

            GameObject CreateNextPlane(Transform prevObjT, Vector3 reach)
            {
                var nextPlane = Instantiate(plane, objMade.transform);
                SetPosition(prevObjT.transform, nextPlane.transform, reach);
                return nextPlane;
            }
            
            void SetPosition(Transform prevObjT, Transform nextObjT, Vector3 reach)
            {
                nextObjT.transform.position = prevObjT.position + reach;
            }
        }
    }
}

#endif