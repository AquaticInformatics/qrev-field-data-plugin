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
        private FieldVisitInfo FieldVisitInfo { get; }
        private Config Config { get; }

        public bool IsMetric { get; private set; }

        public DischargeActivityMapper(Config config, FieldVisitInfo fieldVisitInfo)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            FieldVisitInfo = fieldVisitInfo ?? throw new ArgumentNullException(nameof(fieldVisitInfo));
        }

        public DischargeActivity Map(Channel channel)
        {
            IsMetric = InferMetricUnits(channel);

            var unitSystem = IsMetric
                ? Units.MetricUnitSystem
                : Units.ImperialUnitSystem;

            var dischargeActivity = CreateDischargeActivityWithSummary(channel, unitSystem);

            SetDischargeSection(dischargeActivity, channel, unitSystem);

            return dischargeActivity;
        }

        private bool InferMetricUnits(Channel channel)
        {
            ThrowIfUnexpectedUnits( "cms", nameof(channel.ChannelSummary.Discharge.Total          ), channel.ChannelSummary?.Discharge?.Total?.unitsCode );
            ThrowIfUnexpectedUnits( "m",   nameof(channel.ChannelSummary.Other.MeanWidth          ), channel.ChannelSummary?.Other?.MeanWidth?.unitsCode );
            ThrowIfUnexpectedUnits( "sqm", nameof(channel.ChannelSummary.Other.MeanArea           ), channel.ChannelSummary?.Other?.MeanArea?.unitsCode );
            ThrowIfUnexpectedUnits( "mps", nameof(channel.ChannelSummary.Other.MeanQoverA         ), channel.ChannelSummary?.Other?.MeanQoverA?.unitsCode );
            ThrowIfUnexpectedUnits( "deg", nameof(channel.Processing.Navigation.MagneticVariation ), channel.Processing?.Navigation?.MagneticVariation?.unitsCode );
            ThrowIfUnexpectedUnits( "m",   nameof(channel.Processing.Depth.ADCPDepth              ), channel.Processing?.Depth?.ADCPDepth?.unitsCode );

            return true;
        }

        private void ThrowIfUnexpectedUnits(string expectedUnits, string name, string actualUnits)
        {
            if (string.IsNullOrEmpty(actualUnits))
                // A value might not be provided.
                return;

            if (actualUnits != expectedUnits)
                throw new ArgumentException($"Expected units '{expectedUnits}' for {name} but found '{actualUnits}'");
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
                TransducerDepth = channel.Processing?.Depth?.ADCPDepth?.Value.AsDouble()
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
