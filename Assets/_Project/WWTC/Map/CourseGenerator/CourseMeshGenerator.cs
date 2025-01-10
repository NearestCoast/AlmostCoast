using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class CourseMeshGenerator : MonoBehaviour
{
    [Header("Curve Converter Reference")]
    [Tooltip("CatmullRomCurveConverter를 할당하세요.")]
    public CatmullRomCurveConverter curveConverter;

    [Header("Container Parent")]
    [Tooltip("생성된 코스 Mesh 오브젝트들을 담아둘 부모 트랜스폼입니다.")]
    public Transform container;

    [FoldoutGroup("Course Mesh Settings")]
    [Tooltip("코스(길)의 폭 (좌우 폭).")]
    public float width = 1f;

    // thickness(두께) 제거

    [FoldoutGroup("Course Mesh Settings")]
    [Tooltip("메시 생성 시, 곡선을 따라 세그먼트를 분할해서 생성할지 여부(데모용)")]
    public bool generateEachSegment = true;

    // (선택) 새로 생성한 오브젝트에 할당할 머티리얼
    [FoldoutGroup("Course Mesh Settings")]
    public Material courseMaterial;

    // 내부적으로 만들 Mesh 객체
    private Mesh courseMesh;

    [FoldoutGroup("Course Mesh Settings")]
    [Button("Generate Course Mesh", ButtonSizes.Large)]
    public void GenerateCourseMesh()
    {
        if (curveConverter == null)
        {
            Debug.LogWarning("[CourseMeshGenerator] CatmullRomCurveConverter가 할당되지 않았습니다!");
            return;
        }

        // 곡선 포인트 가져오기
        List<Vector3> points = curveConverter.CurvePoints;
        if (points == null || points.Count < 2)
        {
            Debug.LogWarning("[CourseMeshGenerator] CurvePoints가 충분하지 않습니다. (2개 미만)");
            return;
        }

        // **1) 기존에 만들어둔 자식 오브젝트 정리(반복 생성 시 중복 방지)**
        ClearOldChild();

        // **2) 새 자식 오브젝트 생성**
        GameObject courseObj = new GameObject("GeneratedCourseMesh");
        if (container == null)
        {
            container = this.transform;
        }
        courseObj.transform.SetParent(container, false);

        // Mesh 관련 컴포넌트 부착
        MeshFilter mf = courseObj.AddComponent<MeshFilter>();
        MeshRenderer mr = courseObj.AddComponent<MeshRenderer>();
        MeshCollider mc = courseObj.AddComponent<MeshCollider>();

        if (courseMesh == null)
            courseMesh = new Mesh();
        else
            courseMesh.Clear();

        // 정점 리스트와 삼각형 리스트, UV 리스트
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        float halfW = width * 0.5f;

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 p0 = points[i];
            Vector3 p1 = points[i + 1];

            // 곡선 진행방향
            Vector3 forward = (p1 - p0).normalized;

            // 단순히 위쪽은 Vector3.up
            Vector3 up = Vector3.up;

            // 폭(좌우) 방향
            Vector3 right = Vector3.Cross(forward, up).normalized;

            // 상하(두께) 없애고, 폭만 적용
            Vector3 p0Left  = p0 - right * halfW;
            Vector3 p0Right = p0 + right * halfW;
            Vector3 p1Left  = p1 - right * halfW;
            Vector3 p1Right = p1 + right * halfW;

            int baseIndex = vertices.Count;

            // ┌───┐  (사각형)
            // p0Left(0) -- p0Right(1)
            // p1Left(2) -- p1Right(3)

            vertices.Add(p0Left);   // baseIndex + 0
            vertices.Add(p0Right);  // baseIndex + 1
            vertices.Add(p1Left);   // baseIndex + 2
            vertices.Add(p1Right);  // baseIndex + 3

            // **삼각형 인덱스 순서** (위아래가 뒤집힐 때는 이 순서를 바꿔보세요)

            // 첫 삼각형
            triangles.Add(baseIndex + 0); // p0Left
            triangles.Add(baseIndex + 1); // p0Right
            triangles.Add(baseIndex + 2); // p1Left

            // 두 번째 삼각형
            triangles.Add(baseIndex + 2); // p1Left
            triangles.Add(baseIndex + 1); // p0Right
            triangles.Add(baseIndex + 3); // p1Right

            // UV (단순 계산)
            float u0 = (float)i / (points.Count - 1);
            float u1 = (float)(i + 1) / (points.Count - 1);

            uvs.Add(new Vector2(u0, 0f)); // p0Left
            uvs.Add(new Vector2(u0, 1f)); // p0Right
            uvs.Add(new Vector2(u1, 0f)); // p1Left
            uvs.Add(new Vector2(u1, 1f)); // p1Right
        }

        // Mesh 세팅
        courseMesh.vertices = vertices.ToArray();
        courseMesh.triangles = triangles.ToArray();
        courseMesh.uv = uvs.ToArray();

        courseMesh.RecalculateNormals();
        courseMesh.RecalculateBounds();

        // MeshFilter에 할당
        mf.sharedMesh = courseMesh;

        // MeshCollider에 할당
        mc.sharedMesh = courseMesh;
        mc.convex = false;

        // 머티리얼 적용
        if (courseMaterial != null)
        {
            mr.sharedMaterial = courseMaterial;
        }

        Debug.Log($"[CourseMeshGenerator] Course Mesh Generated.\n" +
                  $"Vertices: {courseMesh.vertexCount}, Triangles: {courseMesh.triangles.Length / 3}");
    }

    /// <summary>
    /// 기존에 생성된 "GeneratedCourseMesh"가 있으면 정리
    /// </summary>
    private void ClearOldChild()
    {
        if (container == null) return;

        List<Transform> toRemove = new List<Transform>();
        foreach (Transform child in container)
        {
            if (child.name.Contains("GeneratedCourseMesh"))
            {
                toRemove.Add(child);
            }
        }
        foreach (Transform t in toRemove)
        {
            DestroyImmediate(t.gameObject);
        }
    }
}
