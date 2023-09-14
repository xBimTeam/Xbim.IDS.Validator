using System;
using System.Collections.Generic;
using Xbim.Common;
using Xbim.InformationSpecifications.Helpers;

namespace Xbim.IDS.Validator.Core.Helpers
{
    internal class SchemaTypeMap
    {

        public static SchemaInference? InferSchemaForEntity(IModel model, string entityType)
        {
            if(!_ifc2x3Inferences.ContainsKey(entityType))
            {
                return null;
            }
            else
            {
                return _ifc2x3Inferences[entityType];
            }
            
        }

        public static ClassInfo? GetSchemaEquivalent(IModel model, string entityType)
        {
            return model.SchemaVersion switch
            {
                Xbim.Common.Step21.XbimSchemaVersion.Ifc2X3 => GetEquivalent(_ifc2x3Equivalents, entityType),
                Xbim.Common.Step21.XbimSchemaVersion.Ifc4 => GetEquivalent(_ifc4Equivalents, entityType),
                Xbim.Common.Step21.XbimSchemaVersion.Ifc4x3 => GetEquivalent(_ifc4x3Equivalents, entityType),
                _ => null,
            };
        }

        private static ClassInfo? GetEquivalent(IDictionary<string, ClassInfo> schema, string entityType)
        {
            if(schema.ContainsKey(entityType))
            {
                return schema[entityType];
            }
            return null;
        }

        // Inferences: New types in later schemas that can be inferred in older schemas using Type information

        static IDictionary<string, SchemaInference> _ifc2x3Inferences = new Dictionary<string, SchemaInference>()
        {
            ["IFCELECTRICAPPLIANCE"] = new SchemaInference(SchemaInfo.SchemaIfc2x3["IfcFlowTerminal"], SchemaInfo.SchemaIfc2x3["IfcElectricApplianceType"]),
            ["IFCAIRTERMINAL"] = new SchemaInference(SchemaInfo.SchemaIfc2x3["IfcFlowTerminal"], SchemaInfo.SchemaIfc2x3["IfcAirTerminalType"]),
            ["IFCLIGHTFIXTURE"] = new SchemaInference(SchemaInfo.SchemaIfc2x3["IfcFlowTerminal"], SchemaInfo.SchemaIfc2x3["IfcLightFixtureType"]),
            ["IFCLAMP"] = new SchemaInference(SchemaInfo.SchemaIfc2x3["IfcFlowTerminal"], SchemaInfo.SchemaIfc2x3["IfcLampType"]),
            ["IFCSANITARYTERMINAL"] = new SchemaInference(SchemaInfo.SchemaIfc2x3["IfcFlowTerminal"], SchemaInfo.SchemaIfc2x3["IfcSanitaryTerminalType"]),
            ["IFCOUTLET"] = new SchemaInference(SchemaInfo.SchemaIfc2x3["IfcFlowTerminal"], SchemaInfo.SchemaIfc2x3["IfcOutletType"]),

        };


        // Substitutions: where a type is deprecated / removed and there's a forwarding type we can use

        // types that don't yet exist in IFC2x3 but have a close equivalent
        static IDictionary<string, ClassInfo> _ifc2x3Equivalents = new Dictionary<string, ClassInfo>()

        {
            ["IFCDOORTYPE"] = SchemaInfo.SchemaIfc2x3["IfcDoorStyle"]!,
            ["IFCWINDOWTYPE"] = SchemaInfo.SchemaIfc2x3["IfcWindowStyle"]!,
            // Instances
            ["IFCBUILTELEMENT"] = SchemaInfo.SchemaIfc2x3["IfcBuildingElement"]!,

        };

        // types that don't exist (or no longer exist) in IFC4 but have a close equivalent 
        static IDictionary<string, ClassInfo> _ifc4Equivalents = new Dictionary<string, ClassInfo>()

        {
            ["IFCBUILTELEMENT"] = SchemaInfo.SchemaIfc4["IfcBuildingElement"]!,
            // TODO: IfcElectricalElement etc

        };

        // types that don't exist (or no longer exist) in IFC4x3 but have a close equivalent
        static IDictionary<string, ClassInfo> _ifc4x3Equivalents = new Dictionary<string, ClassInfo>()

        {
            ["IFCBUILDINGELEMENT"] = SchemaInfo.SchemaIfc4x3["IfcBuiltElement"]!,
            ["IFCDOORSTYLE"] = SchemaInfo.SchemaIfc4x3["IfcDoorType"]!,
            ["IFCWINDOWSTYLE"] = SchemaInfo.SchemaIfc4x3["IfcWindowType"]!,
            // TODO: 4x3 entities where there's a mapping
        };


    }

    internal class SchemaInference
    {

        public SchemaInference(ClassInfo elementType, ClassInfo definingType)
        {
            DefiningType = definingType ?? throw new ArgumentNullException(nameof(definingType));
            ElementType = elementType ?? throw new ArgumentNullException(nameof(elementType));
        }
        /// <summary>
        /// The 
        /// </summary>
        public ClassInfo DefiningType { get; private set; }

        public ClassInfo ElementType { get; private set; }
    }
}
