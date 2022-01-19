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
                var list = ExecuteSelector(sender, array[startIndex], ReferenceHub.GetAllHubs().Values);
                var index = startIndex + 1;
                var advanced = string.Join(" ", array.Segment(startIndex)).SafeSubstring(2);
                if (advanced.Length < 2 || !advanced.StartsWith("[")) {
                    hubs = list;
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
                hubs = list.Where(h => filters.All(f => f(h))).ToList();
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
            var set = new List<ReferenceHub>();
            switch (method) {
                case 'r':
                    set.Add(hubs.Random());
                    break;
                case 's':
                    if (sender is PlayerCommandSender p)
                        set.Add(p.ReferenceHub);
                    break;
                case 'a':
                    set.AddRange(hubs);
                    break;
                default:
                    return hubs.ToList();
            }

            return set;
        }

        private static string Simplify(string s) {
            return Pattern.Replace(s.ToLower(), "");
        }

        private static Func<ReferenceHub, bool> GetFilter(string property, string value, bool invert) {
            Log.Debug("property " + property + "|value " + value);
            if (string.IsNullOrEmpty(property))
                return null;
            switch (Simplify(property)) {
                case "playerid":
                case "id": {
                    if (value == null)
                        break;
                    if (int.TryParse(value, out var result))
                        return hub => {
                            var b = hub.queryProcessor.NetworkPlayerId == result;
                            return invert ? !b : b;
                        };
                    break;
                }
                case "r":
                case "role":
                case "class": {
                    if (value == null)
                        break;
                    RoleType r = GetRole(value);
                    if (r == RoleType.None)
                        break;
                    return hub => {
                        var b = hub.characterClassManager.CurRole.roleId == r;
                        return invert ? !b : b;
                    };
                }
                case "scp":
                    return hub => {
                        var b = hub.characterClassManager.CurRole.team == Team.SCP;
                        return invert ? !b : b;
                    };
                case "god":
                case "godmode":
                    return hub => hub.characterClassManager.GodMode != invert;
                case "noclip":
                    return hub => hub.characterClassManager.NetworkNoclipEnabled != invert;
                case "verified":
                    return hub => hub.Ready != invert;
                case "team": {
                    if (value == null || !Enum.TryParse(value, true, out Team t))
                        break;
                    return hub => {
                        var b = hub.characterClassManager.CurRole.team == t;
                        return invert ? !b : b;
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
    }
}