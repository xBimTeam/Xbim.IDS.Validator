using IdsLib.IfcSchema;
using System.Text;
using Xbim.Common.Metadata;
using Xbim.Ifc4.SharedBldgElements;

namespace CodeGenerator
{
    internal class MeasureHelperGenerator
    {
       
        internal static string Execute()
        {
            var source = stub;
            string searchKw = $"<PlaceHolder>\r\n";
            var sb = new StringBuilder();

            var measures = IdsLib.IfcSchema.SchemaInfo.AllMeasureInformation;

            foreach (var item in measures)
            {
                if (!item.IfcMeasure.EndsWith("MEASURE"))
                {
                    continue;
                }
                if (item.UnitTypeEnum.StartsWith("IfcDerivedUnitEnum"))
                {
                    // TODO: Handle Derived?
                    continue;
                }
                var unitSwitchBlock = string.IsNullOrEmpty(item.UnitTypeEnum) ? unitLess : namedUnitCase;
                var ifcTypeName = GetTypeName(item.IfcMeasure);
                unitSwitchBlock = unitSwitchBlock.Replace("<measure>", ifcTypeName);
                unitSwitchBlock = unitSwitchBlock.Replace("<unit>", item.UnitTypeEnum);

                sb.Append(unitSwitchBlock);
            }

            source = source.Replace(searchKw, sb.ToString());
            source = source.Replace("<version>", Xbim.InformationSpecifications.Xids.AssemblyVersion);
            source = source.Replace("<versionIds>", IdsLib.LibraryInformation.AssemblyVersion);
            return source;
        }

        private static readonly ExpressMetaData ifc4Schema = ExpressMetaData.GetMetadata(new Xbim.Ifc4.EntityFactoryIfc4x1());

        // Converts the uppercase Measure name to xbim Class name in IFC4 Interfaces namespace
        private static string GetTypeName(string ifcMeasure)
        {
            if(IdsLib.IfcSchema.SchemaInfo.TryParseIfcDataType(ifcMeasure, out var ifcType))
            {
                // Because IFC4 is the main Interfaces schema it should contain all the measures regardless if 2x3 or 4x3
                return ifc4Schema.ExpressType(ifcMeasure).Name;
            }
            // Fallback (will typically break as Uppercase)
            return ifcMeasure;
            
        }

        private const string namedUnitCase = @"
                case <measure> amount:
                {
                    var unit = units.GetUnit(<unit>);
                    return unit switch
                    {
                        IIfcSIUnit si => new <measure>(amount * si.Power),
                        IIfcConversionBasedUnit cu => new <measure>(amount * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)amount,
                    };
                }
";

        private const string unitLess = @"
                case <measure> amount:
                    return amount;
";



        private const string stub = @"// generated code, any changes made directly here will be lost
// Generated using Xids: <version>
// Generated using ids-lib: <versionIds>
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
        /// <param name=""measure""></param>
        /// <param name=""units""></param>
        /// <param name=""logger""></param>
        /// <returns></returns>
        /// <exception cref=""NotImplementedException""></exception>
        internal static IIfcValue NormaliseUnits(this IIfcValue measure, IIfcUnitAssignment? units, ILogger logger)
        {
            switch (measure)
            {
                case IfcCountMeasure cnt:
                    return cnt;
                case IfcRatioMeasure ratio:
                    return ratio;
                <PlaceHolder>
                default:
                    logger.LogWarning(""Measure {measure} is unsupported for normalisation."", measure.GetType().Name);
                    return measure;
            }
            
        }
    }
}
";
    }
}

