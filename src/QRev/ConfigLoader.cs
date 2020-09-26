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

            config.DateTimeFormats = SanitizeList(config.DateTimeFormats, "MM/dd/yyyy HH:mm:ss");

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

        private static string[] SanitizeList(IList<string> list, params string[] defaultIfEmpty)
        {
            if (defaultIfEmpty == null || defaultIfEmpty.Length == 0)
                throw new ArgumentException("Can't be empty", nameof(defaultIfEmpty));

            if (list == null)
            {
                list = new List<string>();
            }

            list = list
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            if (!list.Any())
            {
                foreach (var s in defaultIfEmpty)
                {
                    list.Add(s);
                }
            }

            return list.ToArray();
        }
    }
}
