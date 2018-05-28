using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hont
{
    using EffectPoint = PlaneWater2D_EffectPoint;

    public class PlaneWater2D : MonoBehaviour
    {
        class MassPoint
        {
            public int VertexIndex { get; set; }
            public Vector3 BaseVertexPosition { get; set; }
            public float Velocity { get; set; }
            public float BaseTurbulent { get; set; }
            public float CurrentForce { get; set; }

            public float FinalForce { get { return CurrentForce + BaseTurbulent; } }

            public MassPoint LeftMassPoint;
            public MassPoint RightMassPoint;
        }

        public Vector2 size = new Vector2(1, 1);
        [Tooltip("The mesh vertex segment number")]
        public int vertexSegment = 20;
        public bool invertNormal;
        [Tooltip("The effect point interactable range")]
        public float interactableAreaRange = 0.2f;
        [Tooltip("At the calm state, water turbulent size scale.")]
        public float baseTurbulentScale = 1f;
        [Tooltip("The k value effect the stiffness")]
        public float hookesLaw_k = 39f;
        [Tooltip("The k value effect the attenuation")]
        public float hookesLaw_atte = 10f;
        [Tooltip("Vertex first spread attenuation")]
        public float vertexSpreadAtte = 0.8f;
        [Tooltip("Vertex second spread attenuation")]
        public float vertexSpreadAtte_Second = 0.31f;
        [Tooltip("Vertex contact to wall spread attenuation")]
        public float vertexSpreadAtte_ContactToWall = 0.8f;
        public List<EffectPoint> effectPointList;
        public bool cameraInvisibleOptimize = true;
        public bool isDrawGizmos = true;

        bool mOnBecameVisible;
        Rect mInteractArea;
        List<MassPoint> mMassPointList;
        Vector3[] mCacheVerticesArray;
        Mesh mCurrentMesh;


        void Awake()
        {
            mMassPointList = new List<MassPoint>();
            mCurrentMesh = GetComponent<MeshFilter>().sharedMesh;
            mCacheVerticesArray = mCurrentMesh.vertices;

            InitInteractArea();
            InitMassPoints();
        }

        void Update()
        {
            if (cameraInvisibleOptimize && !mOnBecameVisible) return;

            UpdateEffectPoints();

            UpdateMassPoint();
        }

        void OnBecameVisible()
        {
            mOnBecameVisible = true;
        }

        void OnBecameInvisible()
        {
            mOnBecameVisible = false;
        }

        void OnDrawGizmos()
        {
            if (!isDrawGizmos) return;

            InitInteractArea();

            var cacheColor = Gizmos.color;

            var rect = new Rect();
            rect.xMin = transform.position.x;
            rect.yMin = transform.position.y;
            rect.xMax = transform.position.x - size.x * transform.localScale.x;
            rect.yMax = transform.position.y + size.y * transform.localScale.y;

            Gizmos.DrawLine(new Vector3(rect.x, rect.y, transform.position.z), new Vector3(rect.xMax, rect.y, transform.position.z));
            Gizmos.DrawLine(new Vector3(rect.xMax, rect.y, transform.position.z), new Vector3(rect.xMax, rect.yMax, transform.position.z));
            Gizmos.DrawLine(new Vector3(rect.xMax, rect.yMax, transform.position.z), new Vector3(rect.x, rect.yMax, transform.position.z));
            Gizmos.DrawLine(new Vector3(rect.x, rect.yMax, transform.position.z), new Vector3(rect.x, rect.y, transform.position.z));

            Gizmos.color = Color.blue;

            Gizmos.DrawLine(new Vector3(mInteractArea.x, mInteractArea.y, transform.position.z), new Vector3(mInteractArea.xMax, mInteractArea.y, transform.position.z));
            Gizmos.DrawLine(new Vector3(mInteractArea.xMax, mInteractArea.y, transform.position.z), new Vector3(mInteractArea.xMax, mInteractArea.yMax, transform.position.z));
            Gizmos.DrawLine(new Vector3(mInteractArea.xMax, mInteractArea.yMax, transform.position.z), new Vector3(mInteractArea.x, mInteractArea.yMax, transform.position.z));
            Gizmos.DrawLine(new Vector3(mInteractArea.x, mInteractArea.yMax, transform.position.z), new Vector3(mInteractArea.x, mInteractArea.y, transform.position.z));

            Gizmos.color = cacheColor;
        }

        void UpdateEffectPoints()
        {
            for (int i = 0, iMax = effectPointList.Count; i < iMax; i++)
            {
                var effectPoint = effectPointList[i];

                if (mInteractArea.Contains(new Vector2(effectPoint.Point.position.x, effectPoint.Point.position.y), true))
                {
                    if (effectPoint.InteractFlag)
                        continue;

                    Debug.Log("!!");

                    var minimumDistance = float.MaxValue;
                    var targetMassPoint = default(MassPoint);
                    for (int j = 0, jMax = mMassPointList.Count; j < jMax; j++)
                    {
                        var massPoint = mMassPointList[j];

                        var worldVertexPosition = transform.localToWorldMatrix.MultiplyPoint3x4(mCacheVerticesArray[massPoint.VertexIndex]);

                        var distance = Vector3.Distance(worldVertexPosition, effectPoint.Point.position);
                        if (distance < minimumDistance)
                        {
                            minimumDistance = distance;
                            targetMassPoint = massPoint;
                        }
                    }

                    if (targetMassPoint != null)
                    {
                        targetMassPoint.Velocity = effectPoint.TakeForce;
                    }

                    effectPoint.InteractFlag = true;
                }
                else
                {
                    effectPoint.InteractFlag = false;
                }
            }
        }

        void UpdateMassPoint()
        {
            for (int i = 0, iMax = mMassPointList.Count; i < iMax; i++)
            {
                var item = mMassPointList[i];

                if (item.CurrentForce > 0)
                {
                    UpdateForceSpread(item, item.CurrentForce, 1);
                }
            }//更新力的传递

            for (int i = 0, iMax = mMassPointList.Count; i < iMax; i++)
            {
                var item = mMassPointList[i];

                item.BaseTurbulent = (Mathf.Cos(i + 2f + Time.time * 6) * 0.002f + Mathf.Sin(i * 3f + Time.time * 9) * 0.003f) * baseTurbulentScale;
            }//基本乱流

            for (int i = 0, iMax = mMassPointList.Count; i < iMax; i++)
            {
                var item = mMassPointList[i];

                var distance = item.CurrentForce;
                var k = Time.deltaTime * hookesLaw_k;
                var f = -distance * k;
                item.Velocity += f;
                item.Velocity = Mathf.Lerp(item.Velocity, 0, Time.deltaTime * hookesLaw_atte);
                item.CurrentForce += item.Velocity;
            }//速率更新

            for (int i = 0, iMax = mMassPointList.Count; i < iMax; i++)
            {
                var basePosition = mMassPointList[i].BaseVertexPosition;
                mCacheVerticesArray[mMassPointList[i].VertexIndex] = basePosition + new Vector3(0, mMassPointList[i].FinalForce, 0);
            }//传递顶点

            mCurrentMesh.vertices = mCacheVerticesArray;
        }

        void UpdateForceSpread(MassPoint massPoint, float force, int deep)
        {
            if (deep > 1) return;

            if (massPoint.LeftMassPoint != null)
            {
                massPoint.LeftMassPoint.Velocity += force * vertexSpreadAtte_Second;
                UpdateForceSpread(massPoint.LeftMassPoint, massPoint.LeftMassPoint.Velocity, ++deep);
            }
            else
            {
                UpdateForceSpread(massPoint.RightMassPoint, force * vertexSpreadAtte_ContactToWall, deep);
            }

            if (massPoint.RightMassPoint != null)
            {
                massPoint.RightMassPoint.Velocity += force * vertexSpreadAtte_Second;
                UpdateForceSpread(massPoint.RightMassPoint, massPoint.RightMassPoint.Velocity, ++deep);
            }
            else
            {
                UpdateForceSpread(massPoint.LeftMassPoint, force * vertexSpreadAtte_ContactToWall, deep);
            }
        }

        void InitInteractArea()
        {
            mInteractArea.xMin = transform.position.x;
            mInteractArea.yMin = transform.position.y + size.y * transform.localScale.y - interactableAreaRange * 0.5f;
            mInteractArea.xMax = mInteractArea.xMin - size.x * transform.localScale.x;
            mInteractArea.yMax = mInteractArea.yMin + interactableAreaRange;

            mInteractArea.yMax = mInteractArea.yMin + mInteractArea.height * transform.localScale.y;
        }

        void InitMassPoints()
        {
            var meshColors = mCurrentMesh.colors;

            var indexList = new List<KeyValuePair<int, Color>>();
            for (int i = 0, iMax = meshColors.Length; i < iMax; i++)
                indexList.Add(new KeyValuePair<int, Color>(i, meshColors[i]));

            indexList = indexList.FindAll(m => m.Value.a > 0);
            indexList.Sort((x, y) => x.Value.a.CompareTo(y.Value.a));

            for (int i = 0, iMax = indexList.Count; i < iMax; i++)
            {
                mMassPointList.Add(new MassPoint() { VertexIndex = indexList[i].Key, BaseVertexPosition = mCacheVerticesArray[indexList[i].Key] });
            }

            for (int i = 0, iMax = mMassPointList.Count; i < iMax; i++)
            {
                if (i - 1 > -1)
                    mMassPointList[i].RightMassPoint = mMassPointList[i - 1];

                if (i + 1 < mMassPointList.Count)
                    mMassPointList[i].LeftMassPoint = mMassPointList[i + 1];
            }
        }
    }
}

