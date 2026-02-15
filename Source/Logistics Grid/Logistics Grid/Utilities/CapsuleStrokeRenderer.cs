using System;
using System.Collections.Generic;
using Logistics_Grid.Framework;
using UnityEngine;
using Verse;

namespace Logistics_Grid.Utilities
{
    internal enum CapsuleDirection : byte
    {
        East = 0,
        West = 1,
        North = 2,
        South = 3
    }

    internal readonly struct CapsuleStrokeStyle
    {
        public CapsuleStrokeStyle(
            float strokeWidth,
            float strokeAltitude,
            float alpha,
            int capSegments = 8,
            float capRadius = -1f,
            float capAltitude = -1f,
            float hubAltitude = -1f)
        {
            StrokeWidth = strokeWidth;
            StrokeAltitude = strokeAltitude;
            Alpha = Mathf.Clamp01(alpha);
            CapSegments = Math.Max(3, capSegments);
            CapRadius = capRadius > 0f ? capRadius : strokeWidth * 0.5f;
            CapAltitude = capAltitude > 0f ? capAltitude : strokeAltitude + 0.0001f;
            HubAltitude = hubAltitude > 0f ? hubAltitude : strokeAltitude + 0.0002f;
        }

        public float StrokeWidth { get; }
        public float StrokeAltitude { get; }
        public float CapRadius { get; }
        public float CapAltitude { get; }
        public float HubAltitude { get; }
        public float Alpha { get; }
        public int CapSegments { get; }
    }

    internal sealed class CapsuleStrokeRenderer
    {
        private readonly CapsuleStrokeStyle style;

        private readonly List<Matrix4x4>[] pathMatricesByBucket;
        private readonly List<Matrix4x4>[] capMatricesByBucket;
        private readonly List<Matrix4x4>[] hubMatricesByBucket;

        private readonly CapStripSpec[] capStripSpecs;
        private readonly HubStripSpec[] hubStripSpecs;

        private struct CapStripSpec
        {
            public float ForwardCenter;
            public float ForwardThickness;
            public float HalfTangent;
        }

        private struct HubStripSpec
        {
            public float OffsetX;
            public float ThicknessX;
            public float HalfZ;
        }

        public CapsuleStrokeRenderer(int bucketCount, CapsuleStrokeStyle style)
        {
            this.style = style;
            pathMatricesByBucket = CreateBucketLists(bucketCount, 1024);
            capMatricesByBucket = CreateBucketLists(bucketCount, 1024);
            hubMatricesByBucket = CreateBucketLists(bucketCount, 512);
            capStripSpecs = BuildCapStripSpecs(style.CapRadius, style.CapSegments);
            hubStripSpecs = BuildHubStripSpecs(style.CapRadius, style.CapSegments * 2);
        }

        public void Clear()
        {
            ClearBucketLists(pathMatricesByBucket);
            ClearBucketLists(capMatricesByBucket);
            ClearBucketLists(hubMatricesByBucket);
        }

        public void AddCardinalSegment(Vector3 startCenter, CapsuleDirection direction, int bucketIndex, bool capAtStart, bool capAtEnd)
        {
            if (!TryGetBucket(pathMatricesByBucket, bucketIndex, out List<Matrix4x4> pathBucket)
                || !TryGetBucket(capMatricesByBucket, bucketIndex, out List<Matrix4x4> capBucket))
            {
                return;
            }

            Vector3 segmentCenter = startCenter;
            Vector3 scale = default(Vector3);
            CapsuleDirection startCapDirection = CapsuleDirection.West;
            CapsuleDirection endCapDirection = CapsuleDirection.East;

            switch (direction)
            {
                case CapsuleDirection.East:
                    segmentCenter += new Vector3(0.5f, 0f, 0f);
                    scale = new Vector3(1f, 1f, style.StrokeWidth);
                    startCapDirection = CapsuleDirection.West;
                    endCapDirection = CapsuleDirection.East;
                    break;
                case CapsuleDirection.West:
                    segmentCenter += new Vector3(-0.5f, 0f, 0f);
                    scale = new Vector3(1f, 1f, style.StrokeWidth);
                    startCapDirection = CapsuleDirection.East;
                    endCapDirection = CapsuleDirection.West;
                    break;
                case CapsuleDirection.North:
                    segmentCenter += new Vector3(0f, 0f, 0.5f);
                    scale = new Vector3(style.StrokeWidth, 1f, 1f);
                    startCapDirection = CapsuleDirection.South;
                    endCapDirection = CapsuleDirection.North;
                    break;
                case CapsuleDirection.South:
                    segmentCenter += new Vector3(0f, 0f, -0.5f);
                    scale = new Vector3(style.StrokeWidth, 1f, 1f);
                    startCapDirection = CapsuleDirection.North;
                    endCapDirection = CapsuleDirection.South;
                    break;
            }

            pathBucket.Add(BuildMatrix(segmentCenter, scale, style.StrokeAltitude));
            if (capAtStart)
            {
                AddCap(capBucket, startCenter, startCapDirection);
            }

            if (capAtEnd)
            {
                AddCap(capBucket, GetCardinalNeighborCenter(startCenter, direction), endCapDirection);
            }
        }

        public void AddHub(Vector3 center, int bucketIndex)
        {
            if (!TryGetBucket(hubMatricesByBucket, bucketIndex, out List<Matrix4x4> hubBucket))
            {
                return;
            }

            for (int i = 0; i < hubStripSpecs.Length; i++)
            {
                HubStripSpec strip = hubStripSpecs[i];
                Vector3 stripCenter = center + new Vector3(strip.OffsetX, 0f, 0f);
                Vector3 stripScale = new Vector3(strip.ThicknessX, 1f, strip.HalfZ * 2f);
                hubBucket.Add(BuildMatrix(stripCenter, stripScale, style.HubAltitude));
            }
        }

        public void Flush(Map map, Func<int, Material> materialForBucket)
        {
            if (map == null || materialForBucket == null)
            {
                return;
            }

            for (int i = 0; i < pathMatricesByBucket.Length; i++)
            {
                List<Matrix4x4> pathBucket = pathMatricesByBucket[i];
                List<Matrix4x4> capBucket = capMatricesByBucket[i];
                List<Matrix4x4> hubBucket = hubMatricesByBucket[i];

                if (pathBucket.Count == 0 && capBucket.Count == 0 && hubBucket.Count == 0)
                {
                    continue;
                }

                Material material = materialForBucket(i);
                if (material == null)
                {
                    continue;
                }

                DrawBucket(pathBucket, material, map);
                DrawBucket(capBucket, material, map);
                DrawBucket(hubBucket, material, map);
            }
        }

        public Color ApplyAlpha(Color color)
        {
            color.a = style.Alpha;
            return color;
        }

        private void AddCap(List<Matrix4x4> capBucket, Vector3 endpointCenter, CapsuleDirection capDirection)
        {
            for (int i = 0; i < capStripSpecs.Length; i++)
            {
                CapStripSpec strip = capStripSpecs[i];
                Vector3 stripCenter = endpointCenter;
                Vector3 stripScale = default(Vector3);

                switch (capDirection)
                {
                    case CapsuleDirection.East:
                        stripCenter += new Vector3(strip.ForwardCenter, 0f, 0f);
                        stripScale = new Vector3(strip.ForwardThickness, 1f, strip.HalfTangent * 2f);
                        break;
                    case CapsuleDirection.West:
                        stripCenter += new Vector3(-strip.ForwardCenter, 0f, 0f);
                        stripScale = new Vector3(strip.ForwardThickness, 1f, strip.HalfTangent * 2f);
                        break;
                    case CapsuleDirection.North:
                        stripCenter += new Vector3(0f, 0f, strip.ForwardCenter);
                        stripScale = new Vector3(strip.HalfTangent * 2f, 1f, strip.ForwardThickness);
                        break;
                    case CapsuleDirection.South:
                        stripCenter += new Vector3(0f, 0f, -strip.ForwardCenter);
                        stripScale = new Vector3(strip.HalfTangent * 2f, 1f, strip.ForwardThickness);
                        break;
                }

                capBucket.Add(BuildMatrix(stripCenter, stripScale, style.CapAltitude));
            }
        }

        private static Vector3 GetCardinalNeighborCenter(Vector3 startCenter, CapsuleDirection direction)
        {
            switch (direction)
            {
                case CapsuleDirection.East:
                    return startCenter + new Vector3(1f, 0f, 0f);
                case CapsuleDirection.West:
                    return startCenter + new Vector3(-1f, 0f, 0f);
                case CapsuleDirection.North:
                    return startCenter + new Vector3(0f, 0f, 1f);
                case CapsuleDirection.South:
                    return startCenter + new Vector3(0f, 0f, -1f);
                default:
                    return startCenter;
            }
        }

        private static Matrix4x4 BuildMatrix(Vector3 center, Vector3 scale, float altitude)
        {
            center.y = altitude;
            return Matrix4x4.TRS(center, Quaternion.identity, scale);
        }

        private static void DrawBucket(List<Matrix4x4> matrices, Material material, Map map)
        {
            for (int i = 0; i < matrices.Count; i++)
            {
                Graphics.DrawMesh(MeshPool.plane10, matrices[i], material, 0);
                UtilityOverlayProfiler.RecordDrawSubmission(map, 1);
            }
        }

        private static bool TryGetBucket(List<Matrix4x4>[] buckets, int bucketIndex, out List<Matrix4x4> bucket)
        {
            bucket = null;
            if (bucketIndex < 0 || bucketIndex >= buckets.Length)
            {
                return false;
            }

            bucket = buckets[bucketIndex];
            return true;
        }

        private static List<Matrix4x4>[] CreateBucketLists(int bucketCount, int capacity)
        {
            int normalizedBucketCount = Math.Max(1, bucketCount);
            List<Matrix4x4>[] buckets = new List<Matrix4x4>[normalizedBucketCount];
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i] = new List<Matrix4x4>(capacity);
            }

            return buckets;
        }

        private static void ClearBucketLists(List<Matrix4x4>[] buckets)
        {
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i].Clear();
            }
        }

        private static CapStripSpec[] BuildCapStripSpecs(float radius, int segments)
        {
            CapStripSpec[] specs = new CapStripSpec[segments];
            for (int i = 0; i < segments; i++)
            {
                float u0 = i / (float)segments;
                float u1 = (i + 1) / (float)segments;
                float mid = (u0 + u1) * 0.5f;
                float thickness = (u1 - u0) * radius;
                float halfTangent = Mathf.Sqrt(Mathf.Max(0f, 1f - (mid * mid))) * radius;

                specs[i] = new CapStripSpec
                {
                    ForwardCenter = mid * radius,
                    ForwardThickness = thickness,
                    HalfTangent = halfTangent
                };
            }

            return specs;
        }

        private static HubStripSpec[] BuildHubStripSpecs(float radius, int strips)
        {
            int normalizedStrips = Math.Max(6, strips);
            HubStripSpec[] specs = new HubStripSpec[normalizedStrips];
            for (int i = 0; i < normalizedStrips; i++)
            {
                float v0 = -1f + (2f * i / normalizedStrips);
                float v1 = -1f + (2f * (i + 1) / normalizedStrips);
                float mid = (v0 + v1) * 0.5f;
                float thickness = (v1 - v0) * radius;
                float halfZ = Mathf.Sqrt(Mathf.Max(0f, 1f - (mid * mid))) * radius;

                specs[i] = new HubStripSpec
                {
                    OffsetX = mid * radius,
                    ThicknessX = thickness,
                    HalfZ = halfZ
                };
            }

            return specs;
        }
    }
}
