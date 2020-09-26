using FieldDataPluginFramework;
using FieldDataPluginFramework.Context;
using FieldDataPluginFramework.Results;
using QRev.Mappers;
using QRev.Schema;

namespace QRev
{
    public class Parser
    {
        private readonly LocationInfo _location;
        private readonly IFieldDataResultsAppender _appender;
        private readonly ILog _logger;

        public Parser(LocationInfo location, IFieldDataResultsAppender appender, ILog logger)
        {
            _location = location;
            _appender = appender;
            _logger = logger;
        }

        public void Parse(Channel channel)
        {
            var config = new ConfigLoader(_appender).Load();
            var fieldVisitInfo = AppendMappedFieldVisitInfo(config, channel, _location);

            AppendMappedMeasurements(config, channel, fieldVisitInfo);
        }

        private FieldVisitInfo AppendMappedFieldVisitInfo(Config config, Channel channel, LocationInfo locationInfo)
        {
            var mapper = new FieldVisitMapper(config, channel, _location);
            var fieldVisitDetails = mapper.MapFieldVisitDetails();

            _logger.Info($"Successfully parsed one visit '{fieldVisitDetails.FieldVisitPeriod}' " +
                         $"for location '{locationInfo.LocationIdentifier}'");

            return _appender.AddFieldVisit(locationInfo, fieldVisitDetails);
        }

        private void AppendMappedMeasurements(Config config, Channel channel, FieldVisitInfo fieldVisitInfo)
        {
            var dischargeActivityMapper = new DischargeActivityMapper(config, fieldVisitInfo);

            _appender.AddDischargeActivity(fieldVisitInfo, dischargeActivityMapper.Map(channel));
        }
    }
}
