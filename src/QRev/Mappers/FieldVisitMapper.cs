using System;
using System.Globalization;
using System.Linq;
using FieldDataPluginFramework.Context;
using FieldDataPluginFramework.DataModel;
using QRev.Schema;

namespace QRev.Mappers
{
    public class FieldVisitMapper
    {
        private readonly Channel _channel;
        private readonly LocationInfo _location;
        private readonly Config _config;

        public FieldVisitMapper(Config config, Channel channel, LocationInfo location)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _location = location ?? throw new ArgumentNullException(nameof(location));
        }

        public FieldVisitDetails MapFieldVisitDetails()
        {
            var visitPeriod = GetVisitTimePeriod();

            return new FieldVisitDetails(visitPeriod);
        }

        private DateTimeInterval GetVisitTimePeriod()
        {
            var times = (_channel.Transect ?? new ChannelTransect[0])
                .SelectMany(t => new[] {t.StartDateTime?.Value, t.EndDateTime?.Value})
                .Select(ParseDateTime)
                .Where(dt => dt.HasValue)
                .Select(dt => dt.Value)
                .OrderBy(dt => dt)
                .ToList();

            if (!times.Any())
                throw new ArgumentException($"Can't parse any timestamps from the transects");

            return new DateTimeInterval(times.First(), times.Last());
        }

        private DateTimeOffset? ParseDateTime(string s)
        {
            return DateTime.TryParseExact(s, _config.DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault | DateTimeStyles.AllowWhiteSpaces, out var dateTime)
                ? (DateTimeOffset?)new DateTimeOffset(dateTime, _location.UtcOffset)
                : null;
        }
    }
}
