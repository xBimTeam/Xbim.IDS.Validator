
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGenerator
{
    internal class MeasureHelperGenerator
    {
       
        internal static string Execute()
        {
            var source = stub;
            string searchKw = $"<PlaceHolder>\r\n";
            var sb = new StringBuilder();
            //int iCnt = 0;

#pragma warning disable CS0618 // Type or member is obsolete - not yet implemented in idslib
            var measures = Xbim.InformationSpecifications.Helpers.SchemaInfo.IfcMeasures;
#pragma warning restore CS0618 // Type or member is obsolete

            foreach (var key in measures.Keys)
            {

                var item = measures[key];
                if (!item.IfcMeasure.EndsWith("Measure"))
                {
                    continue;
                }

                if (item.UnitTypeEnum.StartsWith("IfcDerivedUnitEnum") || string.IsNullOrEmpty(item.UnitTypeEnum))
                {
                    // TODO: Handle Derived?
                    continue;
                }
                var namedUnit = namedUnitCase;
                namedUnit = namedUnit.Replace("<measure>", item.IfcMeasure);
                namedUnit = namedUnit.Replace("<unit>", item.UnitTypeEnum);
                namedUnit = namedUnit.Replace("<typed>", item.IfcMeasure.ToLowerInvariant().Substring(3));
                sb.Append(namedUnit);
            }

            source = source.Replace(searchKw, sb.ToString());

            return source;
        }

        private const string namedUnitCase = @"
                case <measure> <typed>:
                {
                    var unit = units.GetUnit(<unit>);
                    return unit switch
                    {
                        IIfcSIUnit si => new <measure>(<typed> * si.Power),
                        IIfcConversionBasedUnit cu => new <measure>(<typed> * (double)cu.ConversionFactor.ValueComponent.Value),
                        _ => (IIfcValue)<typed>,
                    };
                }
";

//        private const string singleEnumStub = @"
//                case <case> <var>:
//<PlaceHolder>
//                    converted = null;
//                    return false;
//";

        private const string stub = @"// generated code, any changes made directly here will be lost
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
        /// <returns></returns>
        /// <exception cref=""NotImplementedException""></exception>
        internal static IIfcValue NormaliseUnits(this IIfcValue measure, IIfcUnitAssignment? units)
        {
            switch (measure)
            {
                case IfcCountMeasure cnt:
                    return cnt;
                case IfcRatioMeasure ratio:
                    return ratio;
                <PlaceHolder>
                default:
                    throw new NotImplementedException($""Measure not implemented: {measure}"");
            }
            
        }
    }
}
";
    }
}

