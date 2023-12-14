using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.IDS.Validator.Core.Helpers;
using Xbim.Ifc4.Interfaces;
using Xbim.InformationSpecifications;
using static Xbim.InformationSpecifications.PartOfFacet;

namespace Xbim.IDS.Validator.Core.Extensions
{
    /// <summary>
    /// Extension methods to help build xbim queries for <see cref="PartOfFacet"/>
    /// </summary>
    internal static class IfcRelationsExtensions
    {


        /// <summary>
        /// Selects all elements that are related to the Relationship as children, matching the criteria 
        /// </summary>
        /// <param name="rel"></param>
        /// <param name="facet"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<IIfcObjectDefinition> GetRelatedIfcObjects(this IEnumerable<IIfcRelationship> rel, PartOfFacet facet)
        {
            if (facet is null)
            {
                throw new ArgumentNullException(nameof(facet));
            }

            var innerType = TypeHelper.GetImplementedIEnumerableType(rel.GetType());

            // Note: PredefinedType verification only supports IfcObject.ObjectType
            // TODO: Support for IfcElementType.ElementType and IfcTypeProcess.ProcesType. Support for intrinsic Predefined Enum. Type Inheritance of PDT
            if (typeof(IIfcRelAggregates).IsAssignableFrom(innerType))
            {
                var items = rel.Cast<IIfcRelAggregates>();
                return items.Where(r => facet.EntityType?.IfcType?.IsSatisfiedBy(r.RelatingObject.GetType().Name, true) == true)
                    .Where(r => facet.EntityType?.PredefinedType == null || r.RelatingObject is IIfcObject p && facet.EntityType?.PredefinedType?.IsSatisfiedBy(p.ObjectType, true) == true)
                    .SelectMany(r => r.RelatedObjects).OfType<IIfcObjectDefinition>();
            }
            else if (typeof(IIfcRelNests).IsAssignableFrom(innerType))
            {
                var items = rel.Cast<IIfcRelNests>();
                return items.Where(r => facet.EntityType?.IfcType?.IsSatisfiedBy(r.RelatingObject.GetType().Name, true) == true)
                    .Where(r => facet.EntityType?.PredefinedType == null || r.RelatingObject is IIfcObject p && facet.EntityType?.PredefinedType?.IsSatisfiedBy(p.ObjectType, true) == true)
                    .SelectMany(r => r.RelatedObjects).OfType<IIfcObjectDefinition>();
            }
            else if (typeof(IIfcRelAssignsToGroup).IsAssignableFrom(innerType))
            {

                var items = rel.Cast<IIfcRelAssignsToGroup>();
                return items.Where(r => facet.EntityType?.IfcType?.IsSatisfiedBy(r.RelatingGroup.GetType().Name, true) == true)
                    .Where(r => facet.EntityType?.PredefinedType == null || r.RelatingGroup is IIfcObject p && facet.EntityType?.PredefinedType?.IsSatisfiedBy(p.ObjectType, true) == true)
                    .SelectMany(r => r.RelatedObjects).OfType<IIfcObjectDefinition>();
            }
            else if (typeof(IIfcRelContainedInSpatialStructure).IsAssignableFrom(innerType))
            {
                var items = rel.Cast<IIfcRelContainedInSpatialStructure>();
                return items.Where(r => facet.EntityType?.IfcType?.IsSatisfiedBy(r.RelatingStructure.GetType().Name, true) == true)
                    .Where(r => facet.EntityType?.PredefinedType == null || r.RelatingStructure is IIfcObject p && facet.EntityType?.PredefinedType?.IsSatisfiedBy(p.ObjectType, true) == true)
                    .SelectMany(r => r.RelatedElements).OfType<IIfcProduct>();
            }
            else
            {
                throw new NotImplementedException($"{innerType} is not a supported relationship");
            }
        }

        /// <summary>
        /// Filters a set of <see cref="IIfcObject"/> entries to those satisifying the PartOf facet
        /// </summary>
        /// <param name="ent"></param>
        /// <param name="facet"></param>
        /// <returns></returns>
        public static IEnumerable<IIfcObjectDefinition> WhereHasPartOfRelationship(this IEnumerable<IIfcObjectDefinition> ent, PartOfFacet facet)
        {
            // Where classification matches directly, or an object's Type matches classification

            var filtered = ent.Where(e => facet.EntityType?.IfcType?.IsSatisfiedBy(e.GetType().Name, true) == true);
            return (facet.GetRelation()) switch
            {
                PartOfRelation.IfcRelAggregates => filtered.Where(e => e.IsDecomposedBy.OfType<IIfcRelAggregates>().Any()),
                PartOfRelation.IfcRelNests => filtered.Where(e => e.IsDecomposedBy.OfType<IIfcRelNests>().Any()),
                PartOfRelation.IfcRelAssignsToGroup => filtered.Where(e => e.HasAssignments.OfType<IIfcRelAssignsToGroup>().Any()),
                PartOfRelation.IfcRelContainedInSpatialStructure => filtered.Where(e => e.IsDecomposedBy.OfType<IIfcRelAggregates>().Any()),    // TODO Validate this

                _ => ent
            };
        }

        /// <summary>
        /// Gets Parents associated to an entity that match the requirement
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="facet"></param>
        /// <returns></returns>
        public static IEnumerable<IIfcObjectDefinition> GetPartsForEntity(IIfcObjectDefinition obj, PartOfFacet facet)
        {
            var relation = facet.GetRelation();


            var ancestry = relation switch
            {
                PartOfRelation.IfcRelAggregates => obj.Decomposes.Select(r => r.RelatingObject)
                    .UnionAncestry(relation),
                PartOfRelation.IfcRelNests => obj.Nests.Select(r => r.RelatingObject)
                    .UnionAncestry(relation),
                PartOfRelation.IfcRelAssignsToGroup => obj.HasAssignments.OfType<IIfcRelAssignsToGroup>().Select(r => r.RelatingGroup)
                    .UnionAncestry(relation),
                PartOfRelation.IfcRelContainedInSpatialStructure => obj is IIfcProduct pr
                    ? pr.IsContainedIn != null
                        ? new[] { pr.IsContainedIn }.UnionAncestry(relation) // Has Immediate link to SpatialElement
                                                                             // Check for indirect link
                        : new[] { pr }.UnionAncestry(PartOfRelation.IfcRelAggregates).Cast<IIfcProduct>().Select(p => p.IsContainedIn).Where(c => c != null).UnionAncestry(relation)

                    : Enumerable.Empty<IIfcObjectDefinition>(),

                _ => Enumerable.Empty<IIfcObjectDefinition>()
            };

            return ancestry.Where(p => MatchesPart(p, facet));

        }

        private static bool MatchesPart(IIfcObjectDefinition obj, PartOfFacet facet)
        {
            if (facet.EntityType == null) return true;
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            // TODO: Limited support for Predefined Type only. Omits complex scenarios, Type Inheritance, Enums etc
            return (facet.EntityType?.IfcType?.IsSatisfiedBy(obj.GetType().Name, true) == true) &&
                (facet.EntityType?.PredefinedType == null || obj is IIfcObject p && facet.EntityType?.PredefinedType?.IsSatisfiedBy(p.ObjectType, true) == true)
                ;
        }

        private static IEnumerable<IIfcObjectDefinition> UnionAncestry(this IEnumerable<IIfcObjectDefinition> objs, PartOfRelation relation)
        {
            if (!objs.Any())
                return objs;
            return relation switch
            {
                PartOfRelation.IfcRelAggregates => objs.Union(objs.SelectMany(o => o.Decomposes.Select(r => r.RelatingObject)).UnionAncestry(relation)),
                PartOfRelation.IfcRelNests => objs.Union(objs.SelectMany(o => o.Nests.Select(r => r.RelatingObject)).UnionAncestry(relation)),
                PartOfRelation.IfcRelAssignsToGroup => objs.Union(objs.SelectMany(o => o.HasAssignments.OfType<IIfcRelAssignsToGroup>().Select(r => r.RelatingGroup)).UnionAncestry(relation)),
                PartOfRelation.IfcRelContainedInSpatialStructure => objs.Union(objs.Cast<IIfcSpatialStructureElement>().Select(sp => sp.IsContainedIn).Where(s => s != null).UnionAncestry(relation)),

                _ => Enumerable.Empty<IIfcObjectDefinition>()
            };


        }
    }
}
