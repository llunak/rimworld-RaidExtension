// ******************************************************************
//       /\ /|       @file       RaidStrategyWorkerPoaching.cs
//       \ V/        @brief      袭击策略 偷猎
//       | "")       @author     Shadowrabbit, yingtu0401@gmail.com
//       /  |                    
//      /  \\        @Modified   2021-06-17 19:01:36
//    *(__\_\        @Copyright  Copyright (c) 2021, Shadowrabbit
// ******************************************************************
// Modified by llunak, l.lunak@centrum.cz .

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace SR.ModRimWorld.RaidExtension
{
    [UsedImplicitly]
    public class RaidStrategyWorkerPoaching : RaidStrategyWorker
    {
        public Pawn TempAnimal { get; set; } //目标动物

        /// <summary>
        /// 该策略适用于
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="groupKind"></param>
        /// <returns></returns>
        public override bool CanUseWith(IncidentParms parms, PawnGroupKindDef groupKind)
        {
            return base.CanUseWith(parms, groupKind) && parms.faction != null && parms.faction != Faction.OfMechanoids;
        }

        public override bool CanUsePawnGenOption(float pointsTotal, PawnGenOption g, List<PawnGenOptionWithXenotype> chosenGroups, Faction faction = null)
        {
            if( !g.kind.RaceProps.Humanlike )
                return false;
            // Unfortunately CanUsePawn() cannot filter out pawns with poor weapons on its own,
            // because it's called repeatedly until it either succeeds or until it's ignored.
            // So if the pawn kind allows only poor weapons, one will be selected in the end anyway.
            // Try to filter out such pawn kinds, even if guessing just based on weapon tags doesn't work very well.
            bool possiblyHasSuitable = false;
            foreach( string weaponTag in g.kind.weaponTags )
            {
                if( !weaponTag.Contains( "Grenade" ) && !weaponTag.Contains( "Melee" )
                    && !weaponTag.Contains( "GunSingleUse" ) && !weaponTag.Contains( "Flame" )
                    && !weaponTag.Contains( "Tox" ))
                {
                    possiblyHasSuitable = true;
                    break;
                }
            }
            if( !possiblyHasSuitable )
                return false;
            return base.CanUsePawnGenOption( pointsTotal, g, chosenGroups, faction );
        }

        public override bool CanUsePawn(float pointsTotal, Pawn p, List<Pawn> otherPawns)
        {
            if( !base.CanUsePawn(pointsTotal, p, otherPawns))
                return false;
            // Avoid poorly suitable weapons (melee, non-lethal, too dangerous).
            ThingDef weaponDef = p.equipment.Primary?.def;
            if( weaponDef == null )
                return false;
            if( !weaponDef.IsRangedWeapon )
                return false;
            if( weaponDef.weaponTags.Contains("GunSingleUse")) // Dangerous guns such as rocket launchers.
                return false;
            DamageDef damage = p.equipment.PrimaryEq.PrimaryVerb.GetDamageDef();
            if( damage == DamageDefOf.ToxGas
                || damage == DamageDefOf.Smoke
                || damage == DamageDefOf.NerveStun
                || damage == DamageDefOf.EMP
                || damage == DamageDefOf.Stun
                || damage == DamageDefOf.Flame
                || damage == DamageDefOf.Bomb)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 创建集群AI工作
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="map"></param>
        /// <param name="pawns"></param>
        /// <param name="raidSeed"></param>
        /// <returns></returns>
        protected override LordJob MakeLordJob(IncidentParms parms, Map map, List<Pawn> pawns, int raidSeed)
        {
            var siegePositionFrom =
                RCellFinder.FindSiegePositionFrom(parms.spawnCenter.IsValid ? parms.spawnCenter : pawns[0].PositionHeld,
                    map);
            return new LordJobPoaching(siegePositionFrom, TempAnimal);
        }

        /// <summary>
        /// 生成角色
        /// </summary>
        /// <param name="parms"></param>
        /// <returns></returns>
        public override List<Pawn> SpawnThreats(IncidentParms parms)
        {
            var pawnGroupMakerParms =
                IncidentParmsUtility.GetDefaultPawnGroupMakerParms(PawnGroupKindDefOf.Combat, parms);
            var pawnList = PawnGroupMakerUtility.GeneratePawns(pawnGroupMakerParms).ToList();
            if (pawnList.Count == 0)
            {
                return pawnList;
            }

            parms.raidArrivalMode.Worker.Arrive(pawnList, parms);
            return pawnList;
        }
    }
}