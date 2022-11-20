﻿using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RW_NodeTree.Patch
{
    [HarmonyPatch(typeof(ThingWithComps))]
    internal static partial class ThingWithComps_Patcher
    {
        [HarmonyPostfix]
        [HarmonyPatch(
            typeof(ThingWithComps),
            "PreApplyDamage"
        )]
        public static void PostThingWithComps_PreApplyDamage(ThingWithComps __instance, ref DamageInfo dinfo, ref bool absorbed)
        {
            if (absorbed)
            {
                return;
            }
            absorbed = ((CompChildNodeProccesser)__instance)?.PostThingWithComps_PreApplyDamage(ref dinfo, absorbed) ?? absorbed;
        }
    }
}

namespace RW_NodeTree
{
    public partial class CompChildNodeProccesser : ThingComp, IThingHolder
    {


        /// <summary>
        /// event proccesser after ThingDef.SpecialDisplayStats
        /// (WARRING!!!: Don't invoke any method if thet will invoke ThingDef.SpecialDisplayStats)
        /// </summary>
        /// <param name="def">ThingDef instance</param>
        /// <param name="req">parm 'req' of ThingDef.SpecialDisplayStats()</param>
        /// <param name="result">result of ThingDef.SpecialDisplayStats</param>
        public bool PostThingWithComps_PreApplyDamage(ref DamageInfo dinfo, bool absorbed)
        {
            foreach (CompBasicNodeComp comp in AllNodeComp)
            {
                try
                {
                    absorbed = comp.internal_PostThingWithComps_PreApplyDamage(ref dinfo, absorbed);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
            return absorbed;
        }
    }
    public abstract partial class CompBasicNodeComp : ThingComp
    {
        protected virtual bool PostThingWithComps_PreApplyDamage(ref DamageInfo dinfo, bool absorbed)
        {
            return absorbed;
        }
        internal bool internal_PostThingWithComps_PreApplyDamage(ref DamageInfo dinfo, bool absorbed) => PostThingWithComps_PreApplyDamage(ref dinfo, absorbed);

    }
}