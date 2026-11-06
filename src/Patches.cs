using System;
using System.Collections.Generic;
using HarmonyLib;

namespace Oddhatter.TameFollow
{
    /// <summary>
    /// Vanilla Tameable.Interact on a tamed creature calls Command (follow / stay) only when m_commandable is set,
    /// which is true for wolves and false for most other tames, so they just get petted. The command itself
    /// (RPC_Command, run by whoever's game owns the creature) only needs a MonsterAI, and has no commandable check.
    /// So making the creature commandable for the length of this one Interact call is enough, and it works on a
    /// server where nobody else has the mod.
    /// </summary>
    [HarmonyPatch(typeof(Tameable), nameof(Tameable.Interact))]
    internal static class Tameable_Interact_Patch
    {
        /// <summary>Creature prefabs already logged, so each type is reported once.</summary>
        private static readonly HashSet<string> s_logged = new HashSet<string>();

        /// <summary>__state: the creature's own m_commandable, or null when this call didn't change it.</summary>
        private static void Prefix(Tameable __instance, bool alt, out bool? __state)
        {
            __state = null;
            if (!TameFollowPlugin.Enabled.Value || alt || __instance.m_commandable || !__instance.IsTamed())
            {
                return;
            }
            string creature = Utils.GetPrefabName(__instance.gameObject);
            if (__instance.m_monsterAI == null)
            {
                // e.g. a creature driven by AnimalAI: it has no follow behaviour, so it keeps being petted.
                if (s_logged.Add(creature))
                {
                    TameFollowPlugin.Log.LogInfo($"{creature} can't follow (no MonsterAI); it is petted as normal.");
                }
                return;
            }
            if (s_logged.Add(creature))
            {
                TameFollowPlugin.Log.LogInfo($"{creature} can now be told to follow / stay.");
            }
            __state = __instance.m_commandable;
            __instance.m_commandable = true;
        }

        /// <summary>Restores the creature's setting even if Interact throws.</summary>
        private static Exception Finalizer(Tameable __instance, bool? __state, Exception __exception)
        {
            if (__state.HasValue && __instance != null)
            {
                __instance.m_commandable = __state.Value;
            }
            return __exception;
        }
    }
}
