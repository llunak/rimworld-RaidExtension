﻿// ******************************************************************
//       /\ /|       @file       IncidentWorkerLogging.cs
//       \ V/        @brief      事件 伐木
//       | "")       @author     Shadowrabbit, yingtu0401@gmail.com
//       /  |                    
//      /  \\        @Modified   2021-06-17 19:42:56
//    *(__\_\        @Copyright  Copyright (c) 2021, Shadowrabbit
// ******************************************************************
// Modified by llunak, l.lunak@centrum.cz .

using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using System.Collections.Generic;
using SR.ModRimWorld.RaidExtension.Util;
using Verse;

namespace SR.ModRimWorld.RaidExtension
{
    [UsedImplicitly]
    public class IncidentWorkerLogging : IncidentWorker_RaidEnemy
    {
        static IncidentWorkerLogging()
        {
            HideRaidStrategy.RegisterRaidLikeEvent( typeof(IncidentWorkerLogging));
        }

        /// <summary>
        /// 是否可以生成事件
        /// </summary>
        /// <param name="parms"></param>
        /// <returns></returns>
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!(parms.target is Map map))
            {
                Log.Error($"{MiscDef.LogTag}target must be a map.");
                return false;
            }

            if (HarmonyUtil.IsSOS2SpaceMap(map))
            {
                Log.Error($"{MiscDef.LogTag}target must not be an SOS2 space map.");
                return false;
            }
            var isTreeExist = map.IsTreeExist();
            //没有树 无法触发事件
            if (!isTreeExist)
            {
                Log.Warning($"{MiscDef.LogTag}there is no tree.");
                return false;
            }

            //候选派系列表
            var candidateFactionList = CandidateFactions(parms).ToList();
            return Enumerable.Any(candidateFactionList, faction => faction.HostileTo(Faction.OfPlayer));
        }

        /// <summary>
        /// 派系能否成为资源组
        /// </summary>
        /// <param name="f"></param>
        /// <param name="map"></param>
        /// <param name="desperate"></param>
        /// <returns></returns>
        public override bool FactionCanBeGroupSource(Faction f, IncidentParms parms, bool desperate = false)
        {
            return base.FactionCanBeGroupSource(f, parms, desperate) && f.def.humanlikeFaction && !f.Hidden;
        }

        /// <summary>
        /// 袭击点数
        /// </summary>
        /// <param name="parms"></param>
        protected override void ResolveRaidPoints(IncidentParms parms)
        {
            if (parms.points > MiscDef.MaxThreatPoints)
            {
                parms.points = MiscDef.MaxThreatPoints;
            }
            // If the loggers will need to attack the colony, reduce raid size (same logic as with poaching).
            // It is not known how many pawns will be in this raid at this point (since that depends
            // on raid points), so let's say at least 50 trees is enough.
            if (parms.target is Map map && !map.IsEnoughNonColonyTrees( 50 ))
                parms.points *= 0.8f;
        }

        /// <summary>
        /// 解决突袭策略
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="groupKind"></param>
        public override void ResolveRaidStrategy(IncidentParms parms, PawnGroupKindDef groupKind)
        {
            parms.raidStrategy = RaidStrategyDefOf.SrLogging;
        }

        /// <summary>
        /// 获取信件定义
        /// </summary>
        /// <returns></returns>
        protected override LetterDef GetLetterDef()
        {
            if( HideRaidStrategy.Enabled )
                return base.GetLetterDef();
            return LetterDefOf.ThreatSmall;
        }
    }
}