using System.Collections.Generic;
using Logistics_Grid.Framework;
using Verse;

namespace Logistics_Grid.Domains.Power
{
    internal sealed class PowerDomainCache : IUtilityDomainCache
    {
        public const byte NeighborNorth = 1 << 0;
        public const byte NeighborSouth = 1 << 1;
        public const byte NeighborEast = 1 << 2;
        public const byte NeighborWest = 1 << 3;

        private readonly Map map;

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
        private byte[] conduitNeighborMaskGrid = new byte[0];

        public PowerDomainCache(Map map)
        {
            this.map = map;
            Dirty = true;
        }

        public bool Dirty { get; set; }

        public int PrimaryCount => PowerConduitCount;
        public int SecondaryCount => PowerUserCount;
        public string PrimaryLabel => "conduits";
        public string SecondaryLabel => "users";

        public int PowerConduitCount { get; private set; }
        public int PowerUserCount { get; private set; }

        public List<IntVec3> PowerConduitCells => powerConduitCells;
        public List<IntVec3> PowerUserCells => powerUserCells;

        public void PrepareForRebuild()
        {
            powerConduitsBack.Clear();
            powerConduitCellsBack.Clear();
            powerUsersBack.Clear();
            powerUserCellsBack.Clear();

            EnsureGridSize();
            ClearCurrentConduitsFromGrid();
        }

        public void FinalizeRebuild()
        {
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

        public void AddConduit(Building building, PowerConduitType conduitType)
        {
            if (building == null)
            {
                return;
            }

            IntVec3 cell = building.Position;
            powerConduitsBack.Add(building);
            powerConduitCellsBack.Add(cell);

            SetConduitPresence(cell, true);
            SetConduitType(cell, conduitType == PowerConduitType.None ? PowerConduitType.Standard : conduitType);
        }

        public void AddPowerUser(Building building)
        {
            if (building == null)
            {
                return;
            }

            powerUsersBack.Add(building);
            foreach (IntVec3 cell in building.OccupiedRect().Cells)
            {
                powerUserCellsBack.Add(cell);
            }
        }

        public void RebuildNeighborMasks()
        {
            for (int i = 0; i < powerConduitCellsBack.Count; i++)
            {
                IntVec3 cell = powerConduitCellsBack[i];
                byte neighborMask = 0;

                if (HasConduitAt(cell + IntVec3.North))
                {
                    neighborMask |= NeighborNorth;
                }

                if (HasConduitAt(cell + IntVec3.South))
                {
                    neighborMask |= NeighborSouth;
                }

                if (HasConduitAt(cell + IntVec3.East))
                {
                    neighborMask |= NeighborEast;
                }

                if (HasConduitAt(cell + IntVec3.West))
                {
                    neighborMask |= NeighborWest;
                }

                SetNeighborMask(cell, neighborMask);
            }
        }

        public bool HasConduitAt(IntVec3 cell)
        {
            if (!cell.InBounds(map))
            {
                return false;
            }

            int cellIndex = map.cellIndices.CellToIndex(cell);
            return cellIndex >= 0
                && cellIndex < conduitPresenceGrid.Length
                && conduitPresenceGrid[cellIndex];
        }

        public PowerConduitType GetConduitTypeAt(IntVec3 cell)
        {
            if (!cell.InBounds(map))
            {
                return PowerConduitType.None;
            }

            int cellIndex = map.cellIndices.CellToIndex(cell);
            if (cellIndex < 0 || cellIndex >= conduitTypeGrid.Length)
            {
                return PowerConduitType.None;
            }

            return (PowerConduitType)conduitTypeGrid[cellIndex];
        }

        public byte GetNeighborMaskAt(IntVec3 cell)
        {
            if (!cell.InBounds(map))
            {
                return 0;
            }

            int cellIndex = map.cellIndices.CellToIndex(cell);
            if (cellIndex < 0 || cellIndex >= conduitNeighborMaskGrid.Length)
            {
                return 0;
            }

            return conduitNeighborMaskGrid[cellIndex];
        }

        public static int CountNeighbors(byte neighborMask)
        {
            int neighborCount = 0;
            if ((neighborMask & NeighborNorth) != 0) neighborCount++;
            if ((neighborMask & NeighborSouth) != 0) neighborCount++;
            if ((neighborMask & NeighborEast) != 0) neighborCount++;
            if ((neighborMask & NeighborWest) != 0) neighborCount++;
            return neighborCount;
        }

        private void EnsureGridSize()
        {
            int cellCount = map.cellIndices.NumGridCells;
            if (conduitPresenceGrid.Length != cellCount)
            {
                conduitPresenceGrid = new bool[cellCount];
                conduitTypeGrid = new byte[cellCount];
                conduitNeighborMaskGrid = new byte[cellCount];
            }
        }

        private void ClearCurrentConduitsFromGrid()
        {
            for (int i = 0; i < powerConduitCells.Count; i++)
            {
                IntVec3 cell = powerConduitCells[i];
                int cellIndex = map.cellIndices.CellToIndex(cell);
                if (cellIndex < 0 || cellIndex >= conduitPresenceGrid.Length)
                {
                    continue;
                }

                conduitPresenceGrid[cellIndex] = false;
                conduitTypeGrid[cellIndex] = (byte)PowerConduitType.None;
                conduitNeighborMaskGrid[cellIndex] = 0;
            }
        }

        private void SetConduitPresence(IntVec3 cell, bool hasConduit)
        {
            int cellIndex = map.cellIndices.CellToIndex(cell);
            if (cellIndex >= 0 && cellIndex < conduitPresenceGrid.Length)
            {
                conduitPresenceGrid[cellIndex] = hasConduit;
            }
        }

        private void SetConduitType(IntVec3 cell, PowerConduitType conduitType)
        {
            int cellIndex = map.cellIndices.CellToIndex(cell);
            if (cellIndex >= 0 && cellIndex < conduitTypeGrid.Length)
            {
                conduitTypeGrid[cellIndex] = (byte)conduitType;
            }
        }

        private void SetNeighborMask(IntVec3 cell, byte neighborMask)
        {
            int cellIndex = map.cellIndices.CellToIndex(cell);
            if (cellIndex >= 0 && cellIndex < conduitNeighborMaskGrid.Length)
            {
                conduitNeighborMaskGrid[cellIndex] = neighborMask;
            }
        }
    }
}
