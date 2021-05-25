﻿using RimWorld;
using RimWorld.BaseGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace KCSG
{
    class SymbolResolver_KCSG_GenerateRoad : SymbolResolver
    {
        public override void Resolve(ResolveParams rp)
        {
            int x = rp.rect.Corners.ElementAt(2).x,
                y = rp.rect.Corners.ElementAt(2).z;
            Map map = BaseGen.globalSettings.map;

            CurrentGenerationOption.doors.RemoveAll(d => !AnyEmptyCellAround(d, CurrentGenerationOption.grid));
            GridUtils.AddRoadToGrid(CurrentGenerationOption.grid, CurrentGenerationOption.doors);

            for (int i = 0; i < CurrentGenerationOption.grid.Length; i++)
            {
                for (int j = 0; j < CurrentGenerationOption.grid[i].Length; j++)
                {
                    IntVec3 cell = new IntVec3(x + i, 0, y - j);
                    switch (CurrentGenerationOption.grid[i][j].Type)
                    {
                        case CellType.ROAD:
                            SpawnConduit(cell, BaseGen.globalSettings.map);
                            GenUtils.GenerateTerrainAt(map, cell, CurrentGenerationOption.settlementLayoutDef.roadDef);
                            break;

                        case CellType.MAINROAD:
                            SpawnConduit(cell, BaseGen.globalSettings.map);
                            GenUtils.GenerateTerrainAt(map, cell, CurrentGenerationOption.settlementLayoutDef.mainRoadDef);
                            break;

                        default:
                            break;
                    }
                }
            }

            rp.rect.EdgeCells.ToList().ForEach(cell => SpawnConduit(cell, map));
        }

        private bool AnyEmptyCellAround(CustomVector c, CustomVector[][] grid)
        {
            if (!IsInBound(c.X - 1, c.Y, grid) || (IsInBound(c.X - 1, c.Y, grid) && grid[(int)c.X - 1][(int)c.Y].IsNoneOrRoad()))
                return true;
            if (!IsInBound(c.X + 1, c.Y, grid) || (IsInBound(c.X + 1, c.Y, grid) && grid[(int)c.X + 1][(int)c.Y].IsNoneOrRoad()))
                return true;
            if (!IsInBound(c.X, c.Y - 1, grid) || (IsInBound(c.X, c.Y - 1, grid) && grid[(int)c.X][(int)c.Y - 1].IsNoneOrRoad()))
                return true;
            if (!IsInBound(c.X, c.Y + 1, grid) || (IsInBound(c.X, c.Y + 1, grid) && grid[(int)c.X][(int)c.Y + 1].IsNoneOrRoad()))
                return true;

            return false;
        }

        private bool IsInBound<T>(double X, double Y, T[][] grid)
        {
            int gridWidth = grid.Length, 
                gridHeight = grid[0].Length;

            if (X < 0)
                return false;
            if (X >= gridWidth)
                return false;
            if (Y < 0)
                return false;
            if (Y >= gridHeight)
                return false;
            return true;
        }

        private void SpawnConduit(IntVec3 cell, Map map)
        {
            if (cell.Walkable(map))
            {
                map.terrainGrid.TerrainAt(cell).affordances.Contains(TerrainAffordanceDefOf.Bridgeable);
                Thing c = ThingMaker.MakeThing(ThingDefOf.PowerConduit);
                c.SetFactionDirect(map.ParentFaction);
                GenSpawn.Spawn(c, cell, map, WipeMode.VanishOrMoveAside);
            }
        }
    }
}
