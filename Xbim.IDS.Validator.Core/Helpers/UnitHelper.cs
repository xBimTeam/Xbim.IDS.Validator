using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc4.Interfaces;

namespace Xbim.IDS.Validator.Core.Helpers
{
    public static class UnitHelper
    {

        public static IIfcNamedUnit GetUnit(this IIfcUnitAssignment units, IfcUnitEnum unitType) 
        {
            IIfcNamedUnit? nu = units.Units.OfType<IIfcSIUnit>().FirstOrDefault(u => u.UnitType == unitType);
            if (nu == null)
                nu = units.Units.OfType<IIfcConversionBasedUnit>().FirstOrDefault(u => u.UnitType == unitType);
            return nu;
        }

       
    }
}
