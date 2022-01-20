using System;
using System.Collections.Generic;
using HarmonyLib;
using Utils;

namespace MinecraftSelectors {
    [HarmonyPatch(typeof(RAUtils), nameof(RAUtils.ProcessPlayerIdOrNamesList))]
    internal static class ProcessPatch {
        private static bool Prefix(ArraySegment<string> args, int startindex, ref string[] newargs,
            bool keepemptyentries, ref List<ReferenceHub> __result) {
            if (!MinecraftSelectorsPlugin.Enabled ||
                !SelectorProcessor.TryProcessString(MinecraftSelectorsPlugin.CurrentSender, args, startindex,
                    out var res, ref newargs, keepemptyentries))
                return true;
            __result = res;
            return false;
        }
    }
}