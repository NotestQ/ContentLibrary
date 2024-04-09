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
        public static bool GetContentEventPrefix(ushort id, ref ContentEvent __result)
        {
            ContentEvent? contentEvent = ContentLibrary.EventList[id];
            if (contentEvent == null) return true;

            __result = (ContentEvent)Activator.CreateInstance(contentEvent.GetType());
            return false;
        }
    }
}
            