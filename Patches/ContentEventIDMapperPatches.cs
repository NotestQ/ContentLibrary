using ContentLibrary;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Text;

namespace ContentLibrary.Patches
{
    [HarmonyPatch(typeof(ContentEventIDMapper))]
    internal class ContentEventIDMapperPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ContentEventIDMapper.GetContentEvent))]
        public static bool GetContentEventPrefix(ref ushort id, ref ContentEvent __result)
        {
            CLogger.LogDebug($"GetContentEvent was called: {id} Normalized: {id - 2000} EventList count: {ContentLibrary.EventList.Count}");
            ContentEvent? contentEvent = ContentLibrary.EventList[id - 2000];
            if (contentEvent == null) return true;

            __result = (ContentEvent)Activator.CreateInstance(contentEvent.GetType());
            return false;
        }
    }
}
            