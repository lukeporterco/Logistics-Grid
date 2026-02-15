using UnityEngine;
using Verse;

namespace Logistics_Grid.Framework
{
    internal static class UtilityOverlayProfiler
    {
        private static int lastFrame = -1;
        private static int lastMapIndex = -1;
        private static int frameDrawSubmissions;
        private static int frameDrawMeshes;

        public static void BeginFrame(Map map)
        {
            if (map == null)
            {
                return;
            }

            int frame = Time.frameCount;
            if (frame == lastFrame && map.Index == lastMapIndex)
            {
                return;
            }

            lastFrame = frame;
            lastMapIndex = map.Index;
            frameDrawSubmissions = 0;
            frameDrawMeshes = 0;
        }

        public static void RecordDrawSubmission(Map map, int meshCount)
        {
            if (map == null)
            {
                return;
            }

            BeginFrame(map);
            frameDrawSubmissions++;
            frameDrawMeshes += meshCount;
        }

        public static int GetCurrentFrameDrawSubmissions(Map map)
        {
            if (!IsCurrentFrameForMap(map))
            {
                return 0;
            }

            return frameDrawSubmissions;
        }

        public static int GetCurrentFrameDrawMeshes(Map map)
        {
            if (!IsCurrentFrameForMap(map))
            {
                return 0;
            }

            return frameDrawMeshes;
        }

        private static bool IsCurrentFrameForMap(Map map)
        {
            return map != null
                && lastFrame == Time.frameCount
                && lastMapIndex == map.Index;
        }
    }
}
