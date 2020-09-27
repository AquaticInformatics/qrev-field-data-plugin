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
        private Channel Channel { get; }
        private TimeSpan UtcOffset { get; }

        public FieldVisitMapper(Channel channel, LocationInfo location)
        {
            Channel = channel ?? throw new ArgumentNullException(nameof(channel));

            UtcOffset = string.Compare(Channel.QRevVersion,"4", StringComparison.InvariantCultureIgnoreCase) < 0
                ? location.UtcOffset
                : TimeSpan.Zero;
        }

        public FieldVisitDetails MapFieldVisitDetails()
        {
            var visitPeriod = GetVisitTimePeriod();

            return new FieldVisitDetails(visitPeriod);
        }

        private DateTimeInterval GetVisitTimePeriod()
        {
            var times = (Channel.Transect ?? new ChannelTransect[0])
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
            return DateTime.TryParseExact(s, "M/d/yyyy H:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault | DateTimeStyles.AllowWhiteSpaces, out var dateTime)
                ? (DateTimeOffset?)new DateTimeOffset(dateTime, UtcOffset)
                : null;
        }
    }
}
