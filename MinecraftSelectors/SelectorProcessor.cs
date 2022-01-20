using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CommandSystem;
using Exiled.API.Features;
using RemoteAdmin;
using UnityEngine;
using static MinecraftSelectors.MinecraftSelectorsPlugin;

namespace MinecraftSelectors {
    public static class SelectorProcessor {
        private static readonly Regex Pattern = new Regex("[ -.'_]");
        private static readonly List<char> Selectors = new List<char>(4) {'a', 's', 'r', 'p'};
        private static readonly Regex Numbers = new Regex("-?\\d+\\.?\\d*");

        public static bool TryProcessString(ICommandSender sender, ArraySegment<string> arguments, int startIndex,
            out List<ReferenceHub> result, ref string[] newArguments, bool keepEmptyEntries = true) {
            result = new List<ReferenceHub>(10);
            try {
                var array = arguments.ToArray();
                if (!(array.Length > startIndex))
                    return false;
                var selector = array[startIndex];
                if (!selector.StartsWith("@") || selector.Length < 2 || !selector[1].IsValidSelector())
                    return false;
                IEnumerable<ReferenceHub> hubs = ReferenceHub.GetAllHubs().Values;
                if (!Singleton.Config.IncludeHost)
                    hubs = hubs.Where(hub => hub != ReferenceHub.HostHub && hub != ReferenceHub.LocalHub);
                var index = startIndex + 1;
                var advanced = string.Join(" ", array.Segment(startIndex)).SafeSubstring(2);
                if (advanced.Length < 2 || !advanced.StartsWith("[")) {
                    result = ExecuteSelector(sender, array[startIndex], hubs, -1, -1, 1);
                    newArguments = array.Segment(index).Where(e => keepEmptyEntries || e.Length > 0).ToArray();
                    return true;
                }

                var chars = advanced.ToCharArray();
                var propertyStart = 1;
                var valueStart = 1;
                var propertyEnded = false;
                var invert = false;
                string property = null;
                var minDist = -1f;
                var maxDist = -1f;
                var limit = -1;
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
                        if (!propertyEnded)
                            property = advanced.Substring(propertyStart, charIndex - propertyStart);
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
                            invert, ref limit, ref minDist, ref maxDist));
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
                        invert, ref limit, ref minDist, ref maxDist));
                    break;
                }

                newArguments = array.Length > index
                    ? array.Segment(index).Where(e => keepEmptyEntries || e.Length > 0).ToArray()
                    : new string[] { };
                result = ExecuteSelector(sender, selector,
                    hubs.Where(h =>
                        !Singleton.Config.AdvancedSelectorsAllowed || filters.All(f => f(h))), limit, minDist, maxDist);
                return true;
            } catch (Exception e) {
                Log.Error(e);
                return false;
            }
        }

        private static List<ReferenceHub> ExecuteSelector(
            ICommandSender sender, string arg,
            IEnumerable<ReferenceHub> hubs, int limit, float minDist, float maxDist) {
            if (limit == 0)
                return new List<ReferenceHub>(0);
            if (arg.Length < 1)
                return hubs.ToList();
            var method = arg.ToLower()[1];
            var list = hubs.ToList();
            limit = limit < 0 ? list.Count : limit;
            switch (method) {
                case 'r':
                    if (limit < 0)
                        return new List<ReferenceHub>(1) {list.RandomItem()};
                    var l = new List<ReferenceHub>(limit);
                    for (var i = 0; i < limit; i++) {
                        if (list.Count < 1)
                            break;
                        var item = list.RandomItem();
                        l.Add(item);
                        list.Remove(item);
                    }

                    return l;
                case 's': {
                    if (sender is PlayerCommandSender p)
                        return new List<ReferenceHub>(1) {p.ReferenceHub};
                    break;
                }
                case 'p': {
                    if (sender is PlayerCommandSender p) {
                        var pos = p.ReferenceHub.transform.position;
                        return list.Where(h => maxDist < 0 || Vector3.Distance(pos, h.transform.position)
                                .CheckRange(minDist, maxDist, minDist >= 0, maxDist >= 0))
                            .OrderBy(hub => Vector3.Distance(pos, hub.transform.position)).Take(limit).ToList();
                    }

                    break;
                }
            }

            return list.Take(limit).ToList();
        }

        private static string Simplify(string s) {
            return Pattern.Replace(s.ToLower(), "");
        }

        private static Func<ReferenceHub, bool> GetFilter(string property, string value, bool invert, ref int limit,
            ref float minDist, ref float maxDist) {
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
                case "distance":
                case "dist": {
                    if (value == null)
                        break;
                    if (TryParseRange(value, out float min, out var max, out _, out _)) {
                        minDist = min;
                        maxDist = max;
                        break;
                    }

                    if (float.TryParse(value, out var x))
                        minDist = maxDist = x;

                    break;
                }
                case "limit": {
                    if (value == null)
                        break;
                    if (int.TryParse(value, out var x))
                        limit = x;
                    break;
                }
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

        public static bool CheckRange(this float x, float min, float max, bool minSet, bool maxSet) {
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
                minSet = int.TryParse(Numbers.Match(value).Value, out min);
            else if (value.StartsWith(".."))
                maxSet = int.TryParse(Numbers.Match(value).Value, out max);
            else {
                minSet = int.TryParse(
                    Numbers.Match(value.SafeSubstring(0, value.IndexOf("..", StringComparison.Ordinal))).Value,
                    out min);
                maxSet = int.TryParse(
                    Numbers.Match(value.SafeSubstring(value.LastIndexOf("..", StringComparison.Ordinal) + 2)).Value,
                    out max);
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
                minSet = sbyte.TryParse(Numbers.Match(value).Value, out min);
            else if (value.StartsWith(".."))
                maxSet = sbyte.TryParse(Numbers.Match(value).Value, out max);
            else {
                minSet = sbyte.TryParse(
                    Numbers.Match(value.SafeSubstring(0, value.IndexOf("..", StringComparison.Ordinal))).Value,
                    out min);
                maxSet = sbyte.TryParse(
                    Numbers.Match(value.SafeSubstring(value.LastIndexOf("..", StringComparison.Ordinal) + 2)).Value,
                    out max);
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
                minSet = byte.TryParse(Numbers.Match(value).Value, out min);
            else if (value.StartsWith(".."))
                maxSet = byte.TryParse(Numbers.Match(value).Value, out max);
            else {
                minSet = byte.TryParse(
                    Numbers.Match(value.SafeSubstring(0, value.IndexOf("..", StringComparison.Ordinal))).Value,
                    out min);
                maxSet = byte.TryParse(
                    Numbers.Match(value.SafeSubstring(value.LastIndexOf("..", StringComparison.Ordinal) + 2)).Value,
                    out max);
            }

            return minSet || maxSet;
        }

        public static bool TryParseRange(string value, out float min, out float max, out bool minSet, out bool maxSet) {
            min = 0;
            max = 0;
            minSet = false;
            maxSet = false;
            if (string.IsNullOrEmpty(value))
                return false;
            if (value.EndsWith(".."))
                minSet = float.TryParse(Numbers.Match(value).Value, out min);
            else if (value.StartsWith(".."))
                maxSet = float.TryParse(Numbers.Match(value).Value, out max);
            else {
                minSet = float.TryParse(
                    Numbers.Match(value.SafeSubstring(0, value.IndexOf("..", StringComparison.Ordinal))).Value,
                    out min);
                maxSet = float.TryParse(
                    Numbers.Match(value.SafeSubstring(value.LastIndexOf("..", StringComparison.Ordinal) + 2)).Value,
                    out max);
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