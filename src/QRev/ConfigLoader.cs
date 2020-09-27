using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using FieldDataPluginFramework.Results;
using ServiceStack;

namespace QRev
{
    public class ConfigLoader
    {
        private Dictionary<string,string> Settings { get; }

        public ConfigLoader(IFieldDataResultsAppender appender)
        {
            Settings = appender.GetPluginConfigurations();
        }

        public Config Load()
        {
            if (!Settings.TryGetValue(nameof(Config), out var jsonText) || string.IsNullOrWhiteSpace(jsonText))
                return Sanitize(new Config());

            try
            {
                return Sanitize(jsonText.FromJson<Config>());
            }
            catch (SerializationException exception)
            {
                throw new ArgumentException($"Invalid Config JSON:\b{jsonText}", exception);
            }
        }

        private Config Sanitize(Config config)
        {
            config.BottomEstimateMethods = SanitizeMethods(config.BottomEstimateMethods, new Dictionary<string, string>
            {
                {"Power", "POWR"},
                {"No Slip", "NSLP"},
            });

            config.TopEstimateMethods = SanitizeMethods(config.TopEstimateMethods, new Dictionary<string, string>
            {
                {"Constant", "CNST"},
                {"Power", "POWR"},
                {"3-Point", "3PNT"},
            });

            config.NavigationMethods = SanitizeMethods(config.NavigationMethods, new Dictionary<string, string>
            {
                {"BT", "BT"},
                {"GGA", "GGA"},
                {"VTG", "VTG"},
            });

            config.DepthReferences = SanitizeMethods(config.DepthReferences, new Dictionary<string, string>
            {
                {"BT", "BottomTrack"},
                {"VB", "VerticalBeam"},
                {"DS", "DepthSounder"},
                {"Composite", "Composite"},
            });

            return config;
        }

        private static Dictionary<string,string> SanitizeMethods(Dictionary<string,string>methods, Dictionary<string,string> defaultIfEmpty)
        {
            if (defaultIfEmpty == null || !defaultIfEmpty.Any())
                throw new ArgumentException("Can't be empty", nameof(defaultIfEmpty));

            if (methods == null || !methods.Any())
            {
                methods = defaultIfEmpty;
            }

            return methods
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value,
                    StringComparer.InvariantCultureIgnoreCase);
        }
    }
}
