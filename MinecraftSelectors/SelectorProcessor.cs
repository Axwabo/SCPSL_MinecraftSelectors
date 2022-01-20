using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CommandSystem;
using Exiled.API.Features;
using RemoteAdmin;
using static MinecraftSelectors.MinecraftSelectorsPlugin;

namespace MinecraftSelectors {
    public static class SelectorProcessor {
        private static readonly Regex Pattern = new Regex(" ");
        private static readonly List<char> Selectors = new List<char> {'a', 's', 'r'};
        private static readonly char[] Numbers = "0123456789".ToCharArray();

        public static bool TryProcessString(ICommandSender sender, ArraySegment<string> arguments, int startIndex,
            out List<ReferenceHub> hubs, ref string[] newArguments) {
            hubs = new List<ReferenceHub>(10);
            try {
                var array = arguments.ToArray();
                if (!(array.Length > startIndex))
                    return false;
                var selector = array[startIndex];
                if (!selector.StartsWith("@") || selector.Length < 2 || !selector[1].IsValidSelector())
                    return false;
                var index = startIndex + 1;
                var advanced = string.Join(" ", array.Segment(startIndex)).SafeSubstring(2);
                if (advanced.Length < 2 || !advanced.StartsWith("[")) {
                    hubs = ExecuteSelector(sender, array[startIndex], ReferenceHub.GetAllHubs().Values);
                    newArguments = array.Segment(index).ToArray();
                    return true;
                }

                var chars = advanced.ToCharArray();
                var propertyStart = 1;
                var valueStart = 1;
                var propertyEnded = false;
                var invert = false;
                string property = null;
                var filters = new List<Func<ReferenceHub, bool>>();
                for (var charIndex = 1; charIndex < chars.Length; charIndex++) {
                    var c = chars[charIndex];
                    if (c == ' ') {
                        index++;
                        continue;
                    }

                    if (c == '!') {
                        if (!propertyEnded) {
                            invert = true;
                            if (!chars.Equals(charIndex + 1, '=')) {
                                propertyStart = charIndex + 1;
                                continue;
                            }
                        }
                    }

                    if (c == '=') {
                        propertyEnded = true;
                        valueStart = charIndex + 1;
                        continue;
                    }

                    if (c == ',') {
                        if (!propertyEnded)
                            property = advanced.Substring(propertyStart, charIndex - propertyStart);
                        propertyEnded = false;
                        filters.AddNonNull(GetFilter(property,
                            advanced.Substring(valueStart, charIndex - valueStart),
                            invert));
                        propertyStart = charIndex + 1;
                        property = null;
                        continue;
                    }

                    if (c != ']')
                        continue;
                    if (!propertyEnded)
                        property = advanced.Substring(propertyStart, charIndex - propertyStart);
                    filters.AddNonNull(GetFilter(property,
                        advanced.Substring(valueStart, charIndex - valueStart),
                        invert));
                    break;
                }

                newArguments = array.Length > index ? array.Segment(index).ToArray() : new string[] { };
                hubs = ExecuteSelector(sender, selector,
                    ReferenceHub.GetAllHubs().Values.Where(h =>
                        !Singleton.Config.AdvancedSelectorsAllowed || filters.All(f => f(h))));
                return true;
            } catch (Exception e) {
                Log.Error(e);
                return false;
            }
        }

        private static List<ReferenceHub> ExecuteSelector(
            ICommandSender sender, string arg,
            IEnumerable<ReferenceHub> hubs) {
            if (arg.Length < 1)
                return hubs.ToList();
            var method = arg[1];
            var list = hubs.ToList();
            switch (method) {
                case 'r':
                    return new List<ReferenceHub>(1) {list.Random()};
                case 's':
                    if (sender is PlayerCommandSender p)
                        return new List<ReferenceHub>(1) {p.ReferenceHub};
                    break;
            }

            return list;
        }

        private static string Simplify(string s) {
            return Pattern.Replace(s.ToLower(), "");
        }

        private static Func<ReferenceHub, bool> GetFilter(string property, string value, bool invert) {
            if (string.IsNullOrEmpty(property))
                return null;
            switch (Simplify(property)) {
                case "playerid":
                case "id": {
                    if (value == null)
                        break;
                    var range = TryParseRange(value, out int min, out var max, out var minSet, out var maxSet);
                    int.TryParse(value, out var result);
                    return hub => {
                        var id = hub.queryProcessor.NetworkPlayerId;
                        return (id == result || range && id.CheckRange(min, max, minSet, maxSet)) != invert;
                    };
                    break;
                }
                case "r":
                case "role":
                case "class": {
                    if (value == null)
                        break;
                    var range = TryParseRange(value, out sbyte min, out var max, out var minSet, out var maxSet);
                    var r = GetRole(value);
                    return hub => {
                        var id = hub.characterClassManager.CurRole.roleId;
                        return (id == r || range && ((sbyte) id).CheckRange(min, max, minSet, maxSet)) != invert;
                    };
                }
                case "scp":
                    return hub => hub.characterClassManager.CurRole.team == Team.SCP != invert;
                case "god":
                case "godmode":
                    return hub => hub.characterClassManager.GodMode != invert;
                case "noclip":
                    return hub => hub.characterClassManager.NetworkNoclipEnabled != invert;
                case "verified":
                    return hub => hub.Ready != invert;
                case "team": {
                    if (value == null)
                        break;
                    var e = Enum.TryParse(value, true, out Team team);
                    var b = byte.TryParse(value, out var result);
                    var r = TryParseRange(value, out byte min, out var max, out var minSet, out var maxSet);
                    if (!(b || e || r))
                        break;
                    return hub => {
                        var t = hub.characterClassManager.CurRole.team;
                        return (e && t == team || b && (byte) t == result ||
                                r && ((byte) t).CheckRange(min, max, minSet, maxSet)) != invert;
                    };
                }
                case "remoteadmin":
                case "ra":
                    return hub => hub.serverRoles.RemoteAdmin != invert;
                case "bypass":
                    return hub => hub.serverRoles.BypassMode != invert;
                case "dnt":
                case "donottrack":
                    return hub => hub.serverRoles.DoNotTrack != invert;
                case "name":
                    return hub =>
                        hub.nicknameSync.MyNick.Equals(value, StringComparison.OrdinalIgnoreCase) != invert;
                case "namehas":
                    return hub => hub.nicknameSync.MyNick.Contains(value, StringComparison.OrdinalIgnoreCase) != invert;
            }

            return null;
        }

        private static bool Equals<T>(this T[] array, int index, T value) {
            return array.Length > index && value.Equals(array[index]);
        }

        public static void AddNonNull<T>(this List<T> list, T value) {
            if (value == null)
                return;
            list.Add(value);
        }

        public static bool IsValidSelector(this char character) {
            return Selectors.Contains(character);
        }

        public static string SafeSubstring(this string s, int start) {
            return s.SafeSubstring(start, s.Length - start);
        }

        public static string SafeSubstring(this string s, int start, int length) {
            return start < 0 || start > s.Length || length < 0 || start > s.Length - length || length == 0
                ? ""
                : s.Substring(start, length);
        }

        public static bool CheckRange(this int x, int min, int max, bool minSet, bool maxSet) {
            if (!minSet && !maxSet)
                return false;
            if (!minSet)
                return x <= max;
            if (!maxSet)
                return x >= min;
            return x >= min && x <= max;
        }

        public static bool CheckRange(this sbyte x, sbyte min, sbyte max, bool minSet, bool maxSet) {
            if (!minSet && !maxSet)
                return false;
            if (!minSet)
                return x <= max;
            if (!maxSet)
                return x >= min;
            return x >= min && x <= max;
        }

        public static bool CheckRange(this byte x, byte min, byte max, bool minSet, bool maxSet) {
            if (!minSet && !maxSet)
                return false;
            if (!minSet)
                return x <= max;
            if (!maxSet)
                return x >= min;
            return x >= min && x <= max;
        }

        public static bool TryParseRange(string value, out int min, out int max, out bool minSet, out bool maxSet) {
            min = 0;
            max = 0;
            minSet = false;
            maxSet = false;
            if (string.IsNullOrEmpty(value))
                return false;
            if (value.EndsWith(".."))
                minSet = int.TryParse(value.Filter(c => Numbers.Contains(c)), out min);
            else if (value.StartsWith(".."))
                maxSet = int.TryParse(value.Filter(c => Numbers.Contains(c)), out max);
            else {
                minSet = int.TryParse(
                    value.SafeSubstring(value.IndexOf("..", StringComparison.Ordinal))
                        .Filter(c => Numbers.Contains(c)), out min);
                maxSet = int.TryParse(
                    value.SafeSubstring(value.LastIndexOf("..", StringComparison.Ordinal) + 2)
                        .Filter(c => Numbers.Contains(c)), out max);
            }

            return minSet || maxSet;
        }

        public static bool TryParseRange(string value, out sbyte min, out sbyte max, out bool minSet, out bool maxSet) {
            min = 0;
            max = 0;
            minSet = false;
            maxSet = false;
            if (string.IsNullOrEmpty(value))
                return false;
            if (value.EndsWith(".."))
                minSet = sbyte.TryParse(value.Filter(c => Numbers.Contains(c)), out min);
            else if (value.StartsWith(".."))
                maxSet = sbyte.TryParse(value.Filter(c => Numbers.Contains(c)), out max);
            else {
                minSet = sbyte.TryParse(
                    value.SafeSubstring(value.IndexOf("..", StringComparison.Ordinal))
                        .Filter(c => Numbers.Contains(c)), out min);
                maxSet = sbyte.TryParse(
                    value.SafeSubstring(value.LastIndexOf("..", StringComparison.Ordinal) + 2)
                        .Filter(c => Numbers.Contains(c)), out max);
            }

            return minSet || maxSet;
        }

        public static bool TryParseRange(string value, out byte min, out byte max, out bool minSet, out bool maxSet) {
            min = 0;
            max = 0;
            minSet = false;
            maxSet = false;
            if (string.IsNullOrEmpty(value))
                return false;
            if (value.EndsWith(".."))
                minSet = byte.TryParse(value.Filter(c => Numbers.Contains(c)), out min);
            else if (value.StartsWith(".."))
                maxSet = byte.TryParse(value.Filter(c => Numbers.Contains(c)), out max);
            else {
                minSet = byte.TryParse(
                    value.SafeSubstring(value.IndexOf("..", StringComparison.Ordinal))
                        .Filter(c => Numbers.Contains(c)), out min);
                maxSet = byte.TryParse(
                    value.SafeSubstring(value.LastIndexOf("..", StringComparison.Ordinal) + 2)
                        .Filter(c => Numbers.Contains(c)), out max);
            }

            return minSet || maxSet;
        }

        public static string Filter(this string s, Func<char, bool> filter) {
            return string.Join("", s.ToCharArray().Where(filter));
        }

        public static bool Contains(this string s, string contain, StringComparison comparison) {
            return s.IndexOf(contain, comparison) >= 0;
        }
    }
}