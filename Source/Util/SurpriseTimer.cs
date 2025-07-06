// Modified by llunak, l.lunak@centrum.cz .

using Verse;
using Verse.AI.Group;
using System;

namespace SR.ModRimWorld.RaidExtension;

public class SurpriseTimer
{
    private int ticksToSurpriseAttack; // <0 - not surprise attack
    private const float surpriseChance = 0.1f;

    public bool IsSurpriseActive => ticksToSurpriseAttack >= 0;

    private static bool debug = false;

    public void InitSurprise( int maxTicks = 60000 )
    {
        bool isSurprise = Rand.Chance(surpriseChance);
        if( !isSurprise )
        {
            ticksToSurpriseAttack = -1;
            return;
        }
        ticksToSurpriseAttack = Math.Min( maxTicks, (int)(2500f * Rand.Range(1f, 4f)));
    }

    public bool ActivateOn( TriggerSignal signal )
    {
        if( signal.type != TriggerSignalType.Tick )
            return false;
        if( debug )
        {
            ticksToSurpriseAttack = 10;
            debug = false;
        }
        if( ticksToSurpriseAttack < 0 )
            return false;
        if( ticksToSurpriseAttack > 0 )
            --ticksToSurpriseAttack;
        return ticksToSurpriseAttack == 0;
    }

    public void Delay( int delayTicks )
    {
        ticksToSurpriseAttack += delayTicks;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref ticksToSurpriseAttack, "ticksToSurpriseAttack");
    }
}
