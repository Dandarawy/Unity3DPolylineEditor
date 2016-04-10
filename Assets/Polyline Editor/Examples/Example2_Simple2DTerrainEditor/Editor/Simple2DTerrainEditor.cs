using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using Examples_Simple2DTerrainEditor;
[CustomEditor(typeof(Simple2DTerrain))]
public class Simple2DTerrainEditor : Editor
{
    Texture nodeTexture;
    //int indexToDelete;
    static GUIStyle handleStyle = new GUIStyle();
    void OnEnable()
    {
        nodeTexture = Resources.Load<Texture>("Handle");
        if (nodeTexture == null) nodeTexture = EditorGUIUtility.whiteTexture;
        handleStyle.alignment = TextAnchor.MiddleCenter;
        handleStyle.fixedWidth = 15;
        handleStyle.fixedHeight = 15;
    }
    void OnSceneGUI()
    {
        Simple2DTerrain polyline = (target as Simple2DTerrain);
        Vector3[] localPoints = polyline.nodes.ToArray();
        Vector3[] worldPoints = new Vector3[polyline.nodes.Count];
        for (int i = 0; i < worldPoints.Length; i++)
            worldPoints[i] = polyline.transform.TransformPoint(localPoints[i]);
        DrawPolyLine(worldPoints);
        DrawNodes(polyline, worldPoints);
        if (Event.current.shift)
        {
            //Adding Points
            Vector3 mousePos = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).origin;
            Vector3 polyLocalMousePos = polyline.transform.InverseTransformPoint(mousePos);
            Vector3 nodeOnPoly = HandleUtility.ClosestPointToPolyLine(worldPoints);
            float handleSize = HandleUtility.GetHandleSize(nodeOnPoly);
            int nodeIndex = FindNodeIndex(worldPoints, nodeOnPoly);
            Handles.DrawLine(worldPoints[nodeIndex - 1], mousePos);
            Handles.DrawLine(worldPoints[nodeIndex], mousePos);
            if (Handles.Button(mousePos, Quaternion.identity, handleSize * 0.09f, handleSize, HandleFunc))
            {
                polyLocalMousePos.z = 0;
                Undo.RecordObject(polyline, "Insert Node");
                polyline.nodes.Insert(nodeIndex, polyLocalMousePos);
                ///////////////////////////////////////
                UpdateTerrain(polyline.nodes.ToArray());
                ///////////////////////////////////////
                Event.current.Use();
            }
        }
        if (Event.current.control)
        {
            //Deleting Points
            int indexToDelete = FindNearestNodeToMouse(worldPoints);
            Handles.color = Color.red;
            float handleSize = HandleUtility.GetHandleSize(worldPoints[0]);
            if (Handles.Button(worldPoints[indexToDelete], Quaternion.identity, handleSize * 0.09f, handleSize, DeleteHandleFunc))
            {
                Undo.RecordObject(polyline, "Remove Node");
                polyline.nodes.RemoveAt(indexToDelete);
                ///////////////////////////////////////
                UpdateTerrain(polyline.nodes.ToArray());
                ///////////////////////////////////////
                indexToDelete = -1;
                Event.current.Use();
            }
            Handles.color = Color.white;
        }

    }
    private int FindNearestNodeToMouse(Vector3[] worldNodesPositions)
    {
        Vector3 mousePos = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).origin;
        mousePos.z = 0;
        int index = -1;
        float minDistnce = float.MaxValue;
        for (int i = 0; i < worldNodesPositions.Length; i++)
        {
            float distance = Vector3.Distance(worldNodesPositions[i], mousePos);
            if (distance < minDistnce)
            {
                index = i;
                minDistnce = distance;
            }
        }
        return index;
    }
    private int FindNodeIndex(Vector3[] worldNodesPositions, Vector3 newNode)
    {
        float smallestdis = float.MaxValue;
        int prevIndex = 0;
        for (int i = 1; i < worldNodesPositions.Length; i++)
        {
            float distance = HandleUtility.DistanceToPolyLine(worldNodesPositions[i - 1], worldNodesPositions[i]);
            if (distance < smallestdis)
            {
                prevIndex = i - 1;
                smallestdis = distance;
            }
        }
        return prevIndex + 1;
    }
    private static void DrawPolyLine(Vector3[] nodes)
    {
        if (Event.current.shift) Handles.color = Color.green;
        else if (Event.current.control) Handles.color = Color.red;
        else Handles.color = Color.white;
        Handles.DrawPolyLine(nodes);
        Handles.color = Color.white;
    }
    private void DrawNodes(Simple2DTerrain polyline, Vector3[] worldPoints)
    {
        for (int i = 0; i < polyline.nodes.Count; i++)
        {
            Vector3 pos = polyline.transform.TransformPoint(polyline.nodes[i]);
            float handleSize = HandleUtility.GetHandleSize(pos);
            Vector3 newPos = Handles.FreeMoveHandle(pos, Quaternion.identity, handleSize * 0.09f, Vector3.one, HandleFunc);
            List<Vector3> alignTo;

            //if (currentControlID == GUIUtility.hotControl)
            //{
            //    if (CheckAlignment(worldPoints, handleSize * 0.1f, i, ref newPos, out alignTo))
            //    {
            //        Handles.color = Color.green;
            //        for (int j = 0; j < alignTo.Count; j++)
            //            Handles.DrawPolyLine(newPos, alignTo[j]);
            //        Handles.color = Color.white;
            //    }
            //}
            if (newPos != pos)
            {
                if (CheckAlignment(worldPoints, handleSize * 0.1f, i, ref newPos, out alignTo))
                {
                    Handles.color = Color.green;
                    for (int j = 0; j < alignTo.Count; j++)
                        Handles.DrawPolyLine(newPos, alignTo[j]);
                    Handles.color = Color.white;
                }
                Undo.RecordObject(polyline, "Move Node");
                polyline.nodes[i] = polyline.transform.InverseTransformPoint(newPos);
                ///////////////////////////////////////
                UpdateTerrain(polyline.nodes.ToArray());
                ///////////////////////////////////////
            }
        }
    }

    private void UpdateTerrain(Vector3[] localPoints)
    {
        if (localPoints.Length < 3) return;
        List<Vector3> vertices = new List<Vector3>(localPoints);
        Triangulator triangulator = new Triangulator(vertices.ToArray());
        int[] indecies = triangulator.Triangulate();
        Simple2DTerrain terrain = target as Simple2DTerrain;
        MeshFilter meshFilter = terrain.GetComponent<MeshFilter>();
        Mesh mesh = meshFilter.sharedMesh;
        mesh.triangles = null;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = indecies;
        mesh.uv = Vec3ToVec2Array(vertices.ToArray());

        PolygonCollider2D collider= terrain.GetComponent<PolygonCollider2D>();
        collider.points= Vec3ToVec2Array(vertices.ToArray());
    }

    private Vector2[] Vec3ToVec2Array(Vector3[] data)
    {
        Vector2[] result = new Vector2[data.Length];
        for (int i = 0; i < data.Length; i++)
            result[i] = data[i];
        return result;
    }

    bool CheckAlignment(Vector3[] worldNodes, float offset, int index, ref Vector3 position, out List<Vector3> alignedTo)
    {
        //Debug.Log("Check aligmnet with index:" + index);
        //check vertical
        //check with the prev node
        bool aligned = false;
        //the node can be aligned to the prev and next node at once, we need to return more than one alginedTo Node
        alignedTo = new List<Vector3>(2);
        if (index > 0)
        {
            float dx = Mathf.Abs(worldNodes[index - 1].x - position.x);
            if (dx < offset)
            {
                position.x = worldNodes[index - 1].x;
                alignedTo.Add(worldNodes[index - 1]);
                aligned = true;
            }
        }
        //check with the next node
        if (index < worldNodes.Length - 1)
        {
            float dx = Mathf.Abs(worldNodes[index + 1].x - position.x);
            if (dx < offset)
            {
                position.x = worldNodes[index + 1].x;
                alignedTo.Add(worldNodes[index + 1]);
                aligned = true;
            }
        }
        //check horizontal
        if (index > 0)
        {
            float dy = Mathf.Abs(worldNodes[index - 1].y - position.y);
            if (dy < offset)
            {
                position.y = worldNodes[index - 1].y;
                alignedTo.Add(worldNodes[index - 1]);
                aligned = true;
            }
        }
        //check with the next node
        if (index < worldNodes.Length - 1)
        {
            float dy = Mathf.Abs(worldNodes[index + 1].y - position.y);
            if (dy < offset)
            {
                position.y = worldNodes[index + 1].y;
                alignedTo.Add(worldNodes[index + 1]);
                aligned = true;
            }
        }


        //check straight lines
        //To be implemented


        return aligned;
    }
    void HandleFunc(int controlID, Vector3 position, Quaternion rotation, float size)
    {
        Vector3 o1 = Camera.current.ScreenToWorldPoint(Vector3.zero);
        Vector3 o2 = Camera.current.ScreenToWorldPoint(new Vector2(4, 4));
        Vector3 offset = (o1 - o2) / 2;
        if (controlID == GUIUtility.hotControl)
            GUI.color = Color.red;
        else
            GUI.color = Color.green;
        Handles.Label(position - offset, new GUIContent(nodeTexture), handleStyle);
        GUI.color = Color.white;
    }
    void DeleteHandleFunc(int controlID, Vector3 position, Quaternion rotation, float size)
    {
        Vector3 o1 = Camera.current.ScreenToWorldPoint(Vector3.zero);
        Vector3 o2 = Camera.current.ScreenToWorldPoint(new Vector2(4, 4));
        Vector3 offset = (o1 - o2) / 2;
        GUI.color = Color.red;
        Handles.Label(position - offset, new GUIContent(nodeTexture), handleStyle);
        GUI.color = Color.white;
    }
}
