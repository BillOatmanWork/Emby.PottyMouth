using System;
using System.Collections.Generic;
using System.IO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using PottyMouth.Configuration;

namespace PottyMouth
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasThumbImage
    {
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public override string Name => "Potty Mouth";


        public override string Description => "Emby plugin that mutes bad words";


        public static Plugin Instance { get; private set; }

        private Guid _id = new Guid("D9085D73-7142-4D82-905B-2A0B1949A6D2");
                                      
        public override Guid Id => _id;

        public Stream GetThumbImage()
        {
            var type = GetType();
            return type.Assembly.GetManifestResourceStream(type.Namespace + ".logo.png");
        }

        public ImageFormat ThumbImageFormat => ImageFormat.Png;

        public IEnumerable<PluginPageInfo> GetPages() => new[]
        {
            new PluginPageInfo
            {
                Name = "PottyMouthConfigurationPage",
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.PottyMouth.html"
            },
            new PluginPageInfo
            {
                Name = "PottyMouthConfigurationPageJS",
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.PottyMouth.js",
            }
        };
    }
}
