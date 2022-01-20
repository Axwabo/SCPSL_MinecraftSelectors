using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Exiled.API.Features;
using HarmonyLib;
using Utils;

namespace MinecraftSelectors {
    [HarmonyPatch(typeof(RAUtils), nameof(RAUtils.ProcessPlayerIdOrNamesList))]
    internal static class ProcessPatch {
        private static bool Prefix(ArraySegment<string> args, ref int startindex, ref string[] newargs,
            ref bool keepemptyentries, ref List<ReferenceHub> __result) {
            return !MinecraftSelectorsPlugin.Enabled ||
                   !SelectorProcessor.TryProcessString(MinecraftSelectorsPlugin.CurrentSender, args, startindex,
                       out __result,
                       ref newargs);
        }
    }
}