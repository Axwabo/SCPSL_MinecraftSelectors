using Exiled.API.Interfaces;

namespace MinecraftSelectors {
    public class McSConfig : IConfig {
        public bool IsEnabled { get; set; } = true;

        public bool EnableSelectors { get; set; } = true;
    }
}