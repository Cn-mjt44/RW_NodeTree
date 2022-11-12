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
    [HarmonyPatch(typeof(VerbTracker))]
    internal static partial class VerbTracker_Patcher
    {
        [HarmonyPrefix]
        [HarmonyPatch(
            typeof(VerbTracker),
            "CreateVerbTargetCommand",
            typeof(Thing),
            typeof(Verb)
        )]
        private static void PreVerbTracker_CreateVerbTargetCommand(VerbTracker __instance,ref Thing ownerThing, Verb verb)
        {
            CompChildNodeProccesser comp = ownerThing;
            ownerThing = comp?.GetBeforeConvertVerbCorrespondingThing(__instance.directOwner.GetType(), verb, comp.Props.CommandIconUseRedictoryToVerbInstanceSource).Item1 ?? ownerThing;
            //if (Prefs.DevMode) Log.Message(verb + " : " + verb?.EquipmentSource);
        }

        [HarmonyPostfix]
        [HarmonyPatch(
            typeof(VerbTracker),
            "CreateVerbTargetCommand",
            typeof(Thing),
            typeof(Verb)
        )]
        private static void PostVerbTracker_CreateVerbTargetCommand(Thing ownerThing, ref Command_VerbTarget __result)
        {
            if(__result != null)
            {
                __result.icon = (ownerThing?.Graphic?.MatSingleFor(ownerThing)?.mainTexture as Texture2D) ?? __result.icon;
                __result.iconProportions = ownerThing?.Graphic?.drawSize ?? __result.iconProportions;
            }
        }
    }
}
