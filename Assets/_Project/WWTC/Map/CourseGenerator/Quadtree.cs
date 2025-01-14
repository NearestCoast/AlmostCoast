using System.Collections.Generic;
using UnityEngine;
public class Quadtree
{
    private Rect bounds;    // 2D 범위 (xMin, yMin, width, height)
    private int capacity;   // 수용할 Tri 개수
    private List<QuadtreeTriangle> objects;

    private bool divided=false;
    private Quadtree topLeft, topRight, botLeft, botRight;

    public Quadtree(Rect boundary, int cap)
    {
        this.bounds= boundary;
        this.capacity= cap;
        objects= new List<QuadtreeTriangle>(cap);
        divided= false;
    }

    public bool Insert(QuadtreeTriangle tri)
    {
        // Tri의 AABB vs this.bounds (AABB 교차) 
        if(!Intersects(tri, bounds)) 
            return false;
        
        if(objects.Count < capacity && !divided)
        {
            objects.Add(tri);
            return true;
        }
        // subdivide
        if(!divided) Subdivide();
        // insert to children
        if(topLeft.Insert(tri)) return true;
        if(topRight.Insert(tri))return true;
        if(botLeft.Insert(tri)) return true;
        if(botRight.Insert(tri))return true;

        return false;
    }

    public void Query(Rect range, List<QuadtreeTriangle> found)
    {
        if(!range.Overlaps(bounds)) return;

        // check local objects
        for(int i=0; i<objects.Count; i++)
        {
            if(AABBOverlap(objects[i], range))
                found.Add(objects[i]);
        }

        if(divided)
        {
            topLeft.Query(range, found);
            topRight.Query(range, found);
            botLeft.Query(range, found);
            botRight.Query(range, found);
        }
    }

    private void Subdivide()
    {
        // 4개 사분면 쿼드트리 생성
        float halfW= bounds.width*0.5f;
        float halfH= bounds.height*0.5f;
        float x= bounds.xMin;
        float y= bounds.yMin;

        topLeft=  new Quadtree(new Rect(x,   y+halfH, halfW, halfH), capacity);
        topRight= new Quadtree(new Rect(x+halfW, y+halfH, halfW, halfH), capacity);
        botLeft=  new Quadtree(new Rect(x,   y, halfW, halfH), capacity);
        botRight= new Quadtree(new Rect(x+halfW, y, halfW, halfH), capacity);

        divided= true;
    }

    private bool Intersects(QuadtreeTriangle tri, Rect rect)
    {
        // 삼각형 AABB vs rect
        if(tri.xMax< rect.xMin) return false;
        if(tri.xMin> rect.xMax) return false;
        if(tri.yMax< rect.yMin) return false;
        if(tri.yMin> rect.yMax) return false;
        return true;
    }
    private bool AABBOverlap(QuadtreeTriangle tri, Rect range)
    {
        return Intersects(tri, range);
    }
}
