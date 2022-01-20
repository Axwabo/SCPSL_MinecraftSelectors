using System.ComponentModel;
using Exiled.API.Interfaces;

namespace MinecraftSelectors {
    public class McSConfig : IConfig {
        public bool IsEnabled { get; set; } = true;

        [Description("Enable or disable advanced selectors ([])")]
        public bool AdvancedSelectorsAllowed { get; set; } = true;
    }
}