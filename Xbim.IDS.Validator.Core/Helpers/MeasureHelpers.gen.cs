// generated code, any changes made directly here will be lost
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
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal static IIfcValue NormaliseUnits(this IIfcValue measure, IIfcUnitAssignment? units)
        {
            switch (measure)
            {
                case IfcCountMeasure cnt:
                    return cnt;
                case IfcRatioMeasure ratio:
                    return ratio;
                
                case IfcAmountOfSubstanceMeasure amountofsubstancemeasure:
                {
                    var unit = units.GetUnit(IfcUnitEnum.AMOUNTOFSUBSTANCEUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcAmountOfSubstanceMeasure(amountofsubstancemeasure * si.Power),
                        IIfcConversionBasedUnit cu => new IfcAmountOfSubstanceMeasure(amountofsubstancemeasure * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)amountofsubstancemeasure,
                    };
                }

                case IfcAreaMeasure areameasure:
                {
                    var unit = units.GetUnit(IfcUnitEnum.AREAUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcAreaMeasure(areameasure * si.Power),
                        IIfcConversionBasedUnit cu => new IfcAreaMeasure(areameasure * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)areameasure,
                    };
                }

                case IfcElectricCapacitanceMeasure electriccapacitancemeasure:
                {
                    var unit = units.GetUnit(IfcUnitEnum.ELECTRICCAPACITANCEUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcElectricCapacitanceMeasure(electriccapacitancemeasure * si.Power),
                        IIfcConversionBasedUnit cu => new IfcElectricCapacitanceMeasure(electriccapacitancemeasure * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)electriccapacitancemeasure,
                    };
                }

                case IfcElectricChargeMeasure electricchargemeasure:
                {
                    var unit = units.GetUnit(IfcUnitEnum.ELECTRICCHARGEUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcElectricChargeMeasure(electricchargemeasure * si.Power),
                        IIfcConversionBasedUnit cu => new IfcElectricChargeMeasure(electricchargemeasure * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)electricchargemeasure,
                    };
                }

                case IfcElectricConductanceMeasure electricconductancemeasure:
                {
                    var unit = units.GetUnit(IfcUnitEnum.ELECTRICCONDUCTANCEUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcElectricConductanceMeasure(electricconductancemeasure * si.Power),
                        IIfcConversionBasedUnit cu => new IfcElectricConductanceMeasure(electricconductancemeasure * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)electricconductancemeasure,
                    };
                }

                case IfcElectricCurrentMeasure electriccurrentmeasure:
                {
                    var unit = units.GetUnit(IfcUnitEnum.ELECTRICCURRENTUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcElectricCurrentMeasure(electriccurrentmeasure * si.Power),
                        IIfcConversionBasedUnit cu => new IfcElectricCurrentMeasure(electriccurrentmeasure * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)electriccurrentmeasure,
                    };
                }

                case IfcElectricResistanceMeasure electricresistancemeasure:
                {
                    var unit = units.GetUnit(IfcUnitEnum.ELECTRICRESISTANCEUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcElectricResistanceMeasure(electricresistancemeasure * si.Power),
                        IIfcConversionBasedUnit cu => new IfcElectricResistanceMeasure(electricresistancemeasure * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)electricresistancemeasure,
                    };
                }

                case IfcElectricVoltageMeasure electricvoltagemeasure:
                {
                    var unit = units.GetUnit(IfcUnitEnum.ELECTRICVOLTAGEUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcElectricVoltageMeasure(electricvoltagemeasure * si.Power),
                        IIfcConversionBasedUnit cu => new IfcElectricVoltageMeasure(electricvoltagemeasure * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)electricvoltagemeasure,
                    };
                }

                case IfcEnergyMeasure energymeasure:
                {
                    var unit = units.GetUnit(IfcUnitEnum.ENERGYUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcEnergyMeasure(energymeasure * si.Power),
                        IIfcConversionBasedUnit cu => new IfcEnergyMeasure(energymeasure * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)energymeasure,
                    };
                }

                case IfcForceMeasure forcemeasure:
                {
                    var unit = units.GetUnit(IfcUnitEnum.FORCEUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcForceMeasure(forcemeasure * si.Power),
                        IIfcConversionBasedUnit cu => new IfcForceMeasure(forcemeasure * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)forcemeasure,
                    };
                }

                case IfcFrequencyMeasure frequencymeasure:
                {
                    var unit = units.GetUnit(IfcUnitEnum.FREQUENCYUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcFrequencyMeasure(frequencymeasure * si.Power),
                        IIfcConversionBasedUnit cu => new IfcFrequencyMeasure(frequencymeasure * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)frequencymeasure,
                    };
                }

                case IfcIlluminanceMeasure illuminancemeasure:
                {
                    var unit = units.GetUnit(IfcUnitEnum.ILLUMINANCEUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcIlluminanceMeasure(illuminancemeasure * si.Power),
                        IIfcConversionBasedUnit cu => new IfcIlluminanceMeasure(illuminancemeasure * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)illuminancemeasure,
                    };
                }

                case IfcLengthMeasure lengthmeasure:
                {
                    var unit = units.GetUnit(IfcUnitEnum.LENGTHUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcLengthMeasure(lengthmeasure * si.Power),
                        IIfcConversionBasedUnit cu => new IfcLengthMeasure(lengthmeasure * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)lengthmeasure,
                    };
                }

                case IfcLuminousFluxMeasure luminousfluxmeasure:
                {
                    var unit = units.GetUnit(IfcUnitEnum.LUMINOUSFLUXUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcLuminousFluxMeasure(luminousfluxmeasure * si.Power),
                        IIfcConversionBasedUnit cu => new IfcLuminousFluxMeasure(luminousfluxmeasure * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)luminousfluxmeasure,
                    };
                }

                case IfcLuminousIntensityMeasure luminousintensitymeasure:
                {
                    var unit = units.GetUnit(IfcUnitEnum.LUMINOUSINTENSITYUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcLuminousIntensityMeasure(luminousintensitymeasure * si.Power),
                        IIfcConversionBasedUnit cu => new IfcLuminousIntensityMeasure(luminousintensitymeasure * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)luminousintensitymeasure,
                    };
                }

                case IfcMassMeasure massmeasure:
                {
                    var unit = units.GetUnit(IfcUnitEnum.MASSUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcMassMeasure(massmeasure * si.Power),
                        IIfcConversionBasedUnit cu => new IfcMassMeasure(massmeasure * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)massmeasure,
                    };
                }

                case IfcPlaneAngleMeasure planeanglemeasure:
                {
                    var unit = units.GetUnit(IfcUnitEnum.PLANEANGLEUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcPlaneAngleMeasure(planeanglemeasure * si.Power),
                        IIfcConversionBasedUnit cu => new IfcPlaneAngleMeasure(planeanglemeasure * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)planeanglemeasure,
                    };
                }

                case IfcPowerMeasure powermeasure:
                {
                    var unit = units.GetUnit(IfcUnitEnum.POWERUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcPowerMeasure(powermeasure * si.Power),
                        IIfcConversionBasedUnit cu => new IfcPowerMeasure(powermeasure * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)powermeasure,
                    };
                }

                case IfcPressureMeasure pressuremeasure:
                {
                    var unit = units.GetUnit(IfcUnitEnum.PRESSUREUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcPressureMeasure(pressuremeasure * si.Power),
                        IIfcConversionBasedUnit cu => new IfcPressureMeasure(pressuremeasure * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)pressuremeasure,
                    };
                }

                case IfcRadioActivityMeasure radioactivitymeasure:
                {
                    var unit = units.GetUnit(IfcUnitEnum.RADIOACTIVITYUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcRadioActivityMeasure(radioactivitymeasure * si.Power),
                        IIfcConversionBasedUnit cu => new IfcRadioActivityMeasure(radioactivitymeasure * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)radioactivitymeasure,
                    };
                }

                case IfcThermodynamicTemperatureMeasure thermodynamictemperaturemeasure:
                {
                    var unit = units.GetUnit(IfcUnitEnum.THERMODYNAMICTEMPERATUREUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcThermodynamicTemperatureMeasure(thermodynamictemperaturemeasure * si.Power),
                        IIfcConversionBasedUnit cu => new IfcThermodynamicTemperatureMeasure(thermodynamictemperaturemeasure * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)thermodynamictemperaturemeasure,
                    };
                }

                case IfcTimeMeasure timemeasure:
                {
                    var unit = units.GetUnit(IfcUnitEnum.TIMEUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcTimeMeasure(timemeasure * si.Power),
                        IIfcConversionBasedUnit cu => new IfcTimeMeasure(timemeasure * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)timemeasure,
                    };
                }

                case IfcVolumeMeasure volumemeasure:
                {
                    var unit = units.GetUnit(IfcUnitEnum.VOLUMEUNIT);
                    return unit switch
                    {
                        IIfcSIUnit si => new IfcVolumeMeasure(volumemeasure * si.Power),
                        IIfcConversionBasedUnit cu => new IfcVolumeMeasure(volumemeasure * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)volumemeasure,
                    };
                }
                default:
                    throw new NotImplementedException($"Measure not implemented: {measure}");
            }
            
        }
    }
}
