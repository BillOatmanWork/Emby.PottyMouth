using MediaBrowser.Model.Plugins;

namespace PottyMouth.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public bool EnablePottyMouth { get; set; }
        public int startOffset { get; set; }
        public int endOffset { get; set; }
    }
}