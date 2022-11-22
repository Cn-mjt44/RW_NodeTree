﻿using HarmonyLib;
using RW_NodeTree.Tools;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace RW_NodeTree.Patch
{
    internal static partial class IVerbOwner_Patcher
    {
        private static readonly MethodInfo _PreIVerbOwner_GetVerbProperties = typeof(IVerbOwner_Patcher).GetMethod("PreIVerbOwner_GetVerbProperties", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo _PreIVerbOwner_GetTools = typeof(IVerbOwner_Patcher).GetMethod("PreIVerbOwner_GetTools", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo _PostIVerbOwner_GetVerbProperties = typeof(IVerbOwner_Patcher).GetMethod("PostIVerbOwner_GetVerbProperties", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo _PostIVerbOwner_GetTools = typeof(IVerbOwner_Patcher).GetMethod("PostIVerbOwner_GetTools", BindingFlags.NonPublic | BindingFlags.Static);

        private static readonly Dictionary<Type, MethodInfo> MethodInfo_get_VerbProperties_OfType = new Dictionary<Type, MethodInfo>();
        private static readonly Dictionary<Type, MethodInfo> MethodInfo_get_Tools_OfType = new Dictionary<Type, MethodInfo>();

        private static MethodInfo GetMethodInfo_get_VerbProperties_OfType(Type type)
        {
            MethodInfo result;
            if (!MethodInfo_get_VerbProperties_OfType.TryGetValue(type, out result))
            {
                MethodInfo_get_VerbProperties_OfType.Add(
                    type,
                    result = type.GetMethod("get_VerbProperties", BindingFlags.Public | BindingFlags.Instance)
                );
            }
            return result;
        }
        private static MethodInfo GetMethodInfo_get_Tools_OfType(Type type)
        {
            MethodInfo result;
            if (!MethodInfo_get_Tools_OfType.TryGetValue(type, out result))
            {
                MethodInfo_get_Tools_OfType.Add(
                    type,
                    result = type.GetMethod("get_Tools", BindingFlags.Public | BindingFlags.Instance)
                );
            }
            return result;
        }



        private static void PreIVerbOwner_GetVerbProperties(IVerbOwner __instance, MethodInfo __originalMethod, ref (Dictionary<string ,object>, CompChildNodeProccesser, Type) __state)
        {
            Type type = __instance.GetType();
            CompChildNodeProccesser proccesser = (((__instance) as ThingComp)?.parent) ?? ((__instance) as Thing);
            if (proccesser!= null &&
                __originalMethod.DeclaringType
                ==
                GetMethodInfo_get_VerbProperties_OfType(type).DeclaringType
            )
            {
                __state.Item1 = new Dictionary<string ,object>();
                __state.Item2 = proccesser;
                __state.Item3 = type;
                proccesser.PreIVerbOwner_GetVerbProperties(type, __state.Item1);
            }
        }

        private static void PreIVerbOwner_GetTools(IVerbOwner __instance, MethodInfo __originalMethod, ref (Dictionary<string, object>, CompChildNodeProccesser, Type) __state)
        {
            Type type = __instance.GetType();
            CompChildNodeProccesser proccesser = (((__instance) as ThingComp)?.parent) ?? ((__instance) as Thing);
            if (proccesser != null &&
                __originalMethod.DeclaringType
                ==
                GetMethodInfo_get_Tools_OfType(type).DeclaringType
            )
            {
                __state.Item1 = new Dictionary<string, object>();
                __state.Item2 = proccesser;
                __state.Item3 = type;
                proccesser.PreIVerbOwner_GetTools(type, __state.Item1);
            }
        }
        private static void PostIVerbOwner_GetVerbProperties(ref List<VerbProperties> __result, (Dictionary<string, object>, CompChildNodeProccesser, Type) __state)
        {
            (Dictionary<string, object> forPostRead, CompChildNodeProccesser proccess, Type type) = __state;
            if (forPostRead != null &&
                proccess != null &&
                type != null
            )
            {
                __result = (__result != null) ? new List<VerbProperties>(__result) : new List<VerbProperties>();
                __result = proccess.PostIVerbOwner_GetVerbProperties(type, __result, forPostRead) ?? __result;
            }
        }

        private static void PostIVerbOwner_GetTools(ref List<Tool> __result, (Dictionary<string, object>, CompChildNodeProccesser, Type) __state)
        {
            (Dictionary<string, object> forPostRead, CompChildNodeProccesser proccesser, Type type) = __state;
            if (forPostRead != null &&
                proccesser != null &&
                type != null
            )
            {
                __result = (__result != null) ? new List<Tool>(__result) : new List<Tool>();
                __result = proccesser.PostIVerbOwner_GetTools(type, __result, forPostRead) ?? __result;
            }
        }

        public static void PatchIVerbOwner(Type type, Harmony patcher)
        {
            if (typeof(IVerbOwner).IsAssignableFrom(type))
            {
                MethodInfo _get_VerbProperties = GetMethodInfo_get_VerbProperties_OfType(type);
                if (_get_VerbProperties?.DeclaringType == type && _get_VerbProperties.HasMethodBody())
                {
                    patcher.Patch(_get_VerbProperties, new HarmonyMethod(_PreIVerbOwner_GetVerbProperties), new HarmonyMethod(_PostIVerbOwner_GetVerbProperties));
                    //if (Prefs.DevMode) Log.Message(type + "::" + _get_VerbProperties + " PatchSuccess\n");
                }

                MethodInfo _get_Tools = GetMethodInfo_get_Tools_OfType(type);
                if (_get_Tools?.DeclaringType == type && _get_Tools.HasMethodBody())
                {
                    patcher.Patch(_get_Tools, new HarmonyMethod(_PreIVerbOwner_GetTools), new HarmonyMethod(_PostIVerbOwner_GetTools));
                    //if (Prefs.DevMode) Log.Message(type + "::" + _get_Tools + " PatchSuccess\n");
                }
            }
        }
    }
}

namespace RW_NodeTree
{
    /// <summary>
    /// Node function proccesser
    /// </summary>
    public partial class CompChildNodeProccesser : ThingComp, IThingHolder
    {


        /// <summary>
        /// event proccesser before IVerbOwner.VerbProperties
        /// (WARRING!!!: Don't invoke any method if thet will invoke IVerbOwner.VerbProperties)
        /// </summary>
        /// <param name="owner">IVerbOwner source</param>
        /// <param name="verbProperties">result of IVerbOwner.VerbProperties</param>
        internal void PreIVerbOwner_GetVerbProperties(Type ownerType, Dictionary<string, object> forPostRead)
        {
            if (ownerType != null && typeof(IVerbOwner).IsAssignableFrom(ownerType))
            {
                foreach (CompBasicNodeComp comp in AllNodeComp)
                {
                    try
                    {
                        comp.internal_PreIVerbOwner_GetVerbProperties(ownerType, forPostRead);
                    }
                    catch(Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                }
            }
            return;
        }

        /// <summary>
        /// event proccesser before IVerbOwner.VerbProperties
        /// (WARRING!!!: Don't invoke any method if thet will invoke IVerbOwner.Tools)
        /// </summary>
        /// <param name="owner">IVerbOwner source</param>
        /// <param name="verbProperties">result of IVerbOwner.Tools</param>
        internal void PreIVerbOwner_GetTools(Type ownerType, Dictionary<string, object> forPostRead)
        {
            if (ownerType != null && typeof(IVerbOwner).IsAssignableFrom(ownerType))
            {
                foreach (CompBasicNodeComp comp in AllNodeComp)
                {
                    try
                    {
                        comp.internal_PreIVerbOwner_GetTools(ownerType, forPostRead);
                    }
                    catch(Exception ex)
                    {
                        Log.Error(ex.ToString());
                    }
                }
            }
            return;
        }
        /// event proccesser after IVerbOwner.VerbProperties
        /// (WARRING!!!: Don't invoke any method if thet will invoke IVerbOwner.VerbProperties)
        /// </summary>
        /// <param name="owner">IVerbOwner source</param>
        /// <param name="verbProperties">result of IVerbOwner.VerbProperties</param>
        internal List<VerbProperties> PostIVerbOwner_GetVerbProperties(Type ownerType, List<VerbProperties> verbProperties, Dictionary<string, object> forPostRead)
        {
            if (ownerType != null && typeof(IVerbOwner).IsAssignableFrom(ownerType))
            {
                List<VerbPropertiesRegiestInfo> verbPropertiesRegiestInfo;
                if (!regiestedNodeVerbPropertiesInfos.TryGetValue(ownerType, out verbPropertiesRegiestInfo))
                {
                    verbPropertiesRegiestInfo = new List<VerbPropertiesRegiestInfo>(verbProperties.Count);
                    foreach (VerbProperties verbProperty in verbProperties)
                    {
                        verbPropertiesRegiestInfo.Add(new VerbPropertiesRegiestInfo(null, verbProperty, verbProperty));
                    }
                    foreach (CompBasicNodeComp comp in AllNodeComp)
                    {
                        try
                        {
                            verbPropertiesRegiestInfo = comp.internal_PostIVerbOwner_GetVerbProperties(ownerType, verbPropertiesRegiestInfo, forPostRead) ?? verbPropertiesRegiestInfo;
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                        }
                    }
                    for (int i = verbPropertiesRegiestInfo.Count - 1; i >= 0; i--)
                    {
                        if (!verbPropertiesRegiestInfo[i].Vaildity)
                        {
                            verbPropertiesRegiestInfo.RemoveAt(i);
                        }
                    }
                    regiestedNodeVerbPropertiesInfos.Add(ownerType, verbPropertiesRegiestInfo);
                }
                verbProperties = new List<VerbProperties>(verbPropertiesRegiestInfo.Count);
                foreach(VerbPropertiesRegiestInfo regiestInfo in verbPropertiesRegiestInfo)
                {
                    verbProperties.Add(regiestInfo.afterConvertProperties);
                }
            }
            return verbProperties;
        }

        /// <summary>
        /// event proccesser after IVerbOwner.VerbProperties
        /// (WARRING!!!: Don't invoke any method if thet will invoke IVerbOwner.Tools)
        /// </summary>
        /// <param name="owner">IVerbOwner source</param>
        /// <param name="verbProperties">result of IVerbOwner.Tools</param>
        internal List<Tool> PostIVerbOwner_GetTools(Type ownerType, List<Tool> tools, Dictionary<string, object> forPostRead)
        {
            if (ownerType != null && typeof(IVerbOwner).IsAssignableFrom(ownerType))
            {
                List<VerbToolRegiestInfo> verbToolRegiestInfo;
                if (!regiestedNodeVerbToolInfos.TryGetValue(ownerType,out verbToolRegiestInfo))
                {
                    verbToolRegiestInfo = new List<VerbToolRegiestInfo>(tools.Count);
                    foreach (Tool tool in tools)
                    {
                        verbToolRegiestInfo.Add(new VerbToolRegiestInfo(null, tool, tool));
                    }
                    foreach (CompBasicNodeComp comp in AllNodeComp)
                    {
                        try
                        {
                            verbToolRegiestInfo = comp.internal_PostIVerbOwner_GetTools(ownerType, verbToolRegiestInfo, forPostRead) ?? verbToolRegiestInfo;
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                        }
                    }
                    for (int i = verbToolRegiestInfo.Count - 1; i >= 0; i--)
                    {
                        if (!verbToolRegiestInfo[i].Vaildity)
                        {
                            verbToolRegiestInfo.RemoveAt(i);
                        }
                    }
                    for(int i = 0; i < verbToolRegiestInfo.Count; i++)
                    {
                        VerbToolRegiestInfo regiestInfo = verbToolRegiestInfo[i];
                        regiestInfo.afterCobvertTool = Gen.MemberwiseClone(regiestInfo.afterCobvertTool);
                        regiestInfo.afterCobvertTool.id = i.ToString();
                        verbToolRegiestInfo[i] = regiestInfo;
                    }
                    regiestedNodeVerbToolInfos.Add(ownerType, verbToolRegiestInfo);
                }
                tools = new List<Tool>(verbToolRegiestInfo.Count);
                foreach (VerbToolRegiestInfo regiestInfo in verbToolRegiestInfo)
                {
                    tools.Add(regiestInfo.afterCobvertTool);
                }
            }
            return tools;
        }
    }
    public abstract partial class CompBasicNodeComp : ThingComp
    {
        protected virtual void PreIVerbOwner_GetVerbProperties(Type ownerType, Dictionary<string, object> forPostRead)
        {
            return;
        }
        protected virtual void PreIVerbOwner_GetTools(Type ownerType, Dictionary<string, object> forPostRead)
        {
            return;
        }
        protected virtual List<VerbPropertiesRegiestInfo> PostIVerbOwner_GetVerbProperties(Type ownerType, List<VerbPropertiesRegiestInfo> result, Dictionary<string, object> forPostRead)
        {
            return result;
        }
        protected virtual List<VerbToolRegiestInfo> PostIVerbOwner_GetTools(Type ownerType, List<VerbToolRegiestInfo> result, Dictionary<string, object> forPostRead)
        {
            return result;
        }
        internal void internal_PreIVerbOwner_GetVerbProperties(Type ownerType, Dictionary<string, object> forPostRead)
            => PreIVerbOwner_GetVerbProperties(ownerType, forPostRead);
        internal void internal_PreIVerbOwner_GetTools(Type ownerType, Dictionary<string, object> forPostRead)
            => PreIVerbOwner_GetTools(ownerType, forPostRead);
        internal List<VerbPropertiesRegiestInfo> internal_PostIVerbOwner_GetVerbProperties(Type ownerType, List<VerbPropertiesRegiestInfo> result, Dictionary<string, object> forPostRead)
            => PostIVerbOwner_GetVerbProperties(ownerType, result, forPostRead);
        internal List<VerbToolRegiestInfo> internal_PostIVerbOwner_GetTools(Type ownerType, List<VerbToolRegiestInfo> result, Dictionary<string, object> forPostRead)
            => PostIVerbOwner_GetTools(ownerType, result, forPostRead);
    }
}