using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
[System.Serializable]
public abstract class Effect 
{
    public abstract GameAction GetGameAction(List<CombatantView> targets, CombatantView caster);
}
