using System.Text;

namespace CodeGenerator
{
    internal class MeasureHelperGenerator
    {
       
        internal static string Execute()
        {
            var source = stub;
            string searchKw = $"<PlaceHolder>\r\n";
            var sb = new StringBuilder();


#pragma warning disable CS0618 // Type or member is obsolete - not yet implemented in idslib
            // We can't yet generate code from IdsLib.IfcSchema.IfcMeasureInformation since it doesn't provide the xbim typename or any means 
            // to go from IFCLENGTHMEASURE to IfcLengthMeasure. XIDS provides the ConcreteClasses which can be used to ID
            // an xbim IfcMeasure type from the Ifc4.Interfaces namespace
            var measures = Xbim.InformationSpecifications.Helpers.SchemaInfo.IfcMeasures;
#pragma warning restore CS0618 // Type or member is obsolete

            foreach (var key in measures.Keys)
            {

                var item = measures[key];
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
                var ifcTypeName = item.ConcreteClasses.FirstOrDefault()!.Split('.').Last();
                unitSwitchBlock = unitSwitchBlock.Replace("<measure>", ifcTypeName);
                unitSwitchBlock = unitSwitchBlock.Replace("<unit>", item.UnitTypeEnum);

                sb.Append(unitSwitchBlock);
            }

            source = source.Replace(searchKw, sb.ToString());
            source = source.Replace("<version>", Xbim.InformationSpecifications.Xids.AssemblyVersion);
            return source;
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

