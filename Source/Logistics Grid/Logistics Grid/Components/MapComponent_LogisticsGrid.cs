using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Logistics_Grid.Components
{
    internal sealed class MapComponent_LogisticsGrid : MapComponent
    {
        private const int PeriodicRebuildIntervalTicks = 250;
        private const int ProofLogIntervalTicks = 300;

        public bool Dirty = true;

        private List<Building> powerConduits = new List<Building>();
        private List<Building> powerUsers = new List<Building>();
        private List<IntVec3> powerConduitCells = new List<IntVec3>();
        private List<IntVec3> powerUserCells = new List<IntVec3>();
        private List<Building> powerConduitsBack = new List<Building>();
        private List<Building> powerUsersBack = new List<Building>();
        private List<IntVec3> powerConduitCellsBack = new List<IntVec3>();
        private List<IntVec3> powerUserCellsBack = new List<IntVec3>();

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

            List<Thing> allThings = map.listerThings.AllThings;
            List<Thing> artificialBuildings = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
            ThingDef conduitDef = ThingDefOf.PowerConduit;
            for (int i = 0; i < allThings.Count; i++)
            {
                Building building = allThings[i] as Building;
                if (building == null)
                {
                    continue;
                }

                bool isConduit = (building.def.building != null && building.def.building.isPowerConduit)
                    || (conduitDef != null && building.def == conduitDef)
                    || building.def.defName == "PowerConduit";
                if (isConduit)
                {
                    powerConduitsBack.Add(building);
                    powerConduitCellsBack.Add(building.Position);
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
    }
}
