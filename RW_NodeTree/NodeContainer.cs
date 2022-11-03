﻿using HarmonyLib;
using RW_NodeTree.DataStructure;
using RW_NodeTree.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace RW_NodeTree
{
    /// <summary>
    /// Node storge proccesser
    /// </summary>
    public class NodeContainer : ThingOwner, IDictionary<string, Thing>
    {

        public NodeContainer(CompChildNodeProccesser proccesser) : base(proccesser) { }

        public string this[uint index] => innerIdList[(int)index];


        public Thing this[string key]
        {
            get
            {
                Thing result;
                TryGetValue(key, out result);
                return result;
            }
            set => Add(key, value);
        }

        public string this[Thing item]
        {
            get
            {
                int index = IndexOf(item);
                if(index < 0 || index >= Count) return null;
                return innerIdList[index];
            }
            set
            {
                if(Comp != null && value.IsVaildityKeyFormat() && !innerIdList.Contains(value) && Comp.AllowNode(item, value))
                {
                    int index = IndexOf(item);
                    if (index >= 0 && index < Count)
                    {
                        innerIdList[index] = value;
                        NeedUpdate = true;
                    }
                }
            }
        }

        public CompChildNodeProccesser Comp => (CompChildNodeProccesser)base.Owner;

        public NodeContainer ParentContainer => Comp?.ParentProccesser?.ChildNodes;

        public bool NeedUpdate
        {
            get => needUpdate;
            set
            {
                if(needUpdate = value)
                {
                    NodeContainer parent = ParentContainer;
                    if(parent != null) parent.NeedUpdate = true;
                }
            }
        }

        public ICollection<string> Keys => new List<string>(innerIdList);

        public ICollection<Thing> Values => new List<Thing>(innerList);

        bool ICollection<KeyValuePair<string, Thing>>.IsReadOnly => ((ICollection<Thing>)this).IsReadOnly;

        public override int Count => Math.Min(innerList.Count,innerIdList.Count);

        public override void ExposeData()
        {
            base.ExposeData();

            if(Scribe.EnterNode("InnerList"))
            {

                if (Scribe.mode == LoadSaveMode.Saving)
                {
                    HashSet<string> UsedIds = DebugLoadIDsSavingErrorsChecker_deepSaved(Scribe.saver.loadIDsErrorsChecker);
                    for (int i = 0; i < Count; i++)
                    {
                        Thing thing = innerList[i];
                        string id = innerIdList[i];
                        try
                        {
                            if (UsedIds.Contains(thing.GetUniqueLoadID()) || (thing.holdingOwner != null && thing.holdingOwner != this)) Scribe_References.Look(ref thing, id);
                            else Scribe_Deep.Look(ref thing, id);
                        }
                        catch(Exception e)
                        {
                            Log.Error(e.ToString());
                        }
                    }
                }
                else if(Scribe.mode == LoadSaveMode.LoadingVars)
                {
                    int i = 0;
                    for (XmlNode xmlNode = Scribe.loader.curXmlParent.FirstChild; xmlNode != null; xmlNode = xmlNode.NextSibling)
                    {
                        if (xmlNode.NodeType == XmlNodeType.Element)
                        {
                            try
                            {
                                if (innerList.Count <= i) innerList.Add(null);
                                if (innerIdList.Count <= i) innerIdList.Add(null);
                                innerIdList[i] = xmlNode.Name;
                                if (xmlNode.ChildNodes.Count == 1 && xmlNode.FirstChild.NodeType == XmlNodeType.Text)
                                {
                                    Thing thing = innerList[i];
                                    Scribe_References.Look(ref thing, innerIdList[i]);
                                    innerList[i] = thing;
                                    innerIdList[i] += " !ref";
                                }
                                else
                                {
                                    Thing thing = innerList[i];
                                    Scribe_Deep.Look(ref thing, innerIdList[i]);
                                    innerList[i] = thing;
                                    if (innerList[i] != null) innerList[i].holdingOwner = this;
                                }
                            }
                            catch (Exception e)
                            {
                                Log.Error(e.ToString());
                            }
                            i++;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < Count; i++)
                    {
                        try
                        {
                            Thing thing = innerList[i];
                            string id = innerIdList[i];
                            if (id.EndsWith(" !ref"))
                            {
                                id = id.Substring(0, id.Length - 5);
                                Scribe_References.Look(ref thing, id);
                                innerIdList[i] = id;
                                innerList[i] = thing;
                            }
                        }
                        catch(Exception e)
                        {
                            Log.Error(e.ToString());
                        }
                    }
                }
                Scribe.ExitNode();
            }

            if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
            {
                for (int i = Count - 1; i >= 0; i--)
                {
                    if (innerList[i] == null || innerIdList[i] == null)
                    {
                        innerList.RemoveAt(i);
                        innerIdList.RemoveAt(i);
                    }
                }
                for (int i = innerList.Count - 1; i >= Count; i--)
                {
                    innerList.RemoveAt(i);
                }
                for (int i = innerIdList.Count - 1; i >= Count; i--)
                {
                    innerIdList.RemoveAt(i);
                }
            }
            Scribe_Values.Look<bool>(ref this.needUpdate, "needUpdate");
            //if (Scribe.mode == LoadSaveMode.PostLoadInit) needUpdate = false;
        }

        internal bool internal_UpdateNode(CompChildNodeProccesser actionNode = null)
        {
            bool StopEventBubble = false;
            CompChildNodeProccesser proccess = this.Comp;
            if(proccess != null)
            {
                if (actionNode == null)
                {
                    return proccess.RootNode.ChildNodes.internal_UpdateNode(proccess);
                }
                else if (NeedUpdate)
                {
                    bool reset = true;
                    foreach (Thing node in this.innerList)
                    {
                        NodeContainer container = ((CompChildNodeProccesser)node)?.ChildNodes;
                        if (container != null && container.NeedUpdate)
                        {
                            StopEventBubble = container.internal_UpdateNode(actionNode) || StopEventBubble;
                            reset = false;
                        }
                    }
                    if (!StopEventBubble)
                    {
                        foreach (CompBasicNodeComp comp in proccess.AllNodeComp)
                        {
                            StopEventBubble = comp.internal_UpdateNode(actionNode) || StopEventBubble;
                        }
                    }
                    if (reset)
                    {
                        proccess.ResetVerbs();
                        proccess.ResetRenderedTexture();
                        proccess.ResetRegiestedNodeId();
                    }
                }
            }
            NeedUpdate = false;
            return StopEventBubble;
        }

        public override int GetCountCanAccept(Thing item, bool canMergeWithExistingStacks = true)
        {
            if(Comp != null && innerIdList.Count > Count && Comp.AllowNode(item, innerIdList[Count]))
            {
                return base.GetCountCanAccept(item, false);
            }
            return 0;
        }

        public override int TryAdd(Thing item, int count, bool canMergeWithExistingStacks = true)
        {
            if (count <= 0)
            {
                return 0;
            }

            if (item == null)
            {
                Log.Warning("Tried to add null item to ThingOwner.");
                return 0;
            }

            if (Contains(item))
            {
                Log.Warning(string.Concat("Tried to add ", item, " to ThingOwner but this item is already here."));
                return 0;
            }

            if (!CanAcceptAnyOf(item, canMergeWithExistingStacks))
            {
                return 0;
            }

            int stackCount = item.stackCount;
            int num = Mathf.Min(stackCount, count);
            Thing thing = item.SplitOff(num);
            if (!TryAdd(thing, canMergeWithExistingStacks))
            {
                if (thing != item)
                {
                    int result = stackCount - item.stackCount - thing.stackCount;
                    item.TryAbsorbStack(thing, respectStackLimit: false);
                    return result;
                }

                return stackCount - item.stackCount;
            }

            return num;
        }

        public override bool TryAdd(Thing item, bool canMergeWithExistingStacks = true)
        {

            if (item == null)
            {
                Log.Warning("Tried to add null item to ThingOwner.");
                return false;
            }

            string id = null;
            bool flag = innerIdList.Count > innerList.Count;
            if (flag)
            {
                for (int i = innerIdList.Count - 2; i >= Count; i--)
                {
                    innerIdList.RemoveAt(i);
                }
                id = innerIdList[Count];
            }
            else
            {
                for (int i = innerList.Count - 1; i >= Count; i--)
                {
                    innerList.RemoveAt(i);
                }
            }
            Comp.internal_PerAdd(ref item, ref id);
            if(!innerList.Contains(item))
            {
                if(id.IsVaildityKeyFormat())
                {
                    if(flag) innerIdList[Count] = id;
                    else innerIdList.Add(id);
                }
                else
                {
                    Log.Warning("Invaild key format : " + id);
                    goto fail;
                }
            }
            else
            {
                Log.Warning("Tried to add " + item.ToStringSafe() + " to ThingOwner but this item is already here.");
                goto fail;
            }

            if (!CanAcceptAnyOf(item, canMergeWithExistingStacks)) goto fail;

            if (Count >= maxStacks) goto fail;

            innerList.Add(item);
            if(item.holdingOwner == null) item.holdingOwner = this;
            NeedUpdate = true;

            Comp.internal_PostAdd(item, id, true);
            ((CompChildNodeProccesser)item)?.internal_Added(this, id);
            return true;
            fail:
            Comp.internal_PostAdd(item, id, false);
            return false;
        }

        public override bool Remove(Thing item)
        {

            for (int i = innerList.Count - 1; i >= Count; i--)
            {
                innerList.RemoveAt(i);
            }
            for (int i = innerIdList.Count - 1; i >= Count; i--)
            {
                innerIdList.RemoveAt(i);
            }

            Comp.internal_PerRemove(ref item);

            int index = innerList.LastIndexOf(item);
            string id = (index >= 0 && index < Count) ? innerIdList[index] : null;

            if (index < 0 || index >= Count)
            {
                Comp.internal_PostRemove(item, id, false);
                return false;
            }

            innerList.RemoveAt(index);
            innerIdList.RemoveAt(index);

            if(item.holdingOwner == this) item.holdingOwner = null;

            NeedUpdate = true;

            Comp.internal_PostRemove(item, id, true);
            ((CompChildNodeProccesser)item)?.internal_Removed(this, id);
            return true;
        }


        public bool IsChildOf(Thing node)
        {
            if(node == null) return false;
            IThingHolder thingHolder = Owner;
            Thing parent = (thingHolder as ThingComp)?.parent ?? (thingHolder as Thing);
            while(thingHolder != null && parent != node)
            {
                thingHolder = thingHolder.ParentHolder;
                parent = (thingHolder as ThingComp)?.parent ?? (thingHolder as Thing);
            }
            return parent == node;
        }

        public bool ContainsKey(string key)
        {
            return innerIdList.Contains(key);
        }

        public void Add(string key, Thing value)
        {
            if (key.IsVaildityKeyFormat())
            {
                Thing t = this[key];
                if ((t != null ? Remove(t) : true) && value != null)
                {
                    innerIdList.Add(key);
                    if (!TryAdd(value) && (t == null || !TryAdd(t)))
                    {
                        innerIdList.RemoveAt(Count);
                    }
                }
            }
        }

        public bool Remove(string key)
        {
            return Remove(this[key]);
        }

        public bool TryGetValue(string key, out Thing value)
        {
            value = null;
            if (key.IsVaildityKeyFormat())
            {
                int index = innerIdList.IndexOf(key);
                if(index >= 0 && index < Count)
                {
                    value = this[index];
                    return true;
                }
            }
            return false;
        }

        public IEnumerator<KeyValuePair<string, Thing>> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return new KeyValuePair<string, Thing>(this[(uint)i], this[i]);
            }
        }

        void ICollection<KeyValuePair<string, Thing>>.Add(KeyValuePair<string, Thing> item)
        {
            Add(item.Key, item.Value);
        }

        bool ICollection<KeyValuePair<string, Thing>>.Contains(KeyValuePair<string, Thing> item)
        {
            return item.Value != null && this[item.Key] == item.Value;
        }

        public void CopyTo(KeyValuePair<string, Thing>[] array, int arrayIndex)
        {
            for(int i = 0; i < Count; i++)
            {
                array[i + arrayIndex] = new KeyValuePair<string, Thing>(this[(uint)i], this[i]);
            }
        }

        bool ICollection<KeyValuePair<string, Thing>>.Remove(KeyValuePair<string, Thing> item)
        {
            if(((ICollection<KeyValuePair<string, Thing>>)this).Contains(item))
            {
                return Remove(item.Value);
            }
            return false;
        }


        private static readonly AccessTools.FieldRef<DebugLoadIDsSavingErrorsChecker,HashSet<string>> DebugLoadIDsSavingErrorsChecker_deepSaved = AccessTools.FieldRefAccess<DebugLoadIDsSavingErrorsChecker,HashSet<string>>( "deepSaved");

        public override int IndexOf(Thing item) => innerList.IndexOf(item);

        protected override Thing GetAt(int index) => innerList[index];

        private bool needUpdate = false;

        private List<Thing> innerList = new List<Thing>();

        private List<string> innerIdList = new List<string>();

    }
}
