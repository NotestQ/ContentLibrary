using HarmonyLib;
using System;

namespace ContentLibrary.Patches
{
    [HarmonyPatch(typeof(ContentEventIDMapper))]
    internal class ContentEventIDMapperPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ContentEventIDMapper.GetContentEvent))]
        public static bool GetContentEventPrefix(ref ushort id, ref ContentEvent __result)
        {
            CLogger.LogDebug($"GetContentEvent was called: {id} Normalized: {id - 2000} EventList count: {ContentHandler.EventList!.Count}");
            if (id - 2000 < 0) return true; // IDs 0 - 1999 are reserved for the base game

            ContentEvent? contentEvent = ContentHandler.EventList[id - 2000];
            if (contentEvent == null) return true;

            __result = (ContentEvent)Activator.CreateInstance(contentEvent.GetType());
            return false;
        }
    }
}
            