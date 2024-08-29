// generated code, any changes made directly here will be lost
// Generated using Xids: 1.0.1
using Microsoft.Extensions.Logging;
using System;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;

namespace Xbim.IDS.Validator.Core.Helpers
{
    internal static partial class MeasureHelpers
    {
        /// <summary>
        /// Converts a Measure quantity to the normalised unit quantity.
        /// </summary>
        /// <remarks>Implemented solely against IFC4 since all other schemas use this interface internally</remarks>
        /// <param name="measure"></param>
        /// <param name="units"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal static IIfcValue NormaliseUnits(this IIfcValue measure, IIfcUnitAssignment? units, ILogger logger)
        {
            switch (measure)
            {
                case IfcCountMeasure cnt:
                    return cnt;
                case IfcRatioMeasure ratio:
                    return ratio;
                
                case IfcAbsorbedDoseMeasure amount:
                {
                    var unit = units.GetUnit(IfcUnitEnum.ABSORBEDDOSEUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcAbsorbedDoseMeasure(amount * si.Power),
                        IIfcConversionBasedUnit cu => new IfcAbsorbedDoseMeasure(amount * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)amount,
                    };
                }

                case IfcAmountOfSubstanceMeasure amount:
                {
                    var unit = units.GetUnit(IfcUnitEnum.AMOUNTOFSUBSTANCEUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcAmountOfSubstanceMeasure(amount * si.Power),
                        IIfcConversionBasedUnit cu => new IfcAmountOfSubstanceMeasure(amount * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)amount,
                    };
                }

                case IfcAreaMeasure amount:
                {
                    var unit = units.GetUnit(IfcUnitEnum.AREAUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcAreaMeasure(amount * si.Power),
                        IIfcConversionBasedUnit cu => new IfcAreaMeasure(amount * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)amount,
                    };
                }

                case IfcDoseEquivalentMeasure amount:
                {
                    var unit = units.GetUnit(IfcUnitEnum.DOSEEQUIVALENTUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcDoseEquivalentMeasure(amount * si.Power),
                        IIfcConversionBasedUnit cu => new IfcDoseEquivalentMeasure(amount * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)amount,
                    };
                }

                case IfcElectricCapacitanceMeasure amount:
                {
                    var unit = units.GetUnit(IfcUnitEnum.ELECTRICCAPACITANCEUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcElectricCapacitanceMeasure(amount * si.Power),
                        IIfcConversionBasedUnit cu => new IfcElectricCapacitanceMeasure(amount * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)amount,
                    };
                }

                case IfcElectricChargeMeasure amount:
                {
                    var unit = units.GetUnit(IfcUnitEnum.ELECTRICCHARGEUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcElectricChargeMeasure(amount * si.Power),
                        IIfcConversionBasedUnit cu => new IfcElectricChargeMeasure(amount * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)amount,
                    };
                }

                case IfcElectricConductanceMeasure amount:
                {
                    var unit = units.GetUnit(IfcUnitEnum.ELECTRICCONDUCTANCEUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcElectricConductanceMeasure(amount * si.Power),
                        IIfcConversionBasedUnit cu => new IfcElectricConductanceMeasure(amount * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)amount,
                    };
                }

                case IfcElectricCurrentMeasure amount:
                {
                    var unit = units.GetUnit(IfcUnitEnum.ELECTRICCURRENTUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcElectricCurrentMeasure(amount * si.Power),
                        IIfcConversionBasedUnit cu => new IfcElectricCurrentMeasure(amount * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)amount,
                    };
                }

                case IfcElectricResistanceMeasure amount:
                {
                    var unit = units.GetUnit(IfcUnitEnum.ELECTRICRESISTANCEUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcElectricResistanceMeasure(amount * si.Power),
                        IIfcConversionBasedUnit cu => new IfcElectricResistanceMeasure(amount * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)amount,
                    };
                }

                case IfcElectricVoltageMeasure amount:
                {
                    var unit = units.GetUnit(IfcUnitEnum.ELECTRICVOLTAGEUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcElectricVoltageMeasure(amount * si.Power),
                        IIfcConversionBasedUnit cu => new IfcElectricVoltageMeasure(amount * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)amount,
                    };
                }

                case IfcEnergyMeasure amount:
                {
                    var unit = units.GetUnit(IfcUnitEnum.ENERGYUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcEnergyMeasure(amount * si.Power),
                        IIfcConversionBasedUnit cu => new IfcEnergyMeasure(amount * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)amount,
                    };
                }

                case IfcForceMeasure amount:
                {
                    var unit = units.GetUnit(IfcUnitEnum.FORCEUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcForceMeasure(amount * si.Power),
                        IIfcConversionBasedUnit cu => new IfcForceMeasure(amount * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)amount,
                    };
                }

                case IfcFrequencyMeasure amount:
                {
                    var unit = units.GetUnit(IfcUnitEnum.FREQUENCYUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcFrequencyMeasure(amount * si.Power),
                        IIfcConversionBasedUnit cu => new IfcFrequencyMeasure(amount * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)amount,
                    };
                }

                case IfcIlluminanceMeasure amount:
                {
                    var unit = units.GetUnit(IfcUnitEnum.ILLUMINANCEUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcIlluminanceMeasure(amount * si.Power),
                        IIfcConversionBasedUnit cu => new IfcIlluminanceMeasure(amount * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)amount,
                    };
                }

                case IfcInductanceMeasure amount:
                {
                    var unit = units.GetUnit(IfcUnitEnum.INDUCTANCEUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcInductanceMeasure(amount * si.Power),
                        IIfcConversionBasedUnit cu => new IfcInductanceMeasure(amount * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)amount,
                    };
                }

                case IfcLengthMeasure amount:
                {
                    var unit = units.GetUnit(IfcUnitEnum.LENGTHUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcLengthMeasure(amount * si.Power),
                        IIfcConversionBasedUnit cu => new IfcLengthMeasure(amount * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)amount,
                    };
                }

                case IfcLuminousFluxMeasure amount:
                {
                    var unit = units.GetUnit(IfcUnitEnum.LUMINOUSFLUXUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcLuminousFluxMeasure(amount * si.Power),
                        IIfcConversionBasedUnit cu => new IfcLuminousFluxMeasure(amount * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)amount,
                    };
                }

                case IfcLuminousIntensityMeasure amount:
                {
                    var unit = units.GetUnit(IfcUnitEnum.LUMINOUSINTENSITYUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcLuminousIntensityMeasure(amount * si.Power),
                        IIfcConversionBasedUnit cu => new IfcLuminousIntensityMeasure(amount * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)amount,
                    };
                }

                case IfcMagneticFluxDensityMeasure amount:
                {
                    var unit = units.GetUnit(IfcUnitEnum.MAGNETICFLUXDENSITYUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcMagneticFluxDensityMeasure(amount * si.Power),
                        IIfcConversionBasedUnit cu => new IfcMagneticFluxDensityMeasure(amount * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)amount,
                    };
                }

                case IfcMagneticFluxMeasure amount:
                {
                    var unit = units.GetUnit(IfcUnitEnum.MAGNETICFLUXUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcMagneticFluxMeasure(amount * si.Power),
                        IIfcConversionBasedUnit cu => new IfcMagneticFluxMeasure(amount * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)amount,
                    };
                }

                case IfcMassMeasure amount:
                {
                    var unit = units.GetUnit(IfcUnitEnum.MASSUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcMassMeasure(amount * si.Power),
                        IIfcConversionBasedUnit cu => new IfcMassMeasure(amount * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)amount,
                    };
                }

                case IfcNonNegativeLengthMeasure amount:
                    return amount;

                case IfcPlaneAngleMeasure amount:
                {
                    var unit = units.GetUnit(IfcUnitEnum.PLANEANGLEUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcPlaneAngleMeasure(amount * si.Power),
                        IIfcConversionBasedUnit cu => new IfcPlaneAngleMeasure(amount * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)amount,
                    };
                }

                case IfcPositiveLengthMeasure amount:
                    return amount;

                case IfcPositivePlaneAngleMeasure amount:
                    return amount;

                case IfcPowerMeasure amount:
                {
                    var unit = units.GetUnit(IfcUnitEnum.POWERUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcPowerMeasure(amount * si.Power),
                        IIfcConversionBasedUnit cu => new IfcPowerMeasure(amount * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)amount,
                    };
                }

                case IfcPressureMeasure amount:
                {
                    var unit = units.GetUnit(IfcUnitEnum.PRESSUREUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcPressureMeasure(amount * si.Power),
                        IIfcConversionBasedUnit cu => new IfcPressureMeasure(amount * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)amount,
                    };
                }

                case IfcRadioActivityMeasure amount:
                {
                    var unit = units.GetUnit(IfcUnitEnum.RADIOACTIVITYUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcRadioActivityMeasure(amount * si.Power),
                        IIfcConversionBasedUnit cu => new IfcRadioActivityMeasure(amount * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)amount,
                    };
                }

                case IfcSectionalAreaIntegralMeasure amount:
                    return amount;

                case IfcSolidAngleMeasure amount:
                {
                    var unit = units.GetUnit(IfcUnitEnum.SOLIDANGLEUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcSolidAngleMeasure(amount * si.Power),
                        IIfcConversionBasedUnit cu => new IfcSolidAngleMeasure(amount * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)amount,
                    };
                }

                case IfcThermalConductivityMeasure amount:
                    return amount;

                case IfcThermodynamicTemperatureMeasure amount:
                {
                    var unit = units.GetUnit(IfcUnitEnum.THERMODYNAMICTEMPERATUREUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcThermodynamicTemperatureMeasure(amount * si.Power),
                        IIfcConversionBasedUnit cu => new IfcThermodynamicTemperatureMeasure(amount * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)amount,
                    };
                }

                case IfcTimeMeasure amount:
                {
                    var unit = units.GetUnit(IfcUnitEnum.TIMEUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcTimeMeasure(amount * si.Power),
                        IIfcConversionBasedUnit cu => new IfcTimeMeasure(amount * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)amount,
                    };
                }

                case IfcVolumeMeasure amount:
                {
                    var unit = units.GetUnit(IfcUnitEnum.VOLUMEUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcVolumeMeasure(amount * si.Power),
                        IIfcConversionBasedUnit cu => new IfcVolumeMeasure(amount * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)amount,
                    };
                }
                default:
                    logger.LogWarning("Measure {measure} is unsupported for normalisation.", measure.GetType().Name);
                    return measure;
            }
            
        }
    }
}
