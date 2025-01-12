using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "PathData", menuName = "Project/WWTC/PathDataSO")]
public class PathDataSO : ScriptableObject
{
    [FoldoutGroup("Data"), SerializeField] private List<Vector3> pathNodes           = new List<Vector3>();
    [FoldoutGroup("Data"), SerializeField] private List<Vector3> convertedPathNodes  = new List<Vector3>();
    [FoldoutGroup("Data"), SerializeField] private List<Vector3> curvePoints         = new List<Vector3>();
    [FoldoutGroup("Data"), SerializeField] private List<Vector3> downhillPoints      = new List<Vector3>();

    [FoldoutGroup("Data"), SerializeField] private List<DownhillNode> downhillNodes  = new List<DownhillNode>();

    [FoldoutGroup("Data"), SerializeField] private Rect boundingRect;

    [FoldoutGroup("Data"), SerializeField] private List<Vector3> courseVertices = new List<Vector3>();
    [FoldoutGroup("Data"), SerializeField] private List<int>     courseTris     = new List<int>();

    // === Getter ===
    public List<Vector3> PathNodes           => pathNodes;
    public List<Vector3> ConvertedPathNodes  => convertedPathNodes;
    public List<Vector3> CurvePoints         => curvePoints;
    public List<Vector3> DownhillPoints      => downhillPoints;
    public List<DownhillNode> DownhillNodes  => downhillNodes;
    public Rect BoundingRect                 => boundingRect;
    public List<Vector3> CourseVertices      => courseVertices;
    public List<int>     CourseTris          => courseTris;

    // === Clear / Set Methods ===

    [Button("Clear PathNodes")]
    public void ClearPathNodes()
    {
        pathNodes.Clear();
    }
    public void SetPathNodes(List<Vector3> newNodes)
    {
        pathNodes.Clear();
        pathNodes.AddRange(newNodes);
    }
    public List<Vector3> GetPathNodesCopy()
    {
        return new List<Vector3>(pathNodes);
    }

    [Button("Clear ConvertedPathNodes")]
    public void ClearConvertedPathNodes()
    {
        convertedPathNodes.Clear();
    }
    public void SetConvertedPathNodes(List<Vector3> newNodes)
    {
        convertedPathNodes.Clear();
        convertedPathNodes.AddRange(newNodes);
    }
    public List<Vector3> GetConvertedPathNodesCopy()
    {
        return new List<Vector3>(convertedPathNodes);
    }

    [Button("Clear CurvePoints")]
    public void ClearCurvePoints()
    {
        curvePoints.Clear();
    }
    public void SetCurvePoints(List<Vector3> newPoints)
    {
        curvePoints.Clear();
        curvePoints.AddRange(newPoints);
    }
    public List<Vector3> GetCurvePointsCopy()
    {
        return new List<Vector3>(curvePoints);
    }

    [Button("Clear DownhillPoints")]
    public void ClearDownhillPoints()
    {
        downhillPoints.Clear();
    }
    public void SetDownhillPoints(List<Vector3> newPoints)
    {
        downhillPoints.Clear();
        downhillPoints.AddRange(newPoints);
    }
    public List<Vector3> GetDownhillPointsCopy()
    {
        return new List<Vector3>(downhillPoints);
    }

    [Button("Clear DownhillNodes")]
    public void ClearDownhillNodes()
    {
        downhillNodes.Clear();
    }
    public void SetDownhillNodes(List<DownhillNode> newNodes)
    {
        downhillNodes.Clear();
        downhillNodes.AddRange(newNodes);
    }
    public List<DownhillNode> GetDownhillNodesCopy()
    {
        return new List<DownhillNode>(downhillNodes);
    }

    [Button("Clear BoundingRect")]
    public void ClearBoundingRect()
    {
        boundingRect = new Rect(0, 0, 0, 0);
    }
    public void SetBoundingRect(Rect r)
    {
        boundingRect = r;
    }

    [Button("Clear Course Data")]
    public void ClearCourseData()
    {
        courseVertices.Clear();
        courseTris.Clear();
    }
    public void SetCourseData(List<Vector3> verts, List<int> tris)
    {
        courseVertices.Clear();
        courseVertices.AddRange(verts);

        courseTris.Clear();
        courseTris.AddRange(tris);
    }
}
