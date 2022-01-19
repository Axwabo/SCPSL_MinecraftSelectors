using System.Reflection;
using CommandSystem;
using GameCore;
using HarmonyLib;

namespace MinecraftSelectors {
    [HarmonyPatch(typeof(Console), "TypeCommand")]
    internal static class ConsolePatch {
        private static bool Prefix(CommandSender sender) {
            var f = typeof(Console).GetField("_ccs", BindingFlags.NonPublic | BindingFlags.Static);
            MinecraftSelectorsPlugin.CurrentSender = sender ?? (ICommandSender) f?.GetValue(null);
            return true;
        }
    }
}