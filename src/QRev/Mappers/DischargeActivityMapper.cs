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

namespace QRev.Mappers
{
    internal class DischargeActivityMapper
    {
        private FieldVisitInfo FieldVisitInfo { get; }
        private Config Config { get; }

        private UnitConverter UnitConverter { get; set; }

        public DischargeActivityMapper(Config config, FieldVisitInfo fieldVisitInfo)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            FieldVisitInfo = fieldVisitInfo ?? throw new ArgumentNullException(nameof(fieldVisitInfo));
        }

        public DischargeActivity Map(Channel channel)
        {
            ValidateInternalMetricUnits(channel);

            UnitConverter = new UnitConverter(Config.ImperialUnits);

            var unitSystem = CreateUnitSystem();

            var dischargeActivity = CreateDischargeActivityWithSummary(channel, unitSystem);

            SetDischargeSection(dischargeActivity, channel, unitSystem);

            return dischargeActivity;
        }

        private void ValidateInternalMetricUnits(Channel channel)
        {
            ThrowIfUnexpectedUnits( "cms", nameof(channel.ChannelSummary.Discharge.Total          ), channel.ChannelSummary?.Discharge?.Total?.unitsCode );
            ThrowIfUnexpectedUnits("cms", nameof(channel.ChannelSummary.Discharge.Middle          ), channel.ChannelSummary?.Discharge?.Middle?.unitsCode);
            ThrowIfUnexpectedUnits( "m",   nameof(channel.ChannelSummary.Other.MeanWidth          ), channel.ChannelSummary?.Other?.MeanWidth?.unitsCode );
            ThrowIfUnexpectedUnits( "sqm", nameof(channel.ChannelSummary.Other.MeanArea           ), channel.ChannelSummary?.Other?.MeanArea?.unitsCode );
            ThrowIfUnexpectedUnits( "mps", nameof(channel.ChannelSummary.Other.MeanQoverA         ), channel.ChannelSummary?.Other?.MeanQoverA?.unitsCode );
            ThrowIfUnexpectedUnits( "deg", nameof(channel.Processing.Navigation.MagneticVariation ), channel.Processing?.Navigation?.MagneticVariation?.unitsCode );
            ThrowIfUnexpectedUnits( "m",   nameof(channel.Processing.Depth.ADCPDepth              ), channel.Processing?.Depth?.ADCPDepth?.unitsCode );
        }

        private void ThrowIfUnexpectedUnits(string expectedUnits, string name, string actualUnits)
        {
            if (string.IsNullOrEmpty(actualUnits))
                // A value might not be provided.
                return;

            if (actualUnits != expectedUnits)
                throw new ArgumentException($"Expected units '{expectedUnits}' for {name} but found '{actualUnits}'");
        }

        private UnitSystem CreateUnitSystem()
        {
            UnitConverter = new UnitConverter(Config.ImperialUnits);

            return new UnitSystem
            {
                DistanceUnitId = GetUnitId(UnitConverter.DistanceUnitGroup),
                AreaUnitId = GetUnitId(UnitConverter.AreaUnitGroup),
                VelocityUnitId = GetUnitId(UnitConverter.VelocityUnitGroup),
                DischargeUnitId = GetUnitId(UnitConverter.DischargeUnitGroup),
            };
        }

        private string GetUnitId(string unitGroup)
        {
            var unit = UnitConverter.Units[unitGroup];

            return UnitConverter.IsImperial
                ? unit.ImperialId
                : unit.MetricId;
        }

        private DischargeActivity CreateDischargeActivityWithSummary(Channel channel, UnitSystem unitSystem)
        {
            var factory = new DischargeActivityFactory(unitSystem);

            var totalDischarge = channel.ChannelSummary?.Discharge?.Total?.Value ??
                                 throw new ArgumentException("No total discharge amount provided");

            //Discharge summary:
            var measurementPeriod = GetMeasurementPeriod();
            var dischargeActivity = factory.CreateDischargeActivity(
                measurementPeriod,
                UnitConverter.ConvertDischarge(totalDischarge.AsDouble()));

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
            return new DateTimeInterval(FieldVisitInfo.StartDate, FieldVisitInfo.EndDate);
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
            var middleDischarge = channel.ChannelSummary?.Discharge?.Middle?.Value;

            var percentOfDischargeMeasured = middleDischarge.HasValue
                ? (double?) (100.0 * UnitConverter.ConvertDischarge(middleDischarge.Value.AsDouble()) / dischargeActivity.Discharge.Value)
                : null;

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
                WidthValue = UnitConverter.ConvertDistance(channel.ChannelSummary?.Other?.MeanWidth?.Value.AsDouble()),
                AreaValue = UnitConverter.ConvertArea(channel.ChannelSummary?.Other?.MeanArea?.Value.AsDouble()),
                VelocityAverageValue = UnitConverter.ConvertVelocity(channel.ChannelSummary?.Other?.MeanQoverA?.Value.AsDouble()),
                NumberOfTransects = channel.ChannelSummary?.Other?.NumberofTransects?.Value,
                SoftwareVersion = channel.QRevVersion,
                FirmwareVersion = channel.Instrument?.FirmwareVersion?.Value,
                MeasurementDevice = new MeasurementDevice(channel.Instrument?.Manufacturer?.Value, channel.Instrument?.Model?.Value, channel.Instrument?.SerialNumber?.Value),
                DischargeCoefficientVariation = channel.ChannelSummary?.Uncertainty?.COV?.Value.AsDouble(),
                MagneticVariation = channel.Processing?.Navigation?.MagneticVariation?.Value.AsDouble(),
                TransducerDepth = UnitConverter.ConvertDistance(channel.Processing?.Depth?.ADCPDepth?.Value.AsDouble()),
                PercentOfDischargeMeasured = percentOfDischargeMeasured,
            };

            var exponent = channel.Processing?.Extrapolation?.Exponent?.Value.AsDouble();

            var bottomEstimateMethod = channel.Processing?.Extrapolation?.BottomMethod?.Value;

            if (!string.IsNullOrEmpty(bottomEstimateMethod))
            {
                if (BottomMethodsWithExponents.Contains(bottomEstimateMethod))
                    adcpDischargeSection.BottomEstimateExponent = exponent;

                if (Config.BottomEstimateMethods.TryGetValue(bottomEstimateMethod, out var alias))
                    bottomEstimateMethod = alias;

                adcpDischargeSection.BottomEstimateMethod = new BottomEstimateMethodPickList(bottomEstimateMethod);
            }

            var topEstimateMethod = channel.Processing?.Extrapolation?.TopMethod?.Value;

            if (!string.IsNullOrEmpty(topEstimateMethod))
            {
                if (TopMethodsWithExponents.Contains(topEstimateMethod))
                    adcpDischargeSection.BottomEstimateExponent = exponent;

                if (Config.TopEstimateMethods.TryGetValue(topEstimateMethod, out var alias))
                    topEstimateMethod = alias;

                adcpDischargeSection.TopEstimateMethod = new TopEstimateMethodPickList(topEstimateMethod);
            }

            var depthCompositeEnabled = "On".Equals(channel.Processing?.Depth?.CompositeDepth?.Value, StringComparison.InvariantCultureIgnoreCase);
            var depthReference = depthCompositeEnabled
                ? $"{DepthReferenceType.Composite}"
                : channel.Processing?.Depth?.Reference?.Value;

            if (!string.IsNullOrEmpty(depthReference))
            {
                if (Config.DepthReferences.TryGetValue(depthReference, out var alias))
                    depthReference = alias;

                if (Enum.TryParse<DepthReferenceType>(depthReference, true, out var depthReferenceType))
                    adcpDischargeSection.DepthReference = depthReferenceType;
            }

            var navigationCompositeEnabled = "On".Equals(channel.Processing?.Navigation?.CompositeTrack?.Value, StringComparison.InvariantCultureIgnoreCase);
            var navigationReference = channel.Processing?.Navigation?.Reference?.Value;

            if (!navigationCompositeEnabled && !string.IsNullOrEmpty(navigationReference))
            {
                if (Config.NavigationMethods.TryGetValue(navigationReference, out var alias))
                    navigationReference = alias;

                adcpDischargeSection.NavigationMethod = new NavigationMethodPickList(navigationReference);
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
