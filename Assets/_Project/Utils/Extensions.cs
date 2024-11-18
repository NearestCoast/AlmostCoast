using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Utils
{
    public static class Extensions
    {
        public static bool IsAngleBelowThreshold(this Vector3 vectorA, Vector3 vectorB, float thresholdAngle)
        {
            // 벡터의 도트 곱 계산
            float dotProduct = Vector3.Dot(vectorA.normalized, vectorB.normalized);

            // 두 벡터 사이의 각도 계산 (라디안)
            float angle = Mathf.Acos(dotProduct) * Mathf.Rad2Deg;

            // 각도가 thresholdAngle 이하인지 확인
            return angle <= thresholdAngle;
        }

        public static bool IsInLayerMask(this int layer, LayerMask layerMask)
        {
            return (layerMask.value & (1 << layer)) != 0;
        }
        
        public static void DestroyChildren(this Transform parent)
        {
            for (var i = parent.childCount -1 ; i > -1; i--)
            {
                GameObject.Destroy(parent.GetChild(i).gameObject);
            }
        }
        public static void DestroyChildrenImmediate(this Transform parent)
        {
            for (var i = parent.childCount -1 ; i > -1; i--)
            {
                GameObject.DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }
        public static bool IsEmpty(this string text)
        {
            return text == "";
        }
        public static T GetCopy<T>(T resource) where T : ICloneable
        {
            return (T) resource.Clone();
        }

        public static Vector3 XYZ3toXyZ3(this Vector3 origin, float y)
        {
            return new Vector3(origin.x, y, origin.z);
        }

        public static Vector3 XYZ3toX0Z3(this Vector3 origin)
        {
            return new Vector3(origin.x,0, origin.z);
        }

        public static Vector3 XYZ3to0Y03(this Vector3 origin)
        {
            return new Vector3(0, origin.y, 0);
        }

        public static Vector2 XZ3toXY2(this Vector3 origin)
        {
            return new Vector2(origin.x, origin.z);
        }

        public static Vector3 XY2toXZ3(this Vector2 origin)
        {
            return new Vector3(origin.x, 0, origin.y);
        }

        public static Vector3 GetRotatedVector(this Vector3 originalVector, float angleDegrees)
        {
            // xz 평면 투영 벡터
            Vector2 projectedVector = new Vector2(originalVector.x, originalVector.z);

            // 투영 벡터의 각도 계산
            float theta = Mathf.Atan2(projectedVector.y, projectedVector.x);

            // 새로운 각도 계산
            float newAngle = theta + angleDegrees * Mathf.PI / 180;

            // 새로운 벡터 계산
            float projectedVectorLength = projectedVector.magnitude;
            Vector3 newVector = new Vector3(
                projectedVectorLength * Mathf.Cos(newAngle),
                originalVector.y,
                projectedVectorLength * Mathf.Sin(newAngle)
            );

            return newVector;
        }
                
        public static Vector2 GetFourDirection(this Vector2 input)
        {
            // 위 또는 아래로 더 가까운 경우
            if (Mathf.Abs(input.x) < Mathf.Abs(input.y))
            {
                // 위로 가까우면 위로, 아래로 가까우면 아래로 설정
                return input.y > 0 ? Vector2.up : Vector2.down;
            }
            else
            {
                // 왼쪽으로 가까우면 왼쪽으로, 오른쪽으로 가까우면 오른쪽으로 설정
                return input.x > 0 ? Vector2.right : Vector2.left;
            }
        }
        
        public static Vector2 GetEightDirection(this Vector2 input)
        {
            if (input == Vector2.zero) return Vector2.zero;
            if (input.magnitude < 0.1f) return Vector2.zero;
            // 입력 벡터의 각도를 계산 (0도는 오른쪽, 반시계 방향으로 각도 증가)
            float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;

            // 각도를 양수로 변환 (0~360도 사이의 값)
            if (angle < 0) angle += 360f;

            // 8방향에 맞는 각도로 스냅하기
            if (angle >= 337.5f || angle < 22.5f)
                return Vector2.right;   // 0도: 오른쪽 (동쪽)
            else if (angle >= 22.5f && angle < 67.5f)
                return new Vector2(1, 1).normalized;  // 45도: 오른쪽 위 (북동쪽)
            else if (angle >= 67.5f && angle < 112.5f)
                return Vector2.up;      // 90도: 위 (북쪽)
            else if (angle >= 112.5f && angle < 157.5f)
                return new Vector2(-1, 1).normalized; // 135도: 왼쪽 위 (북서쪽)
            else if (angle >= 157.5f && angle < 202.5f)
                return Vector2.left;    // 180도: 왼쪽 (서쪽)
            else if (angle >= 202.5f && angle < 247.5f)
                return new Vector2(-1, -1).normalized; // 225도: 왼쪽 아래 (남서쪽)
            else if (angle >= 247.5f && angle < 292.5f)
                return Vector2.down;    // 270도: 아래 (남쪽)
            else if (angle >= 292.5f && angle < 337.5f)
                return new Vector2(1, -1).normalized; // 315도: 오른쪽 아래 (남동쪽)
            
            return Vector2.zero;    
        }
        
        public static Bounds CalculateCombinedBounds(this GameObject gameObject)
        {
            // 상위 오브젝트의 모든 자식 오브젝트에서 MeshRenderer를 찾음
            MeshRenderer[] meshRenderers = gameObject.GetComponentsInChildren<MeshRenderer>();

            if (meshRenderers.Length == 0)
            {
                Debug.LogWarning("No MeshRenderers found!");
                return new Bounds();  // 빈 바운드 반환
            }

            // 첫 번째 MeshRenderer의 바운드로 시작
            Bounds combinedBounds = meshRenderers[0].bounds;

            // 모든 MeshRenderer의 바운드를 합산하여 큰 바운딩 박스 생성
            for (int i = 1; i < meshRenderers.Length; i++)
            {
                combinedBounds.Encapsulate(meshRenderers[i].bounds);
            }

            return combinedBounds;
        }
        
        public static GameObject FindInactiveObjectByName(string name, Transform parent = null)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.isLoaded) return null;
            // 씬의 모든 루트 오브젝트를 가져오기
            GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

            foreach (GameObject rootObject in rootObjects)
            {
                GameObject result = FindInChildren(parent ? parent : rootObject.transform, name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static GameObject FindInChildren(Transform parent, string name)
        {
            // 현재 오브젝트가 찾는 오브젝트인지 확인
            if (parent.name == name)
            {
                return parent.gameObject;
            }

            // 자식 오브젝트들 재귀적으로 탐색
            foreach (Transform child in parent)
            {
                GameObject result = FindInChildren(child, name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
        
        public static T GetComponentInChildrenOfType<T>(this Transform parent, bool includeInactive = false) where T : Component
        {
            Queue<Transform> queue = new Queue<Transform>();
            queue.Enqueue(parent);

            while (queue.Count > 0)
            {
                Transform current = queue.Dequeue();

                // 비활성화된 오브젝트를 포함하지 않으려면 건너뜀
                if (!includeInactive && !current.gameObject.activeSelf)
                {
                    continue;
                }

                // 현재 Transform에서 T 컴포넌트 검색
                T component = current.GetComponent<T>();
                if (component != null)
                {
                    return component; // 조건에 맞는 첫 번째 오브젝트 반환
                }

                // 자식 Transform들을 큐에 추가
                foreach (Transform child in current)
                {
                    queue.Enqueue(child);
                }
            }

            return null; // 조건에 맞는 오브젝트가 없을 경우 null 반환
        }
        
        public static List<T> GetComponentsInChildrenOfType<T>(this Transform parent) where T : Component
        {
            List<T> results = new List<T>();
            Queue<Transform> queue = new Queue<Transform>();
            queue.Enqueue(parent);

            while (queue.Count > 0)
            {
                Transform current = queue.Dequeue();

                // 현재 Transform에서 T 컴포넌트 검색
                T component = current.GetComponent<T>();
                if (component != null)
                {
                    results.Add(component);
                }

                // 자식 Transform들을 큐에 추가
                foreach (Transform child in current)
                {
                    queue.Enqueue(child);
                }
            }

            return results;
        }
        
        public static T GetComponentInDirectChildren<T>(this GameObject parent) where T : Component
        {
            foreach (Transform child in parent.transform)
            {
                T component = child.GetComponent<T>();
                if (component != null)
                {
                    return component;
                }
            }
            return null;
        }

    }
}