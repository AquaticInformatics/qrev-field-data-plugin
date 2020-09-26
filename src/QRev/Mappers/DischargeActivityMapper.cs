using System;
using System.Collections.Generic;
using System.Linq;
using FieldDataPluginFramework.Context;
using FieldDataPluginFramework.DataModel;
using FieldDataPluginFramework.DataModel.ChannelMeasurements;
using FieldDataPluginFramework.DataModel.DischargeActivities;
using FieldDataPluginFramework.DataModel.PickLists;
using FieldDataPluginFramework.Units;
using QRev.Helper;
using QRev.Schema;
using QRev.SystemCode;

namespace QRev.Mappers
{
    internal class DischargeActivityMapper
    {
        private readonly FieldVisitInfo _fieldVisitInfo;
        private readonly Config _config;

        public bool IsMetric { get; private set; }

        public DischargeActivityMapper(Config config, FieldVisitInfo fieldVisitInfo)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _fieldVisitInfo = fieldVisitInfo ?? throw new ArgumentNullException(nameof(fieldVisitInfo));
        }

        public DischargeActivity Map(Channel channel)
        {
            IsMetric = "cms" == channel.ChannelSummary?.Discharge?.Total?.unitsCode;

            var unitSystem = IsMetric
                ? Units.MetricUnitSystem
                : Units.ImperialUnitSystem;

            var dischargeActivity = CreateDischargeActivityWithSummary(channel, unitSystem);

            SetDischargeSection(dischargeActivity, channel, unitSystem);

            return dischargeActivity;
        }

        private DischargeActivity CreateDischargeActivityWithSummary(Channel channel, UnitSystem unitSystem)
        {
            var factory = new DischargeActivityFactory(unitSystem);

            var totalDischarge = channel.ChannelSummary?.Discharge?.Total?.Value ??
                                 throw new ArgumentException("No total discharge amount provided");

            //Discharge summary:
            var measurementPeriod = GetMeasurementPeriod();
            var dischargeActivity = factory.CreateDischargeActivity(measurementPeriod, totalDischarge.AsDouble());

            dischargeActivity.Comments = string.Join("\n", new[]
                {
                    channel.ChannelSummary?.Other?.UserComment?.Value,
                    channel.QA?.QRev_Message?.Value,
                }
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim()));

            return dischargeActivity;
        }

        private DateTimeInterval GetMeasurementPeriod()
        {
            return new DateTimeInterval(_fieldVisitInfo.StartDate, _fieldVisitInfo.EndDate);
        }

        private void SetDischargeSection(DischargeActivity dischargeActivity, Channel channel, UnitSystem unitSystem)
        {
            var dischargeSection = CreateDischargeSectionWithDescription(dischargeActivity, channel, unitSystem);

            dischargeActivity.ChannelMeasurements.Add(dischargeSection);

            dischargeActivity.QuantitativeUncertainty = channel.ChannelSummary?.Uncertainty?.Total?.Value;
            dischargeActivity.ActiveUncertaintyType = dischargeActivity.QuantitativeUncertainty.HasValue
                ? UncertaintyType.Quantitative
                : UncertaintyType.None;
        }

        private AdcpDischargeSection CreateDischargeSectionWithDescription(DischargeActivity dischargeActivity,
            Channel channel, UnitSystem unitSystem)
        {
            var adcpDischargeSection = new AdcpDischargeSection(
                dischargeActivity.MeasurementPeriod,
                ChannelMeasurementBaseConstants.DefaultChannelName,
                dischargeActivity.Discharge,
                $"{channel.Instrument?.Manufacturer?.Value} {channel.Instrument?.Model?.Value}",
                unitSystem.DistanceUnitId,
                unitSystem.AreaUnitId,
                unitSystem.VelocityUnitId)
            {
                Party = dischargeActivity.Party,
                Comments = dischargeActivity.Comments,
                WidthValue = channel.ChannelSummary?.Other?.MeanWidth?.Value.AsDouble(),
                AreaValue = channel.ChannelSummary?.Other?.MeanArea?.Value.AsDouble(),
                VelocityAverageValue = channel.ChannelSummary?.Other?.MeanQoverA?.Value.AsDouble(),
                NumberOfTransects = channel.ChannelSummary?.Other?.NumberofTransects?.Value,
                SoftwareVersion = channel.QRevVersion,
                FirmwareVersion = channel.Instrument?.FirmwareVersion?.Value,
                MeasurementDevice = new MeasurementDevice(channel.Instrument?.Manufacturer?.Value, channel.Instrument?.Model?.Value, channel.Instrument?.SerialNumber?.Value),
                DischargeCoefficientVariation = channel.ChannelSummary?.Uncertainty?.COV?.Value.AsDouble(),
                MagneticVariation = channel.Processing?.Navigation?.MagneticVariation?.Value.AsDouble(),
            };

            var exponent = channel.Processing?.Extrapolation?.Exponent?.Value.AsDouble();

            var bottomEstimateMethod = channel.Processing?.Extrapolation?.BottomMethod?.Value;

            if (!string.IsNullOrEmpty(bottomEstimateMethod))
            {
                if (BottomMethodsWithExponents.Contains(bottomEstimateMethod))
                    adcpDischargeSection.BottomEstimateExponent = exponent;

                if (_config.BottomEstimateMethods.TryGetValue(bottomEstimateMethod, out var alias))
                {
                    bottomEstimateMethod = alias;
                }

                adcpDischargeSection.BottomEstimateMethod = new BottomEstimateMethodPickList(bottomEstimateMethod);
            }

            var topEstimateMethod = channel.Processing?.Extrapolation?.TopMethod?.Value;

            if (!string.IsNullOrEmpty(topEstimateMethod))
            {
                if (TopMethodsWithExponents.Contains(topEstimateMethod))
                    adcpDischargeSection.BottomEstimateExponent = exponent;

                if (_config.TopEstimateMethods.TryGetValue(topEstimateMethod, out var alias))
                {
                    topEstimateMethod = alias;
                }

                adcpDischargeSection.TopEstimateMethod = new TopEstimateMethodPickList(topEstimateMethod);
            }

            return adcpDischargeSection;
        }

        private static readonly HashSet<string> BottomMethodsWithExponents =
            new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
            {
                "Power",
                "No Slip",
            };

        private static readonly HashSet<string> TopMethodsWithExponents =
            new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
            {
                "Power",
            };
    }
}
