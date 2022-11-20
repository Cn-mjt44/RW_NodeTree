﻿using HarmonyLib;
using Mono.Unix.Native;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RW_NodeTree.Patch
{

    [HarmonyPatch(typeof(Pawn))]
    internal static partial class Pawn_Patcher
    {
        [HarmonyPostfix]
        [HarmonyPatch(
            typeof(Pawn),
            "GetGizmos"
        )]
        private static void PostPawn_GetGizmos(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            __result = PerAndPostFixFor_Pawn_GetGizmos(__instance, __result) ?? __result;
        }

        private static IEnumerable<Gizmo> PerAndPostFixFor_Pawn_GetGizmos(Pawn instance, IEnumerable<Gizmo> result)
        {
            ThingOwner list = instance.equipment?.GetDirectlyHeldThings();
            List<(Thing, List<Tool>, List<VerbProperties>)> state = null;
            if (list != null)
            {
                state = new List<(Thing, List<Tool>, List<VerbProperties>)>(list.Count);
                foreach (Thing thing in list)
                {
                    state.Add((thing, thing.def.tools, ThingDef_verbs(thing.def)));
                    try
                    {
                        List<Verb> verbs = thing.TryGetComp<CompEquippable>().AllVerbs;
                        ThingDef_verbs(thing.def) = new List<VerbProperties>();
                        thing.def.tools = new List<Tool>();
                        foreach (Verb verb in verbs)
                        {
                            if (verb.tool == null) ThingDef_verbs(thing.def).Add(verb.verbProps);
                            else thing.def.tools.Add(verb.tool);
                        }
                    }
                    catch(Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                }
            }

            foreach (Gizmo gizmo in result) yield return gizmo;

            if(state != null)
            {
                foreach ((Thing thing, List<Tool> tools, List<VerbProperties> verbs) in state)
                {
                    thing.def.tools = tools;
                    ThingDef_verbs(thing.def) = verbs;
                }
            }
        }

        private static AccessTools.FieldRef<ThingDef, List<VerbProperties>> ThingDef_verbs = AccessTools.FieldRefAccess<ThingDef, List<VerbProperties>>("verbs");
    }
}
