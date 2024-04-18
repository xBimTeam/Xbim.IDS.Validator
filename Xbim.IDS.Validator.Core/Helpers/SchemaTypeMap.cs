using IdsLib.IfcSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Common;
using Xbim.Common.Metadata;

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


        private static Lazy<IDictionary<string, SchemaInference>> lazySchemaMap = new Lazy<IDictionary<string, SchemaInference>>(() => BuildSchemaInferenceMappings());

        static IDictionary<string, SchemaInference> _ifc2x3Inferences => lazySchemaMap.Value;

        // Inferences: New types in later schemas that can be inferred in older schemas using Type information
        static IDictionary<string, SchemaInference> BuildSchemaInferenceMappings()
        {
            var baseSchema = ExpressMetaData.GetMetadata(typeof(Ifc2x3.EntityFactoryIfc2x3).Module);
            var targetSchema = ExpressMetaData.GetMetadata(typeof(Ifc4.EntityFactoryIfc4).Module);

            var implicitlyMapped = GetMappings(baseSchema, targetSchema);

            var dict = new Dictionary<string, SchemaInference>();

            foreach( var mapping in implicitlyMapped )
            {
                var inference = new SchemaInference(SchemaInfo.SchemaIfc2x3[mapping.NewType.SuperType.ExpressName], SchemaInfo.SchemaIfc2x3[mapping.DefinedBy.ExpressName]);
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
                    // New in the targer schema. Check if a Type exists by convention we can use
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
