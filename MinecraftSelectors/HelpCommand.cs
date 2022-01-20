using System;
using CommandSystem;

namespace MinecraftSelectors {
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    public class HelpCommand : ICommand {
        private const string Info =
            "The plugin adds Minecraft-like player selectors to the game. The selectors are used in various SCP:SL commands: give, broadcast etc.\n" +
            "These selectors are to get a list of players to execute the command on.\n" +
            "Try \"mcs basics\" and \"mcs advanced\" for more info about selectors.\n" +
            "In the info, Remote Admin is referenced as RA.";

        private const string Basics =
            "There are 3 available selectors. All players (a), random player (r) and self (s). Self is the executor of the command.\n" +
            "To use the selectors, type '@' and the selector's character. Example: @a\n" +
            "For advanced selectors, try \"mcs advanced\"";

        private const string Advanced =
            "Advanced selectors are used to narrow down the list of selected players.\n" +
            "To prepare advanced selectors, type '[]' after the basic selector. Example: @a[]\n" +
            "Between the brackets ([]), type the advanced selectors. Selectors are separated by commas, and have one or two sides.\n" +
            "A one-sided selector is a selector with a true or false value. To invert the selector, type '!' before it. Example: '!scp' - inverted; 'scp' - not inverted\n" +
            "A two-sided selector requires a value. The property and the value is separated by '='. To invert the selector, use '!=' instead of '=' or type '!' before the property. Example: 'id!=2' - inverted; 'id=2' - not inverted\n" +
            "Some two-sided selectors allow ranged values. The minimum and/or the maximum or just a constant value should be set. Example: '..5' - max 5; '1..' - min 1; '6..9' - between 6 and 9. The range check can also be inverted.\n" +
            "For the list of advanced selectors, try \"mcs selectors\"";

        private const string Selectors =
            "List of advanced selectors:\n" +
            "'playerid' or 'id': if the id of the player in RA matches the value; uses range check. Example: @a[id=2]\n" +
            "'r' or 'role' or 'class': the role of the player. Requires the role name, number or range. Examples: @a[r=class-d] @a[r=0..3]\n" +
            "'scp': if the player is an SCP; can be inverted. Example: @r[scp]\n" +
            "'god' or 'godmode': if the player has god mode on. Example: @a[!god]\n" +
            "'noclip': if the player has noclip (can move through objects) enabled. Example: @r[noclip]\n" +
            "'verified': if the player's Steam account was verified and has their nickname assigned. Example: @a[verified]\n" +
            "'team': the team of the player. Use \"mcs values\" to list the teams. Example: @r[team=SCP]\n" +
            "'remoteadmin' or 'ra': if the player is logged into RA. Example: @a[!ra]\n" +
            "'bypass': if the player has bypass mode (open everything with hand) enabled. Example: @r[!bypass]\n" +
            "'dnt' or 'donottrack': if the player has DNT (forbids the server to use User ID for purposes not related to security) enabled. Example: @a[dnt]\n" +
            "'name': if the player's name is equal to the value (case insensitive). Example: @a[name=Axwabo]\n" +
            "'namehas': if the player's name includes the sequence (case insensitive). Example: @a[namehas=fire]";


        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response) {
            if (arguments.Count > 0)
                switch (arguments.At(0).ToLower()) {
                    case "info":
                        response =
                            $"MinecraftPlayerSelectors by Axwabo, version {MinecraftSelectorsPlugin.Singleton.Version}\n{Info}";
                        return true;
                    case "basics":
                        response = Basics;
                        return true;
                    case "advanced":
                        response = Advanced;
                        return true;
                    case "selectors":
                        response = Selectors;
                        return true;
                    case "values":
                        response = $"Roles: {GetEnums(typeof(RoleType))}\n" +
                                   "Teams: SCP, MTF, CHI, RSC, CDP, RIP, TUT\n" +
                                   "CHI = Chaos Insurgency, RSC = Scientists, CDP = Class-D's, RIP = Spectators, TUT = Tutorial class.\n" +
                                   "In roles, SCP-173 is number 0, and each one after it increases by one (Class-D = 1, Spectator = 2).";
                        return true;
                }

            response = "Usage: mcs <subcommand>\nSubcommands: info, basics, advanced, selectors, values";
            return false;
        }

        public string Command { get; } = "minecraftselectors";
        public string[] Aliases { get; } = {"mcs", "selectorshelp", "selectorhelp"};
        public string Description { get; } = "Gives a detailed guide on using the MinecraftPlayerSelectors plugin.";

        private static string GetEnums(Type type) {
            return string.Join(", ", type.GetEnumNames());
        }
    }
}