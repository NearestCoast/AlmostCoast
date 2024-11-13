#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Threading.Tasks;

namespace _Project.Utils
{
    public class UVScaler : MonoBehaviour
    {
        public float textureScale = 1.0f;

        [Button(ButtonSizes.Large)]
        [InfoBox("Scale UVs for all child MeshFilters to match the specified texel density.")]
        public async void ScaleUVsToTexelDensity()
        {
            MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();

            int processedCount = 0;

            // 애셋 편집 시작
            AssetDatabase.StartAssetEditing();

            try
            {
                for (int i = 0; i < meshFilters.Length; i++)
                {
                    MeshFilter meshFilter = meshFilters[i];
                    if (meshFilter == null || meshFilter.sharedMesh == null)
                        continue;

                    Mesh mesh = meshFilter.sharedMesh;

                    if (ProcessMeshUVs(mesh))
                    {
                        SaveMeshAsAsset(mesh, meshFilter.name, meshFilter);
                        processedCount++;
                    }

                    // 주기적으로 작업을 나누어 에디터의 응답성을 유지
                    if (i % 10 == 0)
                    {
                        await Task.Delay(1); // 1밀리초 대기
                    }
                }
            }
            finally
            {
                // 애셋 편집 종료
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets(); // 변경 사항 저장
            }

            Debug.Log($"{processedCount} mesh(es) UV scaled and saved.");
        }

        private bool ProcessMeshUVs(Mesh mesh)
        {
            // UV 배열 초기화
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector2[] uvs = new Vector2[vertices.Length];

            bool hasChanges = false;

            // UV 좌표 계산
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 localPos = vertices[i];
                Vector3 normal = normals[i];
                Vector2 newUV = CalculateUV(localPos, normal);

                // 기존 UV와 비교하여 변경이 있는 경우
                if (mesh.uv.Length > i && mesh.uv[i] != newUV)
                {
                    hasChanges = true;
                }

                uvs[i] = newUV;
            }

            if (!hasChanges)
            {
                // 변경 사항이 없으면 처리하지 않음
                return false;
            }

            // UV 적용 및 경계 재계산
            mesh.uv = uvs;
            mesh.RecalculateBounds();
            return true;
        }

        private Vector2 CalculateUV(Vector3 localPos, Vector3 normal)
        {
            if (Mathf.Abs(normal.x) > Mathf.Abs(normal.y) && Mathf.Abs(normal.x) > Mathf.Abs(normal.z))
            {
                // X축이 가장 큰 경우 Y-Z 평면 사용
                return new Vector2(localPos.y / textureScale, localPos.z / textureScale);
            }
            else if (Mathf.Abs(normal.y) > Mathf.Abs(normal.x) && Mathf.Abs(normal.y) > Mathf.Abs(normal.z))
            {
                // Y축이 가장 큰 경우 X-Z 평면 사용
                return new Vector2(localPos.x / textureScale, localPos.z / textureScale);
            }
            else
            {
                // Z축이 가장 큰 경우 X-Y 평면 사용
                return new Vector2(localPos.x / textureScale, localPos.y / textureScale);
            }
        }

        private void SaveMeshAsAsset(Mesh mesh, string meshName, MeshFilter meshFilter)
        {
            string sanitizedMeshName = SanitizeFileName(meshName + "_UVScaled");
            string assetPath = $"Assets/_Project/GeneratedMeshes/{sanitizedMeshName}.asset";

            // 폴더가 존재하지 않으면 생성
            System.IO.Directory.CreateDirectory("Assets/_Project/GeneratedMeshes");

            // 기존 애셋을 대체하여 저장
            Mesh newMesh = Instantiate(mesh);
            newMesh.name = sanitizedMeshName;
            AssetDatabase.CreateAsset(newMesh, assetPath);

            // MeshFilter의 sharedMesh를 새로 저장한 메쉬로 변경
            meshFilter.sharedMesh = newMesh;

            Debug.Log($"Mesh saved as asset: {assetPath} and assigned to {meshFilter.gameObject.name}");
        }

        private string SanitizeFileName(string fileName)
        {
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }
            // 유효하지 않은 다른 문자들을 추가적으로 대체
            fileName = fileName.Replace("|", "_").Replace(" ", "_").Replace(".", "_");
            return fileName;
        }
    }
}
#endif
