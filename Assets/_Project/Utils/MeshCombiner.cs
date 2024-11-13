#if UNITY_EDITOR
namespace _Project.Utils
{
    using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MeshCombiner : MonoBehaviour
{
    [MenuItem("Tools/Combine Selected Meshes with Ground and Wall Keywords")]
    public static void CombineSelectedMeshesByMaterialWithKeywords()
    {
        // 병합에 사용할 키워드 설정
        string[] keywords = new string[] { "Ground", "Wall" };

        // 현재 선택된 오브젝트들 가져오기
        GameObject[] selectedObjects = Selection.gameObjects;
        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("선택된 오브젝트가 없습니다.");
            return;
        }

        // 매터리얼별로 MeshFilter를 그룹화
        Dictionary<Material, List<MeshFilter>> materialToMeshFilters = new Dictionary<Material, List<MeshFilter>>();

        foreach (GameObject rootObj in selectedObjects)
        {
            // 선택된 오브젝트와 모든 하위 계층의 오브젝트 포함
            MeshFilter[] meshFilters = rootObj.GetComponentsInChildren<MeshFilter>(true);
            foreach (MeshFilter meshFilter in meshFilters)
            {
                // 오브젝트 이름에 키워드가 포함된 경우에만 진행
                if (!ObjectNameContainsKeywords(meshFilter.gameObject.name, keywords))
                    continue;

                MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();

                if (meshRenderer == null || meshFilter.sharedMesh == null)
                    continue;

                Material material = meshRenderer.sharedMaterial;

                // 매터리얼에 따른 리스트가 없으면 생성
                if (!materialToMeshFilters.ContainsKey(material))
                {
                    materialToMeshFilters[material] = new List<MeshFilter>();
                }

                // 매터리얼에 해당하는 MeshFilter 추가
                materialToMeshFilters[material].Add(meshFilter);
            }
        }

        // 각 매터리얼별로 메쉬 병합
        foreach (var entry in materialToMeshFilters)
        {
            Material material = entry.Key;
            List<MeshFilter> meshFilters = entry.Value;

            CombineInstance[] combine = new CombineInstance[meshFilters.Count];
            for (int i = 0; i < meshFilters.Count; i++)
            {
                combine[i].mesh = meshFilters[i].sharedMesh;
                combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            }

            // 새로운 메쉬 생성
            Mesh combinedMesh = new Mesh();
            combinedMesh.CombineMeshes(combine, true, true);

            // 병합된 메쉬를 가진 새로운 게임 오브젝트 생성
            GameObject newObject = new GameObject("CombinedMesh_" + material.name);
            MeshFilter newMeshFilter = newObject.AddComponent<MeshFilter>();
            newMeshFilter.sharedMesh = combinedMesh;
            MeshRenderer newMeshRenderer = newObject.AddComponent<MeshRenderer>();

            // 병합된 메쉬에 매터리얼 할당
            newMeshRenderer.sharedMaterial = material;

            // MeshCollider 추가 및 병합된 메쉬 설정
            MeshCollider meshCollider = newObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = combinedMesh;

            // 레이어 설정: 이름에 따라 Ground 또는 Wall 레이어를 설정
            if (ObjectNameContainsKeywords(newObject.name, new string[] { "Wall" }))
            {
                newObject.layer = LayerMask.NameToLayer("Wall");
            }
            else
            {
                newObject.layer = LayerMask.NameToLayer("Ground");
            }
        }

        // 원래 선택된 오브젝트 비활성화
        foreach (GameObject rootObj in selectedObjects)
        {
            MeshFilter[] meshFilters = rootObj.GetComponentsInChildren<MeshFilter>(true);
            foreach (MeshFilter meshFilter in meshFilters)
            {
                if (ObjectNameContainsKeywords(meshFilter.gameObject.name, keywords))
                {
                    meshFilter.gameObject.SetActive(false);
                }
            }
        }

        Debug.Log("Ground 및 Wall 키워드를 포함한 오브젝트들의 메쉬 병합 및 새로운 오브젝트 생성이 완료되었습니다.");
    }

    // 오브젝트 이름에 키워드가 포함되어 있는지 확인하는 함수
    private static bool ObjectNameContainsKeywords(string name, string[] keywords)
    {
        foreach (string keyword in keywords)
        {
            if (name.Contains(keyword))
            {
                return true;
            }
        }
        return false;
    }
}

}

#endif