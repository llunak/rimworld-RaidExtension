// ******************************************************************
//       /\ /|       @file       LordJobLogging.cs
//       \ V/        @brief      集群AI 伐木
//       | "")       @author     Shadowrabbit, yingtu0401@gmail.com
//       /  |                    
//      /  \\        @Modified   2021-06-17 23:45:54
//    *(__\_\        @Copyright  Copyright (c) 2021, Shadowrabbit
// ******************************************************************
// Modified by llunak, l.lunak@centrum.cz .

using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using System.Collections.Generic;

namespace SR.ModRimWorld.RaidExtension
{
    public class LordJobLogging : LordJobSiegeBase
    {
        private const int ExitTime = 2500 * 4; //离开时间
        private const int WaitTime = 500; //集合等待时间
        private int numTreesCut = 0;
        private Dictionary< Pawn, Pawn > pawnsToFollowCache = new Dictionary< Pawn, Pawn >();
        private Dictionary< Pawn, ( Thing, int ) > treeForPawnCache = new Dictionary< Pawn, ( Thing, int ) >(); // int = tick

        public LordJobLogging()
        {
        }

        public LordJobLogging(IntVec3 siegeSpot) : base(siegeSpot)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref numTreesCut, "numTreesCut");
        }

        public override StateGraph CreateGraph()
        {
            //集群AI流程状态机
            var stateGraph = new StateGraph();
            //添加流程 集结
            var lordToilStage = new LordToil_Stage(siegeSpot);
            stateGraph.AddToil(lordToilStage);
            //添加流程 伐木
            var lordToilLogging = new LordToilLogging();
            stateGraph.AddToil(lordToilLogging);
            //添加流程 带着木材离开
            var lordToilTakeWoodExit = new LordToilTakeWoodExit();
            stateGraph.AddToil(lordToilTakeWoodExit);
            var faction = lord.faction;
            //过渡 集合到开始伐木
            var transition = new Transition(lordToilStage, lordToilLogging);
            transition.AddTrigger(new Trigger_TicksPassed(WaitTime));
            //唤醒成员
            transition.AddPostAction(new TransitionAction_WakeAll());
            stateGraph.AddTransition(transition);
            //过渡 伐木到带着木材离开
            var transitionLoggingToTakeWoodExit = new Transition(lordToilLogging, lordToilTakeWoodExit);
            var triggerTicksPassed = new Trigger_TicksPassed(ExitTime);
            var triggerTreesCut = new Trigger_TickCondition( () => IsEnoughTreesCut(), 250 );
            transitionLoggingToTakeWoodExit.AddTrigger(triggerTicksPassed);
            transitionLoggingToTakeWoodExit.AddTrigger(triggerTreesCut);
            transitionLoggingToTakeWoodExit.AddPreAction(new TransitionAction_Message(
                "SrTakeWoodExit".Translate(faction.def.pawnsPlural.CapitalizeFirst(),
                    faction.Name), MessageTypeDefOf.ThreatSmall));
            stateGraph.AddTransition(transitionLoggingToTakeWoodExit);
            // Handle becoming non-hostile (from LordJob_Siege).
            LordToil_ExitMap lordToil_ExitMap = new LordToil_ExitMap(LocomotionUrgency.Jog, canDig: false, interruptCurrentJob: true)
            {
                useAvoidGrid = true
            };
            stateGraph.AddToil(lordToil_ExitMap);
            Transition transitionNonHostile = new Transition(lordToilStage, lordToil_ExitMap);
            transitionNonHostile.AddSource(lordToilLogging);
            transitionNonHostile.AddSource(lordToilTakeWoodExit);
            transitionNonHostile.AddTrigger(new Trigger_BecameNonHostileToPlayer());
            transitionNonHostile.AddPreAction(new TransitionAction_Message(
                "MessageRaidersLeaving".Translate(faction.def.pawnsPlural.CapitalizeFirst(), faction.Name)));
            stateGraph.AddTransition(transitionNonHostile);
            return stateGraph;
        }

        public void NotifyTreeCut()
        {
            ++numTreesCut;
        }

        public bool IsEnoughTreesCut()
        {
            return lord.ownedPawns.Count > 0 && numTreesCut > lord.ownedPawns.Count / 2;
        }

        public Pawn GetPawnToFollow( Pawn pawn )
        {
            if( pawnsToFollowCache.TryGetValue( pawn, out Pawn pawnToFollow ))
                return pawnToFollow;
            return null;
        }

        public void SetPawnToFollow( Pawn pawn, Pawn pawnToFollow )
        {
            pawnsToFollowCache[ pawn ] = pawnToFollow;
        }

        public ( Thing, int ) GetTreeForPawn( Pawn pawn )
        {
            if( treeForPawnCache.TryGetValue( pawn, out var value ))
                return value;
            return ( null, 0 );
        }

        public void SetTreeForPawn( Pawn pawn, Thing tree, int tick )
        {
            treeForPawnCache[ pawn ] = ( tree, tick );
        }
    }
}
