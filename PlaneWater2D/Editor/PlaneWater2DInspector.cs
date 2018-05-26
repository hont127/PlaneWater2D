using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

using UnityEngine;
using UnityEditor;

using Hont;

[CustomEditor(typeof(PlaneWater2D))]
public class PlaneWater2DInspector : Editor
{
    SerializedProperty mSizeProp;
    SerializedProperty mVertexSegmentProp;
    SerializedProperty mInteractableAreaRangeProp;
    SerializedProperty mBaseTurbulentScaleProp;
    SerializedProperty mHookesLaw_kProp;
    SerializedProperty mHookesLaw_atteProp;
    SerializedProperty mVertexSpreadAtteProp;
    SerializedProperty mVertexSpreadAtte_SecondProp;
    SerializedProperty mVertexSpreadAtte_ToWallProp;
    SerializedProperty mEffectPointListProp;
    SerializedProperty mIsDrawGizmosProp;

    void Awake()
    {
        InitProperties();
    }

    public override void OnInspectorGUI()
    {
        if (mIsDrawGizmosProp == null)
            InitProperties();

        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUI.BeginChangeCheck();
        mIsDrawGizmosProp.boolValue = GUILayout.Toggle(mIsDrawGizmosProp.boolValue, "IsDrawGizmos");
        DrawSplitter();
        EditorGUILayout.PropertyField(mSizeProp);
        EditorGUILayout.PropertyField(mVertexSegmentProp);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Mesh"))
        {
            ClearPlaneMesh();
        }
        if (GUILayout.Button("Update Mesh"))
        {
            UpdatePlaneMesh();
        }
        GUILayout.EndHorizontal();
        DrawSplitter();
        EditorGUILayout.PropertyField(mInteractableAreaRangeProp);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(10f);
        EditorGUILayout.PropertyField(mEffectPointListProp, true);
        EditorGUILayout.EndHorizontal();
        DrawSplitter();
        EditorGUILayout.PropertyField(mBaseTurbulentScaleProp);
        EditorGUILayout.PropertyField(mHookesLaw_kProp);
        EditorGUILayout.PropertyField(mHookesLaw_atteProp);
        EditorGUILayout.PropertyField(mVertexSpreadAtteProp);
        EditorGUILayout.PropertyField(mVertexSpreadAtte_SecondProp);
        EditorGUILayout.PropertyField(mVertexSpreadAtte_ToWallProp);

        if (EditorGUI.EndChangeCheck())
            serializedObject.ApplyModifiedProperties();
        EditorGUILayout.EndVertical();
    }

    string GetFieldPath<T, TValue>(Expression<Func<T, TValue>> expr)
    {
        var me = expr.Body as MemberExpression;

        var members = new List<string>();
        while (me != null)
        {
            members.Add(me.Member.Name);
            me = me.Expression as MemberExpression;
        }

        var sb = new StringBuilder();
        for (int i = members.Count - 1; i >= 0; i--)
        {
            sb.Append(members[i]);
            if (i > 0) sb.Append('.');
        }

        return sb.ToString();
    }

    void DrawSplitter()
    {
        var rect = GUILayoutUtility.GetRect(1f, 1f);
        if (Event.current.type != EventType.Repaint)
            return;

        EditorGUI.DrawRect(rect, !EditorGUIUtility.isProSkin
            ? new Color(0.6f, 0.6f, 0.6f, 1.333f)
            : new Color(0.12f, 0.12f, 0.12f, 1.333f));
    }

    void InitProperties()
    {
        mSizeProp = serializedObject.FindProperty(GetFieldPath((PlaneWater2D c) => c.size));
        mVertexSegmentProp = serializedObject.FindProperty(GetFieldPath((PlaneWater2D c) => c.vertexSegment));
        mInteractableAreaRangeProp = serializedObject.FindProperty(GetFieldPath((PlaneWater2D c) => c.interactableAreaRange));
        mBaseTurbulentScaleProp = serializedObject.FindProperty(GetFieldPath((PlaneWater2D c) => c.baseTurbulentScale));
        mHookesLaw_kProp = serializedObject.FindProperty(GetFieldPath((PlaneWater2D c) => c.hookesLaw_k));
        mHookesLaw_atteProp = serializedObject.FindProperty(GetFieldPath((PlaneWater2D c) => c.hookesLaw_atte));
        mVertexSpreadAtteProp = serializedObject.FindProperty(GetFieldPath((PlaneWater2D c) => c.vertexSpreadAtte));
        mVertexSpreadAtte_SecondProp = serializedObject.FindProperty(GetFieldPath((PlaneWater2D c) => c.vertexSpreadAtte_Second));
        mVertexSpreadAtte_ToWallProp = serializedObject.FindProperty(GetFieldPath((PlaneWater2D c) => c.vertexSpreadAtte_ContactToWall));
        mEffectPointListProp = serializedObject.FindProperty(GetFieldPath((PlaneWater2D c) => c.effectPointList));
        mIsDrawGizmosProp = serializedObject.FindProperty(GetFieldPath((PlaneWater2D c) => c.isDrawGizmos));
    }

    void ClearPlaneMesh()
    {
        var planeWater2D = base.target as PlaneWater2D;

        var meshFilter = planeWater2D.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            if (meshFilter.sharedMesh != null)
                DestroyImmediate(meshFilter.sharedMesh);

            meshFilter.sharedMesh = null;
        }

        var meshRenderer = planeWater2D.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = planeWater2D.gameObject.AddComponent<MeshRenderer>();

        meshRenderer.materials = new Material[0];
    }

    void UpdatePlaneMesh()
    {
        var planeWater2D = base.target as PlaneWater2D;

        var meshFilter = planeWater2D.GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = planeWater2D.gameObject.AddComponent<MeshFilter>();

        var meshRenderer = planeWater2D.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = planeWater2D.gameObject.AddComponent<MeshRenderer>();

        meshRenderer.material = new Material(Shader.Find("Standard"));
        meshFilter.mesh = GetPlaneMesh(planeWater2D.size, planeWater2D.vertexSegment);
    }

    Mesh GetPlaneMesh(Vector2 size, int segment)
    {
        var mesh = new Mesh();
        var triangles = new int[segment * 6];

        for (int i = 0; i < segment; i++)
        {
            var role = segment + 1;
            var self = i;
            var next = i + role;

            triangles[(i * 6) + 0] = self;
            triangles[(i * 6) + 1] = next + 1;
            triangles[(i * 6) + 2] = self + 1;

            triangles[(i * 6) + 3] = self;
            triangles[(i * 6) + 4] = next;
            triangles[(i * 6) + 5] = next + 1;
        }

        var w = size.x / segment;
        var h = size.y / 1;

        var index = 0;
        var vertices = new Vector3[Mathf.FloorToInt((segment + 1) * 2)];
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < segment + 1; j++)
            {
                vertices[index] = new Vector3(-j * w, i * h, 0);
                index++;
            }
        }

        index = 0;
        var uv = new Vector2[(segment + 1) * 2];
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < segment + 1; j++)
            {
                uv[index] = new Vector2(j * (1.0f / segment), i * 1.0f);
                index++;
            }
        }

        var indexList = new List<KeyValuePair<int, Vector3>>();
        for (int i = 0, iMax = vertices.Length; i < iMax; i++)
            indexList.Add(new KeyValuePair<int, Vector3>(i, vertices[i]));

        indexList = indexList.FindAll(m => m.Value.y > 0);

        indexList.Sort((x, y) => x.Value.x.CompareTo(y.Value.x));

        var colors = new Color[(segment + 1) * 2];
        for (int i = 0, iMax = indexList.Count; i < iMax; i++)
        {
            colors[indexList[i].Key] = Color.Lerp(new Color(0.1f, 0.1f, 0.1f, 0.1f), new Color(1, 1, 1, 1), i / (float)indexList.Count);
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.colors = colors;

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return mesh;
    }
}
