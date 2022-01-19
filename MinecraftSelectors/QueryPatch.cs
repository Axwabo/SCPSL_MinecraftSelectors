using HarmonyLib;
using RemoteAdmin;

namespace MinecraftSelectors {
    [HarmonyPatch(typeof(CommandProcessor), "ProcessQuery")]
    internal static class QueryPatch {
        private static bool Prefix(CommandSender sender) {
            MinecraftSelectorsPlugin.CurrentSender = sender;
            return true;
        }
    }
}