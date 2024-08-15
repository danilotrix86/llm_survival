using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SurvivalEngine.WorldGen
{
    /// <summary>
    /// This Voronoi diagram script is based on this open-source tutorial: https://www.habrador.com/tutorials/math/13-voronoi/ by Erik Nordeus
    /// </summary>

    public class Voronoi : MonoBehaviour
    {
        public static List<Vector3> sites = new List<Vector3>();
        public static List<VoronoiCell> cells = new List<VoronoiCell>();

        public static void Generate(float map_size, float nb_points)
        {
            //Generate the random sites
            sites.Clear();
            cells.Clear();

            float max = map_size;
            float min = -map_size;

            for (int i = 0; i < nb_points; i++)
            {
                float randomX = Random.Range(min, max);
                float randomZ = Random.Range(min, max);

                sites.Add(new Vector3(randomX, 0f, randomZ));
            }


            //Points outside of the screen for voronoi which has some cells that are infinite
            float bigSize = map_size * 3f;

            //Star shape which will give a better result when a cell is infinite large
            //When using other shapes, some of the infinite cells misses triangles
            sites.Add(new Vector3(0f, 0f, bigSize));
            sites.Add(new Vector3(0f, 0f, -bigSize));
            sites.Add(new Vector3(bigSize, 0f, 0f));
            sites.Add(new Vector3(-bigSize, 0f, 0f));


            //Generate the voronoi diagram
            List<VoronoiCell> ncells = GenerateVoronoiDiagram(sites);

            ClipDiagram(ncells, map_size);


            foreach (VoronoiCell cell in ncells)
            {
                cell.color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);
            }

            cells.AddRange(ncells);
        }

        public static List<VoronoiCell> GenerateVoronoiDiagram(List<Vector3> sites)
        {
            //First generate the delaunay triangulation
            List<Triangle> triangles = TriangulateByFlippingEdges(sites);


            //Generate the voronoi diagram

            //Step 1. For every delaunay edge, compute a voronoi edge
            //The voronoi edge is the edge connecting the circumcenters of two neighboring delaunay triangles
            List<VoronoiEdge> voronoiEdges = new List<VoronoiEdge>();

            for (int i = 0; i < triangles.Count; i++)
            {
                Triangle t = triangles[i];

                //Each triangle consists of these edges
                HalfEdge e1 = t.halfEdge;
                HalfEdge e2 = e1.nextEdge;
                HalfEdge e3 = e2.nextEdge;

                //Calculate the circumcenter for this triangle
                Vector3 v1 = e1.v.position;
                Vector3 v2 = e2.v.position;
                Vector3 v3 = e3.v.position;

                //The circumcenter is the center of a circle where the triangles corners is on the circumference of that circle
                //The .XZ() is an extension method that removes the y value of a vector3 so it becomes a vector2
                Vector2 center2D = CalculateCircleCenter(new Vector2(v1.x, v1.z), new Vector2(v2.x, v2.z), new Vector2(v3.x, v3.z));

                //The circumcenter is also known as a voronoi vertex, which is a position in the diagram where we are equally
                //close to the surrounding sites
                Vector3 voronoiVertex = new Vector3(center2D.x, 0f, center2D.y);

                TryAddVoronoiEdgeFromTriangleEdge(e1, voronoiVertex, voronoiEdges);
                TryAddVoronoiEdgeFromTriangleEdge(e2, voronoiVertex, voronoiEdges);
                TryAddVoronoiEdgeFromTriangleEdge(e3, voronoiVertex, voronoiEdges);
            }


            //Step 2. Find the voronoi cells where each cell is a list of all edges belonging to a site
            List<VoronoiCell> voronoiCells = new List<VoronoiCell>();

            for (int i = 0; i < voronoiEdges.Count; i++)
            {
                VoronoiEdge e = voronoiEdges[i];

                //Find the position in the list of all cells that includes this site
                int cellPos = TryFindCellPos(e, voronoiCells);

                //No cell was found so we need to create a new cell
                if (cellPos == -1)
                {
                    VoronoiCell newCell = new VoronoiCell(e.sitePos);

                    voronoiCells.Add(newCell);

                    newCell.edges.Add(e);
                    e.addCell(newCell);
                }
                else
                {
                    voronoiCells[cellPos].edges.Add(e);
                }
            }


            return voronoiCells;
        }

        private static void ClipDiagram(List<VoronoiCell> cells, float halfWidth)
        {
            //Remove the first 4 cells we added in the beginning because they are not needed anymore
            //cells.RemoveRange(0, 4);

            for (int i = 0; i < cells.Count; i++)
            {
                //We should move around the cell counter-clockwise so make sure all edges are oriented in that way
                List<VoronoiEdge> cellEdges = cells[i].edges;

                for (int j = cellEdges.Count - 1; j >= 0; j--)
                {
                    Vector3 edge_v1 = cellEdges[j].v1;
                    Vector3 edge_v2 = cellEdges[j].v2;

                    //Remove this edge if it is small
                    if ((edge_v1 - edge_v2).sqrMagnitude < 0.01f)
                    {
                        cellEdges.RemoveAt(j);

                        continue;
                    }

                    Vector3 edgeCenter = (edge_v1 + edge_v2) * 0.5f;

                    //Now we can make a line between the cell and the edge
                    Vector2 a = new Vector2(cells[i].sitePos.x, cells[i].sitePos.z);
                    Vector2 b = new Vector2(edgeCenter.x, edgeCenter.z);

                    //The point to the left of this line is coming after the other point if we are moving counter-clockwise
                    if (IsAPointLeftOfVector(a, b, new Vector2(edge_v1.x, edge_v1.z)) < 0f)
                    {
                        //Flip because we want to go from v1 to v2
                        cellEdges[j].v2 = edge_v1;
                        cellEdges[j].v1 = edge_v2;
                    }
                }

                //Connect the edges

                VoronoiEdge startEdge = cellEdges[0];

                cells[i].borderCoordinates.Add(startEdge.v2);

                Vector3 currentVertex = startEdge.v2;

                for (int j = 1; j < cellEdges.Count; j++)
                {
                    //Find the next edge
                    for (int k = 1; k < cellEdges.Count; k++)
                    {
                        Vector3 thisEdgeStart = cellEdges[k].v1;

                        if ((thisEdgeStart - currentVertex).sqrMagnitude < 0.01f)
                        {
                            cells[i].borderCoordinates.Add(cellEdges[k].v2);

                            currentVertex = cellEdges[k].v2;

                            break;
                        }
                    }
                }
            }

            List<Vector3> clipPolygon = new List<Vector3>();

            //The positions of the square border
            Vector3 TL = new Vector3(-halfWidth, 0f, halfWidth);
            Vector3 TR = new Vector3(halfWidth, 0f, halfWidth);
            Vector3 BR = new Vector3(halfWidth, 0f, -halfWidth);
            Vector3 BL = new Vector3(-halfWidth, 0f, -halfWidth);

            clipPolygon.Add(TL);
            clipPolygon.Add(BL);
            clipPolygon.Add(BR);
            clipPolygon.Add(TR);

            //Create the clipping planes
            List<Plane> clippingPlanes = new List<Plane>();

            for (int i = 0; i < clipPolygon.Count; i++)
            {
                int iPlusOne = ClampListIndex(i + 1, clipPolygon.Count);

                Vector3 v1 = clipPolygon[i];
                Vector3 v2 = clipPolygon[iPlusOne];

                //Doesnt have to be center but easier to debug
                Vector3 planePos = (v1 + v2) * 0.5f;

                Vector3 planeDir = v2 - v1;

                //Should point inwards
                Vector3 planeNormal = new Vector3(-planeDir.z, 0f, planeDir.x).normalized;

                clippingPlanes.Add(new Plane(planePos, planeNormal));
            }

            for (int i = 0; i < cells.Count; i++)
            {
                cells[i].borderCoordinates = ClipPolygon(cells[i].borderCoordinates, clippingPlanes);
            }
        }

        public static List<HalfEdge> TransformFromTriangleToHalfEdge(List<Triangle> triangles)
        {
            //Make sure the triangles have the same orientation
            OrientTrianglesClockwise(triangles);

            //First create a list with all possible half-edges
            List<HalfEdge> halfEdges = new List<HalfEdge>(triangles.Count * 3);

            for (int i = 0; i < triangles.Count; i++)
            {
                Triangle t = triangles[i];

                HalfEdge he1 = new HalfEdge(t.v1);
                HalfEdge he2 = new HalfEdge(t.v2);
                HalfEdge he3 = new HalfEdge(t.v3);

                he1.nextEdge = he2;
                he2.nextEdge = he3;
                he3.nextEdge = he1;

                he1.prevEdge = he3;
                he2.prevEdge = he1;
                he3.prevEdge = he2;

                //The vertex needs to know of an edge going from it
                he1.v.halfEdge = he2;
                he2.v.halfEdge = he3;
                he3.v.halfEdge = he1;

                //The face the half-edge is connected to
                t.halfEdge = he1;

                he1.t = t;
                he2.t = t;
                he3.t = t;

                //Add the half-edges to the list
                halfEdges.Add(he1);
                halfEdges.Add(he2);
                halfEdges.Add(he3);
            }

            //Find the half-edges going in the opposite direction
            for (int i = 0; i < halfEdges.Count; i++)
            {
                HalfEdge he = halfEdges[i];

                Vertex goingToVertex = he.v;
                Vertex goingFromVertex = he.prevEdge.v;

                for (int j = 0; j < halfEdges.Count; j++)
                {
                    //Dont compare with itself
                    if (i == j)
                    {
                        continue;
                    }

                    HalfEdge heOpposite = halfEdges[j];

                    //Is this edge going between the vertices in the opposite direction
                    if (goingFromVertex.position == heOpposite.v.position && goingToVertex.position == heOpposite.prevEdge.v.position)
                    {
                        he.oppositeEdge = heOpposite;

                        break;
                    }
                }
            }


            return halfEdges;
        }

        public static void OrientTrianglesClockwise(List<Triangle> triangles)
        {
            for (int i = 0; i < triangles.Count; i++)
            {
                Triangle tri = triangles[i];

                Vector2 v1 = new Vector2(tri.v1.position.x, tri.v1.position.z);
                Vector2 v2 = new Vector2(tri.v2.position.x, tri.v2.position.z);
                Vector2 v3 = new Vector2(tri.v3.position.x, tri.v3.position.z);

                if (!IsTriangleOrientedClockwise(v1, v2, v3))
                {
                    tri.ChangeOrientation();
                }
            }
        }

        public static List<Triangle> TriangulateByFlippingEdges(List<Vector3> sites)
        {
            //Step 1. Triangulate the points with some algorithm
            //Vector3 to vertex
            List<Vertex> vertices = new List<Vertex>();

            for (int i = 0; i < sites.Count; i++)
            {
                vertices.Add(new Vertex(sites[i]));
            }

            //Triangulate the convex hull of the sites
            List<Triangle> triangles = TriangulatePoints(vertices);
            //List triangles = TriangulatePoints.TriangleSplitting(vertices);

            //Step 2. Change the structure from triangle to half-edge to make it faster to flip edges
            List<HalfEdge> halfEdges = TransformFromTriangleToHalfEdge(triangles);

            //Step 3. Flip edges until we have a delaunay triangulation
            int safety = 0;

            int flippedEdges = 0;

            while (true)
            {
                safety += 1;

                if (safety > 100000)
                {
                    Debug.Log("Stuck in endless loop");

                    break;
                }

                bool hasFlippedEdge = false;

                //Search through all edges to see if we can flip an edge
                for (int i = 0; i < halfEdges.Count; i++)
                {
                    HalfEdge thisEdge = halfEdges[i];

                    //Is this edge sharing an edge, otherwise its a border, and then we cant flip the edge
                    if (thisEdge.oppositeEdge == null)
                    {
                        continue;
                    }

                    //The vertices belonging to the two triangles, c-a are the edge vertices, b belongs to this triangle
                    Vertex a = thisEdge.v;
                    Vertex b = thisEdge.nextEdge.v;
                    Vertex c = thisEdge.prevEdge.v;
                    Vertex d = thisEdge.oppositeEdge.nextEdge.v;

                    Vector2 aPos = a.GetPos2D_XZ();
                    Vector2 bPos = b.GetPos2D_XZ();
                    Vector2 cPos = c.GetPos2D_XZ();
                    Vector2 dPos = d.GetPos2D_XZ();

                    //Use the circle test to test if we need to flip this edge
                    if (IsPointInsideOutsideOrOnCircle(aPos, bPos, cPos, dPos) < 0f)
                    {
                        //Are these the two triangles that share this edge forming a convex quadrilateral?
                        //Otherwise the edge cant be flipped
                        if (IsQuadrilateralConvex(aPos, bPos, cPos, dPos))
                        {
                            //If the new triangle after a flip is not better, then dont flip
                            //This will also stop the algoritm from ending up in an endless loop
                            if (IsPointInsideOutsideOrOnCircle(bPos, cPos, dPos, aPos) < 0f)
                            {
                                continue;
                            }

                            //Flip the edge
                            flippedEdges += 1;

                            hasFlippedEdge = true;

                            FlipEdge(thisEdge);
                        }
                    }
                }

                //We have searched through all edges and havent found an edge to flip, so we have a Delaunay triangulation!
                if (!hasFlippedEdge)
                {
                    //Debug.Log("Found a delaunay triangulation");

                    break;
                }
            }

            //Debug.Log("Flipped edges: " + flippedEdges);

            //Dont have to convert from half edge to triangle because the algorithm will modify the objects, which belongs to the 
            //original triangles, so the triangles have the data we need

            return triangles;
        }

        public static List<Triangle> TriangulatePoints(List<Vertex> points)
        {
            List<Triangle> triangles = new List<Triangle>();

            //Sort the points along x-axis
            //OrderBy is always soring in ascending order - use OrderByDescending to get in the other order
            points = points.OrderBy(n => n.position.x).ToList();

            //The first 3 vertices are always forming a triangle
            Triangle newTriangle = new Triangle(points[0].position, points[1].position, points[2].position);

            triangles.Add(newTriangle);

            //All edges that form the triangles, so we have something to test against
            List<Edge> edges = new List<Edge>();

            edges.Add(new Edge(newTriangle.v1, newTriangle.v2));
            edges.Add(new Edge(newTriangle.v2, newTriangle.v3));
            edges.Add(new Edge(newTriangle.v3, newTriangle.v1));

            //Add the other triangles one by one
            //Starts at 3 because we have already added 0,1,2
            for (int i = 3; i < points.Count; i++)
            {
                Vector3 currentPoint = points[i].position;

                //The edges we add this loop or we will get stuck in an endless loop
                List<Edge> newEdges = new List<Edge>();

                //Is this edge visible? We only need to check if the midpoint of the edge is visible 
                for (int j = 0; j < edges.Count; j++)
                {
                    Edge currentEdge = edges[j];

                    Vector3 midPoint = (currentEdge.v1.position + currentEdge.v2.position) / 2f;

                    Edge edgeToMidpoint = new Edge(currentPoint, midPoint);

                    //Check if this line is intersecting
                    bool canSeeEdge = true;

                    for (int k = 0; k < edges.Count; k++)
                    {
                        //Dont compare the edge with itself
                        if (k == j)
                        {
                            continue;
                        }

                        if (AreEdgesIntersecting(edgeToMidpoint, edges[k]))
                        {
                            canSeeEdge = false;

                            break;
                        }
                    }

                    //This is a valid triangle
                    if (canSeeEdge)
                    {
                        Edge edgeToPoint1 = new Edge(currentEdge.v1, new Vertex(currentPoint));
                        Edge edgeToPoint2 = new Edge(currentEdge.v2, new Vertex(currentPoint));

                        newEdges.Add(edgeToPoint1);
                        newEdges.Add(edgeToPoint2);

                        Triangle newTri = new Triangle(edgeToPoint1.v1, edgeToPoint1.v2, edgeToPoint2.v1);

                        triangles.Add(newTri);
                    }
                }


                for (int j = 0; j < newEdges.Count; j++)
                {
                    edges.Add(newEdges[j]);
                }
            }


            return triangles;
        }



        private static bool AreEdgesIntersecting(Edge edge1, Edge edge2)
        {
            Vector2 l1_p1 = new Vector2(edge1.v1.position.x, edge1.v1.position.z);
            Vector2 l1_p2 = new Vector2(edge1.v2.position.x, edge1.v2.position.z);

            Vector2 l2_p1 = new Vector2(edge2.v1.position.x, edge2.v1.position.z);
            Vector2 l2_p2 = new Vector2(edge2.v2.position.x, edge2.v2.position.z);

            bool isIntersecting = AreLinesIntersecting(l1_p1, l1_p2, l2_p1, l2_p2, true);

            return isIntersecting;
        }

        public static bool AreLinesIntersecting(Vector2 l1_p1, Vector2 l1_p2, Vector2 l2_p1, Vector2 l2_p2, bool shouldIncludeEndPoints)
        {
            //To avoid floating point precision issues we can add a small value
            float epsilon = 0.00001f;

            bool isIntersecting = false;

            float denominator = (l2_p2.y - l2_p1.y) * (l1_p2.x - l1_p1.x) - (l2_p2.x - l2_p1.x) * (l1_p2.y - l1_p1.y);

            //Make sure the denominator is > 0, if not the lines are parallel
            if (denominator != 0f)
            {
                float u_a = ((l2_p2.x - l2_p1.x) * (l1_p1.y - l2_p1.y) - (l2_p2.y - l2_p1.y) * (l1_p1.x - l2_p1.x)) / denominator;
                float u_b = ((l1_p2.x - l1_p1.x) * (l1_p1.y - l2_p1.y) - (l1_p2.y - l1_p1.y) * (l1_p1.x - l2_p1.x)) / denominator;

                //Are the line segments intersecting if the end points are the same
                if (shouldIncludeEndPoints)
                {
                    //Is intersecting if u_a and u_b are between 0 and 1 or exactly 0 or 1
                    if (u_a >= 0f + epsilon && u_a <= 1f - epsilon && u_b >= 0f + epsilon && u_b <= 1f - epsilon)
                    {
                        isIntersecting = true;
                    }
                }
                else
                {
                    //Is intersecting if u_a and u_b are between 0 and 1
                    if (u_a > 0f + epsilon && u_a < 1f - epsilon && u_b > 0f + epsilon && u_b < 1f - epsilon)
                    {
                        isIntersecting = true;
                    }
                }
            }

            return isIntersecting;
        }

        private static void FlipEdge(HalfEdge one)
        {
            //The data we need
            //This edge's triangle
            HalfEdge two = one.nextEdge;
            HalfEdge three = one.prevEdge;
            //The opposite edge's triangle
            HalfEdge four = one.oppositeEdge;
            HalfEdge five = one.oppositeEdge.nextEdge;
            HalfEdge six = one.oppositeEdge.prevEdge;
            //The vertices
            Vertex a = one.v;
            Vertex b = one.nextEdge.v;
            Vertex c = one.prevEdge.v;
            Vertex d = one.oppositeEdge.nextEdge.v;



            //Flip

            //Change vertex
            a.halfEdge = one.nextEdge;
            c.halfEdge = one.oppositeEdge.nextEdge;

            //Change half-edge
            //Half-edge - half-edge connections
            one.nextEdge = three;
            one.prevEdge = five;

            two.nextEdge = four;
            two.prevEdge = six;

            three.nextEdge = five;
            three.prevEdge = one;

            four.nextEdge = six;
            four.prevEdge = two;

            five.nextEdge = one;
            five.prevEdge = three;

            six.nextEdge = two;
            six.prevEdge = four;

            //Half-edge - vertex connection
            one.v = b;
            two.v = b;
            three.v = c;
            four.v = d;
            five.v = d;
            six.v = a;

            //Half-edge - triangle connection
            Triangle t1 = one.t;
            Triangle t2 = four.t;

            one.t = t1;
            three.t = t1;
            five.t = t1;

            two.t = t2;
            four.t = t2;
            six.t = t2;

            //Opposite-edges are not changing!

            //Triangle connection
            t1.v1 = b;
            t1.v2 = c;
            t1.v3 = d;

            t2.v1 = b;
            t2.v2 = d;
            t2.v3 = a;

            t1.halfEdge = three;
            t2.halfEdge = four;
        }

        public static bool IsQuadrilateralConvex(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            bool isConvex = false;

            bool abc = IsTriangleOrientedClockwise(a, b, c);
            bool abd = IsTriangleOrientedClockwise(a, b, d);
            bool bcd = IsTriangleOrientedClockwise(b, c, d);
            bool cad = IsTriangleOrientedClockwise(c, a, d);

            if (abc && abd && bcd & !cad)
            {
                isConvex = true;
            }
            else if (abc && abd && !bcd & cad)
            {
                isConvex = true;
            }
            else if (abc && !abd && bcd & cad)
            {
                isConvex = true;
            }
            //The opposite sign, which makes everything inverted
            else if (!abc && !abd && !bcd & cad)
            {
                isConvex = true;
            }
            else if (!abc && !abd && bcd & !cad)
            {
                isConvex = true;
            }
            else if (!abc && abd && !bcd & !cad)
            {
                isConvex = true;
            }


            return isConvex;
        }

        public static bool IsTriangleOrientedClockwise(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            bool isClockWise = true;

            float determinant = p1.x * p2.y + p3.x * p1.y + p2.x * p3.y - p1.x * p3.y - p3.x * p2.y - p2.x * p1.y;

            if (determinant > 0f)
            {
                isClockWise = false;
            }

            return isClockWise;
        }

        public static float IsPointInsideOutsideOrOnCircle(Vector2 aVec, Vector2 bVec, Vector2 cVec, Vector2 dVec)
        {
            //This first part will simplify how we calculate the determinant
            float a = aVec.x - dVec.x;
            float d = bVec.x - dVec.x;
            float g = cVec.x - dVec.x;

            float b = aVec.y - dVec.y;
            float e = bVec.y - dVec.y;
            float h = cVec.y - dVec.y;

            float c = a * a + b * b;
            float f = d * d + e * e;
            float i = g * g + h * h;

            float determinant = (a * e * i) + (b * f * g) + (c * d * h) - (g * e * c) - (h * f * a) - (i * d * b);

            return determinant;
        }

        //Find the position in the list of all cells that includes this site
        //Returns -1 if no cell is found
        private static int TryFindCellPos(VoronoiEdge e, List<VoronoiCell> voronoiCells)
        {
            for (int i = 0; i < voronoiCells.Count; i++)
            {
                if (e.sitePos == voronoiCells[i].sitePos)
                {
                    return i;
                }
            }

            return -1;
        }

        //Try to add a voronoi edge. Not all edges have a neighboring triangle, and if it hasnt we cant add a voronoi edge
        private static void TryAddVoronoiEdgeFromTriangleEdge(HalfEdge e, Vector3 voronoiVertex, List<VoronoiEdge> allEdges)
        {
            //Ignore if this edge has no neighboring triangle
            if (e.oppositeEdge == null)
            {
                return;
            }

            //Calculate the circumcenter of the neighbor
            HalfEdge eNeighbor = e.oppositeEdge;

            Vector3 v1 = eNeighbor.v.position;
            Vector3 v2 = eNeighbor.nextEdge.v.position;
            Vector3 v3 = eNeighbor.nextEdge.nextEdge.v.position;

            //The .XZ() is an extension method that removes the y value of a vector3 so it becomes a vector2
            Vector2 center2D = CalculateCircleCenter(new Vector2(v1.x, v1.z), new Vector2(v2.x, v2.z), new Vector2(v3.x, v3.z));

            Vector3 voronoiVertexNeighbor = new Vector3(center2D.x, 0f, center2D.y);

            //Create a new voronoi edge between the voronoi vertices
            VoronoiEdge edge = new VoronoiEdge(voronoiVertex, voronoiVertexNeighbor, e.prevEdge.v.position);

            allEdges.Add(edge);
        }

        //Calculate the center of circle in 2d space given three coordinates
        public static Vector2 CalculateCircleCenter(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            Vector2 center = new Vector2();

            float ma = (p2.y - p1.y) / (p2.x - p1.x);
            float mb = (p3.y - p2.y) / (p3.x - p2.x);

            center.x = (ma * mb * (p1.y - p3.y) + mb * (p1.x + p2.x) - ma * (p2.x + p3.x)) / (2 * (mb - ma));

            center.y = (-1 / ma) * (center.x - (p1.x + p2.x) / 2) + (p1.y + p2.y) / 2;

            return center;
        }

        public static float IsAPointLeftOfVector(Vector2 a, Vector2 b, Vector2 p)
        {
            float determinant = (a.x - p.x) * (b.y - p.y) - (a.y - p.y) * (b.x - p.x);

            return determinant;
        }

        public static int ClampListIndex(int index, int listSize)
        {
            index = ((index % listSize) + listSize) % listSize;

            return index;
        }

        public static float DistanceFromPointToPlane(Vector3 planeNormal, Vector3 planePos, Vector3 pointPos)
        {
            //Positive distance denotes that the point p is on the front side of the plane 
            //Negative means it's on the back side
            float distance = Vector3.Dot(planeNormal, pointPos - planePos);

            return distance;
        }

        public static Vector3 GetRayPlaneIntersectionCoordinate(Vector3 planePos, Vector3 planeNormal, Vector3 rayStart, Vector3 rayDir)
        {
            float denominator = Vector3.Dot(-planeNormal, rayDir);

            Vector3 vecBetween = planePos - rayStart;

            float t = Vector3.Dot(vecBetween, -planeNormal) / denominator;

            Vector3 intersectionPoint = rayStart + rayDir * t;

            return intersectionPoint;
        }

        /*public static List<Vector3> ClipPolygon(List<Vector3> poly_1, List<Vector3> poly_2)
        {
            //Calculate the clipping planes
            List<Plane> clippingPlanes = new List<Plane>();

            for (int i = 0; i < poly_2.Count; i++)
            {
                int iPlusOne = ClampListIndex(i + 1, poly_2.Count);

                Vector3 v1 = poly_2[i];
                Vector3 v2 = poly_2[iPlusOne];

                //Doesnt have to be center but easier to debug
                Vector3 planePos = (v1 + v2) * 0.5f;

                Vector3 planeDir = v2 - v1;

                //Should point inwards
                Vector3 planeNormal = new Vector3(-planeDir.z, 0f, planeDir.x).normalized;

                //Gizmos.DrawRay(planePos, planeNormal * 0.1f);

                clippingPlanes.Add(new Plane(planePos, planeNormal));
            }



            List<Vector3> vertices = ClipPolygon(poly_1, clippingPlanes);

            return vertices;
        }*/

        //Sometimes its more efficient to calculate the planes once before we call the method
        //if we want to cut several polygons with the same planes
        public static List<Vector3> ClipPolygon(List<Vector3> poly_1, List<Plane> clippingPlanes)
        {
            //Clone the vertices because we will remove vertices from this list
            List<Vector3> vertices = new List<Vector3>(poly_1);

            //Save the new vertices temporarily in this list before transfering them to vertices
            List<Vector3> vertices_tmp = new List<Vector3>();


            //Clip the polygon
            for (int i = 0; i < clippingPlanes.Count; i++)
            {
                Plane plane = clippingPlanes[i];

                for (int j = 0; j < vertices.Count; j++)
                {
                    int jPlusOne = ClampListIndex(j + 1, vertices.Count);

                    Vector3 v1 = vertices[j];
                    Vector3 v2 = vertices[jPlusOne];

                    //Calculate the distance to the plane from each vertex
                    //This is how we will know if they are inside or outside
                    //If they are inside, the distance is positive, which is why the planes normals have to be oriented to the inside
                    float dist_to_v1 = DistanceFromPointToPlane(plane.normal, plane.pos, v1);
                    float dist_to_v2 = DistanceFromPointToPlane(plane.normal, plane.pos, v2);

                    //Case 1. Both are outside (= to the right), do nothing                    

                    //Case 2. Both are inside (= to the left), save v2
                    if (dist_to_v1 > 0f && dist_to_v2 > 0f)
                    {
                        vertices_tmp.Add(v2);
                    }
                    //Case 3. Outside -> Inside, save intersection point and v2
                    else if (dist_to_v1 < 0f && dist_to_v2 > 0f)
                    {
                        Vector3 rayDir = (v2 - v1).normalized;

                        Vector3 intersectionPoint = GetRayPlaneIntersectionCoordinate(plane.pos, plane.normal, v1, rayDir);

                        vertices_tmp.Add(intersectionPoint);

                        vertices_tmp.Add(v2);
                    }
                    //Case 4. Inside -> Outside, save intersection point
                    else if (dist_to_v1 > 0f && dist_to_v2 < 0f)
                    {
                        Vector3 rayDir = (v2 - v1).normalized;

                        Vector3 intersectionPoint = GetRayPlaneIntersectionCoordinate(plane.pos, plane.normal, v1, rayDir);

                        vertices_tmp.Add(intersectionPoint);
                    }
                }

                //Add the new vertices to the list of vertices
                vertices.Clear();

                vertices.AddRange(vertices_tmp);

                vertices_tmp.Clear();
            }

            return vertices;
        }
    }

    public class HalfEdge
    {
        //The vertex the edge points to
        public Vertex v;

        //The face this edge is a part of
        public Triangle t;

        //The next edge
        public HalfEdge nextEdge;
        //The previous
        public HalfEdge prevEdge;
        //The edge going in the opposite direction
        public HalfEdge oppositeEdge;

        //This structure assumes we have a vertex class with a reference to a half edge going from that vertex
        //and a face (triangle) class with a reference to a half edge which is a part of this face 
        public HalfEdge(Vertex v)
        {
            this.v = v;
        }
    }

    public class Vertex
    {
        public Vector3 position;

        //The outgoing halfedge (a halfedge that starts at this vertex)
        //Doesnt matter which edge we connect to it
        public HalfEdge halfEdge;

        //Which triangle is this vertex a part of?
        public Triangle triangle;

        //The previous and next vertex this vertex is attached to
        public Vertex prevVertex;
        public Vertex nextVertex;

        //Properties this vertex may have
        //Reflex is concave
        public bool isReflex;
        public bool isConvex;
        public bool isEar;

        public Vertex(Vector3 position)
        {
            this.position = position;
        }

        //Get 2d pos of this vertex
        public Vector2 GetPos2D_XZ()
        {
            Vector2 pos_2d_xz = new Vector2(position.x, position.z);

            return pos_2d_xz;
        }
    }

    public class Triangle
    {
        //Corners
        public Vertex v1;
        public Vertex v2;
        public Vertex v3;

        //If we are using the half edge mesh structure, we just need one half edge
        public HalfEdge halfEdge;

        public Triangle(Vertex v1, Vertex v2, Vertex v3)
        {
            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;
        }

        public Triangle(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            this.v1 = new Vertex(v1);
            this.v2 = new Vertex(v2);
            this.v3 = new Vertex(v3);
        }

        public Triangle(HalfEdge halfEdge)
        {
            this.halfEdge = halfEdge;
        }

        //Change orientation of triangle from cw -> ccw or ccw -> cw
        public void ChangeOrientation()
        {
            Vertex temp = this.v1;

            this.v1 = this.v2;

            this.v2 = temp;
        }
    }

    public struct LineSegment
    {
        //Start/end coordinates
        public Vector3 p1, p2;

        public LineSegment(Vector3 p1, Vector3 p2)
        {
            this.p1 = p1;
            this.p2 = p2;
        }
    }

    public class Edge
    {
        public Vertex v1;
        public Vertex v2;

        //Is this edge intersecting with another edge?
        public bool isIntersecting = false;

        public Edge(Vertex v1, Vertex v2)
        {
            this.v1 = v1;
            this.v2 = v2;
        }

        public Edge(Vector3 v1, Vector3 v2)
        {
            this.v1 = new Vertex(v1);
            this.v2 = new Vertex(v2);
        }

        //Get vertex in 2d space (assuming x, z)
        public Vector2 GetVertex2D(Vertex v)
        {
            return new Vector2(v.position.x, v.position.z);
        }

        //Flip edge
        public void FlipEdge()
        {
            Vertex temp = v1;

            v1 = v2;

            v2 = temp;
        }
    }

    public class Plane
    {
        public Vector3 pos;

        public Vector3 normal;

        public Plane(Vector3 pos, Vector3 normal)
        {
            this.pos = pos;

            this.normal = normal;
        }
    }

    public class Cell
    {
        public Vector3 cellPos;

        public List<Edge> edges = new List<Edge>();

        //This list should be sorted so we can walk around the cell border
        public List<Vector3> borderCoordinates = new List<Vector3>();

        public Cell(Vector3 cellPos)
        {
            this.cellPos = cellPos;
        }
    }

    public class VoronoiEdge
    {
        //These are the voronoi vertices
        public Vector3 v1;
        public Vector3 v2;

        //All positions within a voronoi cell is closer to this position than any other position in the diagram
        public Vector3 sitePos;

        private List<VoronoiCell> cells = new List<VoronoiCell>();

        public void addCell(VoronoiCell newCell){
            foreach(VoronoiCell cell in cells){
                cell.addNeighbour(newCell);
                newCell.addNeighbour(cell);
            }
        }

        public VoronoiEdge(Vector3 v1, Vector3 v2, Vector3 sitePos)
        {
            this.v1 = v1;
            this.v2 = v2;

            this.sitePos = sitePos;
        }
    }

    public class VoronoiCell
    {
        //All positions within a voronoi cell is closer to this position than any other position in the diagram
        public Vector3 sitePos;

        public Color color;

        public List<VoronoiEdge> edges = new List<VoronoiEdge>();

        public List<Vector3> borderCoordinates = new List<Vector3>();
        private List<VoronoiCell> neighbours = new List<VoronoiCell>();

        public GameObject worldObject;

        public void addNeighbour(VoronoiCell neighbour){
            neighbours.Add(neighbour);
        }

        public VoronoiCell(Vector3 sitePos)
        {
            this.sitePos = sitePos;
        }
    }

}