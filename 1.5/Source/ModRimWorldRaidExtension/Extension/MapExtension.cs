// ******************************************************************
//       /\ /|       @file       MapExtension.cs
//       \ V/        @brief      地图扩展
//       | "")       @author     Shadowrabbit, yingtu0401@gmail.com
//       /  |                    
//      /  \\        @Modified   2021-07-27 11:46:48
//    *(__\_\        @Copyright  Copyright (c) 2021, Shadowrabbit
// ******************************************************************
// Modified by llunak, l.lunak@centrum.cz .

using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace SR.ModRimWorld.RaidExtension
{
    public static class MapExtension
    {
        /// <summary>
        /// 当前地图上是否存在可以列为目标的动物
        /// </summary>
        /// <param name="map"></param>
        /// <param name="minTargetRequireHealthScale">健康缩放最小需求 用来判断动物强度</param>
        /// <returns></returns>
        public static bool IsAnimalTargetExist(this Map map, float minTargetRequireHealthScale)
        {
            return map.mapPawns != null && Enumerable.Any(map.mapPawns.AllPawnsSpawned,
                (Thing t) => ThingValidator.IsAnimal(t, minTargetRequireHealthScale));
        }

        /// <summary>
        /// 寻找最近的 满足体型的动物
        /// </summary>
        /// <param name="minTargetRequireHealthScale"></param>
        /// <returns></returns>
        public static Pawn FindTargetAnimal(this Map map, IntVec3 position, float minTargetRequireHealthScale)
        {
            //验证器
            bool SpoilValidator(Thing t) => ThingValidator.IsAnimal(t, minTargetRequireHealthScale);

            //找队长身边最近的动物
            var targetThing = GenClosest.ClosestThing_Global_Reachable(position, map,
                map.mapPawns.AllPawnsSpawned, PathEndMode.ClosestTouch,
                TraverseParms.For(TraverseMode.NoPassClosedDoors, Danger.Some), validator: SpoilValidator);

            return (Pawn) targetThing;
        }

        /// <summary>
        /// 当前地图是否存在树
        /// </summary>
        /// <param name="map"></param>
        /// <returns></returns>
        public static bool IsTreeExist(this Map map)
        {
            return map.spawnedThings != null && map.spawnedThings.Any(ThingValidator.IsTree);
        }
    }
}
