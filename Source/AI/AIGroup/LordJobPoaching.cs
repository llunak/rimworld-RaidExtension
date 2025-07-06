// ******************************************************************
//       /\ /|       @file       LordJobPoaching.cs
//       \ V/        @brief      集群AI 偷猎
//       | "")       @author     Shadowrabbit, yingtu0401@gmail.com
//       /  |                    
//      /  \\        @Modified   2021-06-17 23:28:46
//    *(__\_\        @Copyright  Copyright (c) 2021, Shadowrabbit
// ******************************************************************
// Modified by llunak, l.lunak@centrum.cz .

using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using System.Collections.Generic;
using System.Linq;

namespace SR.ModRimWorld.RaidExtension
{
    public class LordJobPoaching : LordJobSiegeBase
    {
        private static readonly IntRange WaitTime = new IntRange(500, 1000); //集合等待时间
        private Pawn _targetAnimal; //集群AI想要猎杀的动物

        public Pawn TargetAnimal => _targetAnimal;

        public LordJobPoaching()
        {
        }

        public LordJobPoaching(IntVec3 siegeSpot, Pawn targetAnimal) : base(siegeSpot)
        {
            _targetAnimal = targetAnimal;
        }

        /// <summary>
        /// 序列化
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref _targetAnimal, "_targetAnimal");
        }

        public override StateGraph CreateGraph()
        {
            //集群AI流程状态机
            var stateGraph = new StateGraph();
            //添加流程 集结
            var lordToilStage = new LordToil_Stage(siegeSpot);
            stateGraph.AddToil(lordToilStage);
            //添加流程 偷猎
            var lordToilPoaching = new LordToilPoaching();
            stateGraph.AddToil(lordToilPoaching);
            //添加流程 带着猎物离开
            var lordToilTakePreyExit = new LordToilTakePreyExit();
            stateGraph.AddToil(lordToilTakePreyExit);
            var faction = lord.faction;
            //过渡 集合到开始偷猎
            var transition = new Transition(lordToilStage, lordToilPoaching);
            transition.AddTrigger(new Trigger_TicksPassed(WaitTime.RandomInRange));
            //唤醒成员
            transition.AddPostAction(new TransitionAction_WakeAll());
            stateGraph.AddTransition(transition);
            //过渡 偷猎到带着猎物离开
            var transitionPoachingToTakePreyExit = new Transition(lordToilPoaching, lordToilTakePreyExit);
            //触发条件 目标猎物被击倒
            var triggerHuntDone = new Trigger_Custom( ( TriggerSignal signal ) => IsHuntDone( signal ));
            transitionPoachingToTakePreyExit.AddTrigger(triggerHuntDone);
            transitionPoachingToTakePreyExit.AddPreAction(new TransitionAction_Message(
                "SrTakePreyExit".Translate(faction.def.pawnsPlural.CapitalizeFirst(),
                    faction.Name), MessageTypeDefOf.ThreatSmall));
            stateGraph.AddTransition(transitionPoachingToTakePreyExit);
            // Handle becoming non-hostile (from LordJob_Siege).
            LordToil_ExitMap lordToil_ExitMap = new LordToil_ExitMap(LocomotionUrgency.Jog, canDig: false, interruptCurrentJob: true)
            {
                useAvoidGrid = true
            };
            stateGraph.AddToil(lordToil_ExitMap);
            Transition transitionNonHostile = new Transition(lordToilStage, lordToil_ExitMap);
            transitionNonHostile.AddSource(lordToilPoaching);
            transitionNonHostile.AddSource(lordToilTakePreyExit);
            transitionNonHostile.AddTrigger(new Trigger_BecameNonHostileToPlayer());
            transitionNonHostile.AddPreAction(new TransitionAction_Message(
                "MessageRaidersLeaving".Translate(faction.def.pawnsPlural.CapitalizeFirst(), faction.Name)));
            stateGraph.AddTransition(transitionNonHostile);
            return stateGraph;
        }

        private bool IsHuntDone( TriggerSignal signal )
        {
            if( signal.type != TriggerSignalType.Tick )
                return false;

            const int CheckEveryTicks = 100;
            if( Find.TickManager.TicksGame % CheckEveryTicks != 0 )
                return false;

            if( !TargetAnimal.Dead )
                return false;

            if( lord.ownedPawns == null || lord.ownedPawns.Count <= 0 )
                return false;

            // Check for any similar animals in a radius around the original one.
            // This serves two purposes:
            // - Hunt more similar animals in the pack.
            // - Do not stop the hunt if the pack has gone manhunter.
            HashSet<ThingDef> acceptableDefs = new HashSet<ThingDef>(TargetAnimal.RaceProps.crossAggroWith
                .OrElseEmptyEnumerable().Prepend(TargetAnimal.def));
            foreach (var thing in GenRadial.RadialDistinctThingsAround(TargetAnimal.Position, lord.Map, 20f, true))
            {
                if( !(thing is Pawn animal))
                    continue;

                // Find animals that can aggro together with the original one (Pawn_MindState.GetPackMates()).
                if( animal == TargetAnimal || !acceptableDefs.Contains(animal.def) || animal.Faction != TargetAnimal.Faction )
                    continue;

                if( !lord.ownedPawns[0].IsTargetAnimalValid(animal, MiscDef.MinTargetRequireHealthScale))
                    continue;

                _targetAnimal = animal;
                return false;
            }
            return true;
        }
    }
}
