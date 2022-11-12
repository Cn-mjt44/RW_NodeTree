﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RW_NodeTree.Patch
{
    [HarmonyPatch(typeof(Verb))]
    internal static class Verb_Patcher
    {


        [HarmonyPostfix]
        [HarmonyPatch(
            typeof(Verb),
            "get_DirectOwner"
        )]
        private static void PostVerb_DirectOwner(Verb __instance, ref IVerbOwner __result)
        {
            Thing thing = (__result as Thing) ?? (__result as ThingComp)?.parent;
            CompChildNodeProccesser compChild = thing ?? (thing?.ParentHolder as CompChildNodeProccesser);
            if (compChild != null && compChild.Props.VerbDirectOwnerRedictory)
            {
                thing = compChild.GetBeforeConvertVerbCorrespondingThing(__result.GetType(), __instance, true).Item1;
                __result = CompChildNodeProccesser.GetSameTypeVerbOwner(__result.GetType(), thing);
            }
        }



        [HarmonyPostfix]
        [HarmonyPatch(
            typeof(Verb),
            "get_EquipmentSource"
        )]
        private static void PostVerb_EquipmentSource(Verb __instance, ref ThingWithComps __result)
        {
            IVerbOwner directOwner = __instance.verbTracker.directOwner;
            Thing thing = (directOwner as Thing) ?? (directOwner as ThingComp)?.parent;
            CompChildNodeProccesser compChild = thing ?? (thing?.ParentHolder as CompChildNodeProccesser);
            if (compChild != null && compChild.Props.VerbEquipmentSourceRedictory)
            {
                __result = (compChild.GetBeforeConvertVerbCorrespondingThing(__instance.verbTracker.directOwner.GetType(), __instance).Item1 as ThingWithComps) ?? __result;
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(
            typeof(Verb),
            "get_UIIcon"
        )]
        private static void PostVerb_UIIcon(Verb __instance, ref Texture2D __result)
        {
            Thing EquipmentSource = __instance.EquipmentSource;
            __result = (EquipmentSource?.Graphic?.MatSingleFor(EquipmentSource)?.mainTexture as Texture2D) ?? __result;
        }
    }
}
