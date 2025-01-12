using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// PathDataSO : 
///   - (A) 기존 Vector3 기반 필드들 (pathNodes, curvePoints, etc.)
///   - (B) DownhillNodes (List<DownhillNode>)
///   - (C) boundingRect
///   - (D) courseVertices, courseTris
///   - (E) convertedPathNodes (복원됨)
///   - (F) courseAreaPoints, hatchAreaPoints
///   - (G) heightAppliedPoints (List<GridNode>)
///   - (H) compositeMeshVerts, compositeMeshTris
///   - (I) compositeGridNodes(전체), courseGridNodes, hatchGridNodes (모두 List<GridNode>)
/// 등등을 관리하는 ScriptableObject.
/// </summary>
[CreateAssetMenu(fileName = "PathData", menuName = "Project/WWTC/PathDataSO")]
public class PathDataSO : ScriptableObject
{
    // ------------------------------------------------------------------------
    // (A) 기존 Vector3 기반 필드
    // ------------------------------------------------------------------------
    [FoldoutGroup("Data"), SerializeField] 
    private List<Vector3> pathNodes = new List<Vector3>();

    [FoldoutGroup("Data"), SerializeField] 
    private List<Vector3> curvePoints = new List<Vector3>();

    [FoldoutGroup("Data"), SerializeField] 
    private List<Vector3> downhillPoints = new List<Vector3>();

    // ------------------------------------------------------------------------
    // (B) DownhillNodes
    // ------------------------------------------------------------------------
    [FoldoutGroup("Data"), SerializeField] 
    private List<DownhillNode> downhillNodes = new List<DownhillNode>();

    // ------------------------------------------------------------------------
    // (C) boundingRect
    // ------------------------------------------------------------------------
    [FoldoutGroup("Data"), SerializeField] 
    private Rect boundingRect;

    // ------------------------------------------------------------------------
    // (D) 코스 Mesh 정보
    // ------------------------------------------------------------------------
    [FoldoutGroup("Data"), SerializeField] 
    private List<Vector3> courseVertices = new List<Vector3>();

    [FoldoutGroup("Data"), SerializeField] 
    private List<int> courseTris = new List<int>();

    // ------------------------------------------------------------------------
    // (E) **복원된** convertedPathNodes
    // ------------------------------------------------------------------------
    [FoldoutGroup("Data"), SerializeField]
    private List<Vector3> convertedPathNodes = new List<Vector3>();

    // ------------------------------------------------------------------------
    // (F) courseAreaPoints, hatchAreaPoints
    // ------------------------------------------------------------------------
    [FoldoutGroup("Data"), SerializeField]
    private List<Vector3> courseAreaPoints = new List<Vector3>();

    [FoldoutGroup("Data"), SerializeField]
    private List<Vector3> hatchAreaPoints  = new List<Vector3>();

    // ------------------------------------------------------------------------
    // (G) heightAppliedPoints => List<GridNode>
    // ------------------------------------------------------------------------
    [FoldoutGroup("Data"), SerializeField]
    private List<GridNode> heightAppliedPoints = new List<GridNode>();

    // ------------------------------------------------------------------------
    // (H) compositeMeshVerts, compositeMeshTris
    // ------------------------------------------------------------------------
    [FoldoutGroup("Data"), SerializeField]
    private List<Vector3> compositeMeshVerts = new List<Vector3>();

    [FoldoutGroup("Data"), SerializeField]
    private List<int> compositeMeshTris = new List<int>();

    // ------------------------------------------------------------------------
    // (I) compositeGridNodes, courseGridNodes, hatchGridNodes
    // ------------------------------------------------------------------------
    [FoldoutGroup("Data"), SerializeField]
    private List<GridNode> compositeGridNodes = new List<GridNode>();

    [FoldoutGroup("Data"), SerializeField]
    private List<GridNode> courseGridNodes    = new List<GridNode>();

    [FoldoutGroup("Data"), SerializeField]
    private List<GridNode> hatchGridNodes     = new List<GridNode>();

    // ================================
    // === Getter ===
    // ================================

    // (A)
    public List<Vector3> PathNodes      => pathNodes;
    public List<Vector3> CurvePoints    => curvePoints;
    public List<Vector3> DownhillPoints => downhillPoints;

    // (B)
    public List<DownhillNode> DownhillNodes => downhillNodes;

    // (C)
    public Rect BoundingRect => boundingRect;

    // (D)
    public List<Vector3> CourseVertices => courseVertices;
    public List<int> CourseTris        => courseTris;

    // (E) 복원된 convertedPathNodes
    public List<Vector3> ConvertedPathNodes => convertedPathNodes;

    // (F)
    public List<Vector3> CourseAreaPoints => courseAreaPoints;
    public List<Vector3> HatchAreaPoints  => hatchAreaPoints;

    // (G)
    public List<GridNode> HeightAppliedPoints => heightAppliedPoints;

    // (H)
    public List<Vector3> CompositeMeshVerts => compositeMeshVerts;
    public List<int> CompositeMeshTris      => compositeMeshTris;

    // (I)
    public List<GridNode> CompositeGridNodes => compositeGridNodes;
    public List<GridNode> CourseGridNodes    => courseGridNodes;
    public List<GridNode> HatchGridNodes     => hatchGridNodes;

    // ================================
    // === Clear / Set Methods ===
    // ================================

    // ---- PathNodes
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

    // ---- CurvePoints
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

    // ---- DownhillPoints
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

    // ---- DownhillNodes
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

    // ---- BoundingRect
    [Button("Clear BoundingRect")]
    public void ClearBoundingRect()
    {
        boundingRect = new Rect(0, 0, 0, 0);
    }
    public void SetBoundingRect(Rect r)
    {
        boundingRect = r;
    }

    // ---- Course Data
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

    // ---- (E) ConvertedPathNodes
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

    // ---- (F) CourseAreaPoints, HatchAreaPoints
    [Button("Clear CourseAreaPoints")]
    public void ClearCourseAreaPoints()
    {
        courseAreaPoints.Clear();
    }
    public void SetCourseAreaPoints(List<Vector3> newPoints)
    {
        courseAreaPoints.Clear();
        courseAreaPoints.AddRange(newPoints);
    }

    [Button("Clear HatchAreaPoints")]
    public void ClearHatchAreaPoints()
    {
        hatchAreaPoints.Clear();
    }
    public void SetHatchAreaPoints(List<Vector3> newPoints)
    {
        hatchAreaPoints.Clear();
        hatchAreaPoints.AddRange(newPoints);
    }

    // ---- (G) HeightAppliedPoints (List<GridNode>)
    [Button("Clear HeightAppliedPoints")]
    public void ClearHeightAppliedPoints()
    {
        heightAppliedPoints.Clear();
    }
    public void SetHeightAppliedPoints(List<GridNode> newNodes)
    {
        heightAppliedPoints.Clear();
        heightAppliedPoints.AddRange(newNodes);
    }

    // ---- (H) CompositeMesh
    [Button("Clear Composite Mesh")]
    public void ClearCompositeMeshData()
    {
        compositeMeshVerts.Clear();
        compositeMeshTris.Clear();
    }
    public void SetCompositeMeshData(List<Vector3> verts, List<int> tris)
    {
        compositeMeshVerts.Clear();
        compositeMeshVerts.AddRange(verts);

        compositeMeshTris.Clear();
        compositeMeshTris.AddRange(tris);
    }

    // ---- (I) CompositeGridNodes, CourseGridNodes, HatchGridNodes
    [Button("Clear Composite Grid Nodes")]
    public void ClearCompositeGridNodes()
    {
        compositeGridNodes.Clear();
    }
    public void SetCompositeGridNodes(List<GridNode> newNodes)
    {
        compositeGridNodes.Clear();
        compositeGridNodes.AddRange(newNodes);
    }

    [Button("Clear Course Grid Nodes")]
    public void ClearCourseGridNodes()
    {
        courseGridNodes.Clear();
    }
    public void SetCourseGridNodes(List<GridNode> newNodes)
    {
        courseGridNodes.Clear();
        courseGridNodes.AddRange(newNodes);
    }

    [Button("Clear Hatch Grid Nodes")]
    public void ClearHatchGridNodes()
    {
        hatchGridNodes.Clear();
    }
    public void SetHatchGridNodes(List<GridNode> newNodes)
    {
        hatchGridNodes.Clear();
        hatchGridNodes.AddRange(newNodes);
    }
}
