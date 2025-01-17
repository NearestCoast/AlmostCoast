using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Example/MapDataSO", fileName = "NewMapDataSO")]
public class MapDataSO : ScriptableObject
{
    public BoundaryData boundaryData;

    // 기존
    public List<CellPolygon> cellPolygons = new List<CellPolygon>();

    // 새로 추가: Voronoi 결과를 저장할 리스트
    public List<CellPolygon> shrunkPolygons = new List<CellPolygon>();
}

[System.Serializable]
public struct BoundaryData
{
    public Vector3 offset;
    public float centerX;
    public float centerZ;
    public float width;
    public float length;
}

[System.Serializable]
public class CellPolygon
{
    public float cellKey; 
    public List<Vector2> points;
    public Vector2 center;
    
    public float area;
}