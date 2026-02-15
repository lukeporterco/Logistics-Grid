using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Logistics_Grid.Components
{
    internal sealed class MapComponent_LogisticsGrid : MapComponent
    {
        public enum ConduitType : byte
        {
            None = 0,
            Standard = 1,
            Hidden = 2,
            Waterproof = 3
        }

        private const int PeriodicRebuildIntervalTicks = 250;
        private const int ProofLogIntervalTicks = 300;
        private const string WaterproofConduitDefName = "WaterproofConduit";

        public bool Dirty = true;

        private List<Building> powerConduits = new List<Building>();
        private List<Building> powerUsers = new List<Building>();
        private List<IntVec3> powerConduitCells = new List<IntVec3>();
        private List<IntVec3> powerUserCells = new List<IntVec3>();
        private List<Building> powerConduitsBack = new List<Building>();
        private List<Building> powerUsersBack = new List<Building>();
        private List<IntVec3> powerConduitCellsBack = new List<IntVec3>();
        private List<IntVec3> powerUserCellsBack = new List<IntVec3>();
        private bool[] conduitPresenceGrid = new bool[0];
        private byte[] conduitTypeGrid = new byte[0];

        public List<Building> PowerConduits => powerConduits;
        public List<IntVec3> PowerConduitCells => powerConduitCells;
        public List<Building> PowerUsers => powerUsers;
        public List<IntVec3> PowerUserCells => powerUserCells;
        public int PowerConduitCount;
        public int PowerUserCount;

        private int lastRebuildTick = -1;
        private int lastProofLogTick = -1;

        public MapComponent_LogisticsGrid(Map map) : base(map)
        {
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            Dirty = true;
        }

        public override void MapComponentTick()
        {
            int ticksGame = Find.TickManager.TicksGame;
            if (Dirty || lastRebuildTick < 0 || ticksGame - lastRebuildTick >= PeriodicRebuildIntervalTicks)
            {
                RebuildCaches();
                lastRebuildTick = ticksGame;
            }

            if (Prefs.DevMode && ticksGame - lastProofLogTick >= ProofLogIntervalTicks)
            {
                Log.Message($"[Logistics Grid] Power cache proof: map={map.Index} conduits={PowerConduitCount} users={PowerUserCount} dirty={Dirty}");
                lastProofLogTick = ticksGame;
            }
        }

        public void MarkDirty()
        {
            Dirty = true;
        }

        public void RebuildCaches()
        {
            powerConduitsBack.Clear();
            powerConduitCellsBack.Clear();
            powerUsersBack.Clear();
            powerUserCellsBack.Clear();
            EnsureConduitPresenceGridSize();
            ClearConduitPresenceGrid();
            EnsureConduitTypeGridSize();
            ClearConduitTypeGrid();

            List<Thing> allThings = map.listerThings.AllThings;
            List<Thing> artificialBuildings = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
            ThingDef conduitDef = ThingDefOf.PowerConduit;
            ThingDef hiddenConduitDef = ThingDefOf.HiddenConduit;
            for (int i = 0; i < allThings.Count; i++)
            {
                Building building = allThings[i] as Building;
                if (building == null)
                {
                    continue;
                }

                ThingDef buildingDef = building.def;
                bool isStandardConduit = conduitDef != null && buildingDef == conduitDef;
                bool isHiddenConduit = hiddenConduitDef != null && buildingDef == hiddenConduitDef;
                bool isWaterproofConduit = buildingDef != null && buildingDef.defName == WaterproofConduitDefName;
                bool isConduit = (buildingDef.building != null && buildingDef.building.isPowerConduit)
                    || isStandardConduit
                    || isHiddenConduit
                    || isWaterproofConduit
                    || buildingDef.defName == "PowerConduit";
                if (isConduit)
                {
                    powerConduitsBack.Add(building);
                    powerConduitCellsBack.Add(building.Position);
                    SetConduitPresence(building.Position);
                    SetConduitType(building.Position, ResolveConduitType(isStandardConduit, isHiddenConduit, isWaterproofConduit));
                }

                if (building.TryGetComp<CompPowerTrader>() != null)
                {
                    powerUsersBack.Add(building);
                }
            }

            for (int i = 0; i < artificialBuildings.Count; i++)
            {
                Building building = artificialBuildings[i] as Building;
                if (building == null || building.TryGetComp<CompPowerTrader>() == null)
                {
                    continue;
                }

                foreach (IntVec3 cell in building.OccupiedRect().Cells)
                {
                    powerUserCellsBack.Add(cell);
                }
            }

            List<Building> powerConduitsSwap = powerConduits;
            powerConduits = powerConduitsBack;
            powerConduitsBack = powerConduitsSwap;

            List<IntVec3> powerConduitCellsSwap = powerConduitCells;
            powerConduitCells = powerConduitCellsBack;
            powerConduitCellsBack = powerConduitCellsSwap;

            List<Building> powerUsersSwap = powerUsers;
            powerUsers = powerUsersBack;
            powerUsersBack = powerUsersSwap;

            List<IntVec3> powerUserCellsSwap = powerUserCells;
            powerUserCells = powerUserCellsBack;
            powerUserCellsBack = powerUserCellsSwap;

            PowerConduitCount = powerConduits.Count;
            PowerUserCount = powerUsers.Count;
            Dirty = false;
        }

        public bool HasConduitAt(IntVec3 cell)
        {
            if (!cell.InBounds(map))
            {
                return false;
            }

            int cellIndex = map.cellIndices.CellToIndex(cell);
            return cellIndex >= 0 && cellIndex < conduitPresenceGrid.Length && conduitPresenceGrid[cellIndex];
        }

        public ConduitType GetConduitTypeAt(IntVec3 cell)
        {
            if (!cell.InBounds(map))
            {
                return ConduitType.None;
            }

            int cellIndex = map.cellIndices.CellToIndex(cell);
            if (cellIndex < 0 || cellIndex >= conduitTypeGrid.Length)
            {
                return ConduitType.None;
            }

            return (ConduitType)conduitTypeGrid[cellIndex];
        }

        private void EnsureConduitPresenceGridSize()
        {
            int cellCount = map.cellIndices.NumGridCells;
            if (conduitPresenceGrid.Length != cellCount)
            {
                conduitPresenceGrid = new bool[cellCount];
            }
        }

        private void EnsureConduitTypeGridSize()
        {
            int cellCount = map.cellIndices.NumGridCells;
            if (conduitTypeGrid.Length != cellCount)
            {
                conduitTypeGrid = new byte[cellCount];
            }
        }

        private void ClearConduitPresenceGrid()
        {
            for (int i = 0; i < conduitPresenceGrid.Length; i++)
            {
                conduitPresenceGrid[i] = false;
            }
        }

        private void ClearConduitTypeGrid()
        {
            for (int i = 0; i < conduitTypeGrid.Length; i++)
            {
                conduitTypeGrid[i] = (byte)ConduitType.None;
            }
        }

        private void SetConduitPresence(IntVec3 cell)
        {
            int cellIndex = map.cellIndices.CellToIndex(cell);
            if (cellIndex >= 0 && cellIndex < conduitPresenceGrid.Length)
            {
                conduitPresenceGrid[cellIndex] = true;
            }
        }

        private void SetConduitType(IntVec3 cell, ConduitType conduitType)
        {
            int cellIndex = map.cellIndices.CellToIndex(cell);
            if (cellIndex >= 0 && cellIndex < conduitTypeGrid.Length)
            {
                conduitTypeGrid[cellIndex] = (byte)conduitType;
            }
        }

        private static ConduitType ResolveConduitType(bool isStandardConduit, bool isHiddenConduit, bool isWaterproofConduit)
        {
            if (isHiddenConduit)
            {
                return ConduitType.Hidden;
            }

            if (isWaterproofConduit)
            {
                return ConduitType.Waterproof;
            }

            if (isStandardConduit)
            {
                return ConduitType.Standard;
            }

            return ConduitType.Standard;
        }
    }
}
