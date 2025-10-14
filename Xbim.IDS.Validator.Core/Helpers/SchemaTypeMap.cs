using IdsLib.IfcSchema;
using System;
using System.Collections.Generic;
using Xbim.Common;
using Xbim.Common.Metadata;

namespace Xbim.IDS.Validator.Core.Helpers
{
    /// <summary>
    /// Provides mapping between IFC types across diffrent schemas where equivalents exist
    /// </summary>
    public class SchemaTypeMap
    {

        /// <summary>
        /// Infers the IFC2x3 equivalent of a new type in IFC 4, where one exists
        /// </summary>
        /// <remarks>Supports the implementation of the 'AirTerminal' issue in IFC2x3: https://github.com/buildingSMART/IDS/issues/116</remarks>
        /// <param name="model"></param>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public static SchemaInference? InferSchemaForEntity(IModel model, string entityType)
        {
            if (_ifc2x3Inferences.TryGetValue(entityType, out var result))
            {
                return result;
            }
            return null;            
        }

        public static ClassInfo? GetSchemaEquivalent(IModel model, string entityType)
        {
            return model.SchemaVersion switch
            {
                Xbim.Common.Step21.XbimSchemaVersion.Ifc2X3 => GetEquivalent(_ifc2x3Equivalents, entityType),
                Xbim.Common.Step21.XbimSchemaVersion.Ifc4 => GetEquivalent(_ifc4Equivalents, entityType),
#if XbimV6
                Xbim.Common.Step21.XbimSchemaVersion.Ifc4x3 => GetEquivalent(_ifc4x3Equivalents, entityType),
#endif
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

        /// <summary>
        /// A map of IFC4 types to IFC2x3 equivalents using a qualifying DefiningType
        /// </summary>
        public static IEnumerable<KeyValuePair<string, SchemaInference>> Ifc2x3TypeMap { get => _ifc2x3Inferences; }

        private static Lazy<IDictionary<string, SchemaInference>> lazySchemaMap = new Lazy<IDictionary<string, SchemaInference>>(() => BuildSchemaInferenceMappings());

        static IDictionary<string, SchemaInference> _ifc2x3Inferences => lazySchemaMap.Value;

        // Inferences: New types in later schemas that can be inferred in older schemas using Type information
        static IDictionary<string, SchemaInference> BuildSchemaInferenceMappings()
        {
            var baseSchema = ExpressMetaData.GetMetadata(new Ifc2x3.EntityFactoryIfc2x3());
            var targetSchema = ExpressMetaData.GetMetadata(new Ifc4.EntityFactoryIfc4());

            var implicitlyMapped = GetMappings(baseSchema, targetSchema);

            var dict = new Dictionary<string, SchemaInference>();

            foreach( var mapping in implicitlyMapped)
            {
                // The IFC2x3 element is usually the supertype of the IFC type, but not always
                string ifc2x3Element = mapping.NewType.ExpressNameUpper switch
                {
                    "IFCVIBRATIONISOLATOR" => "IFCEQUIPMENTELEMENT",    // Special case for IfcVibrationIsolatorType which moved to abstract IfcElementComponent[Type] in IFC4
                    "IFCSPACEHEATER" => "IFCENERGYCONVERSIONDEVICE",    // Spacial case for SpaceHeaterType which moved to IfcFlowTerminal in IFC4
                    _ => mapping.NewType.SuperType.ExpressName
                };
                
                var inference = new SchemaInference(SchemaInfo.SchemaIfc2x3[ifc2x3Element], SchemaInfo.SchemaIfc2x3[mapping.DefinedBy.ExpressName]);
                dict.Add(mapping.NewType.ExpressNameUpper, inference);
            }
            return dict;

        }



        private static IEnumerable<(ExpressType NewType, ExpressType DefinedBy)> GetMappings(ExpressMetaData baseSchema, ExpressMetaData targetSchema)
        {
            var products = targetSchema.ExpressType("IFCPRODUCT");

            foreach (var type in products.AllSubTypes)
            {
                if (baseSchema.ExpressType(type.ExpressNameUpper) == null)
                {
                    // New in the target (newer) schema. Check if a Type exists by convention we can use in the base schema
                    var baseSchemaType = baseSchema.ExpressType(type.ExpressNameUpper + "TYPE");
                    if (baseSchemaType != null)
                    {
                        // New but with Type we can use to discriminate in base schema
                        yield return (type, baseSchemaType);
                    }

                }
            }
        }


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

    /// <summary>
    /// Defines a combination of IfcElement and IfcType that can be used to infer an IFC4+ type in IFC2x3
    /// </summary>
    /// <remarks>e.g. IFC2x3's IFCAIRTERMINAL = IFCFlOWTERMINAL defined by an IFCAIRTERMINALTYPE</remarks>
    public class SchemaInference
    {

        internal SchemaInference(ClassInfo elementType, ClassInfo definingType)
        {
            DefiningType = definingType ?? throw new ArgumentNullException(nameof(definingType));
            ElementType = elementType ?? throw new ArgumentNullException(nameof(elementType));
        }
        /// <summary>
        /// The IfcTypeObject that defines the IFC2x3 element
        /// </summary>
        public ClassInfo DefiningType { get; private set; }

        /// <summary>
        /// The IfcProduct appropriate to the IFC2x3 element
        /// </summary>
        public ClassInfo ElementType { get; private set; }
    }
}
