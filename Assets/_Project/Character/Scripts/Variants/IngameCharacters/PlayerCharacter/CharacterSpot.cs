using System;
using System.Collections.Generic;
using _Project.Cameras;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Project.Character.Scripts.Variants.IngameCharacters.PlayerCharacter
{
    [Serializable]
    public class StartingPosition
    {
        [SerializeField] private Transform characterSpotT;
        [SerializeField] private Vector3 position;
        [SerializeField] private CustomCinemachineCamera camera;
        [SerializeField] private CinemachineBrain brain;
        public StartingPosition(Transform characterSpotT, Vector3 position, CustomCinemachineCamera camera, CinemachineBrain brain)
        {
            this.characterSpotT = characterSpotT;
            this.position = position;
            this.camera = camera;
            this.brain = brain;
        }
        
        
        
        [Button]
        private void MoveToPosition()
        {
            characterSpotT.position = position;
            // camera 이동
            if (camera != null)
            {
                // camera.Follow = characterSpotT; // Follow를 캐릭터로 설정
                // camera.LookAt = characterSpotT; // LookAt을 캐릭터로 설정

                // 카메라 상태를 강제로 업데이트하여 즉시 위치와 방향 적용
                // camera.ForceCameraPosition(camera.transform.position, camera.transform.rotation);

                CinemachineCore.SoloCamera = camera;
                wait().Forget();
            }

            async UniTaskVoid wait()
            {
                await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
                CinemachineCore.SoloCamera = null;
            }
        }
    }
    public class CharacterSpot : MonoBehaviour
    {
        [SerializeField] private List<StartingPosition> startingPositions = new List<StartingPosition>();

        [Button]
        private void AddStartingPositions()
        {
            startingPositions.Add(new StartingPosition(transform, transform.position, FindAnyObjectByType<CustomCinemachineCamera>(), FindAnyObjectByType<CinemachineBrain>()));
        }
    }
}