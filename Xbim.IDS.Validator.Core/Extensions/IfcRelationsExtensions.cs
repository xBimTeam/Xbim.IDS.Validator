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
            else if (typeof(IIfcRelVoidsElement).IsAssignableFrom(innerType))
            {
                var items = rel.Cast<IIfcRelVoidsElement>();
                return items.Where(r => facet.EntityType?.IfcType?.IsSatisfiedBy(r.RelatingBuildingElement.GetType().Name, true) == true)
                  .Where(r => facet.EntityType?.PredefinedType == null || r.RelatingBuildingElement is IIfcObject p && facet.EntityType?.PredefinedType?.IsSatisfiedBy(p.ObjectType, true) == true)
                  .Select(r => r.RelatedOpeningElement).OfType<IIfcOpeningElement>()
                  .SelectMany(o => o.HasFillings).Select(r => r.RelatedBuildingElement).OfType<IIfcObjectDefinition>();
            }
            else if (typeof(IIfcRelationship).IsAssignableFrom(innerType))
            {
                // return all
                return Enumerable.Empty<IIfcObjectDefinition>()
                    .Union(GetRelatedIfcObjects(rel.OfType<IIfcRelAggregates>(), facet))
                    .Union(GetRelatedIfcObjects(rel.OfType<IIfcRelNests>(), facet))
                    .Union(GetRelatedIfcObjects(rel.OfType<IIfcRelAssignsToGroup>(), facet))
                    .Union(GetRelatedIfcObjects(rel.OfType<IIfcRelContainedInSpatialStructure>(), facet))
                    .Union(GetRelatedIfcObjects(rel.OfType<IIfcRelVoidsElement>(), facet));
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

            IEnumerable<IIfcObjectDefinition> ancestry = GetAncestryForEntity(obj, relation);

            return ancestry.Where(p => MatchesPart(p, facet));

        }

        private static IEnumerable<IIfcObjectDefinition> GetAncestryForEntity(IIfcObjectDefinition obj, PartOfRelation relation)
        {
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
                        : new[] { pr }.UnionAncestry(PartOfRelation.IfcRelAggregates).OfType<IIfcProduct>().Select(p => p.IsContainedIn).Where(c => c != null).UnionAncestry(relation)

                    : Enumerable.Empty<IIfcObjectDefinition>(),
                PartOfRelation.IfcRelVoidsFillsElement => obj is IIfcElement el
                    ? el.FillsVoids.Select(r => r.RelatingOpeningElement).OfType<IIfcOpeningElement>().Select(o => o.VoidsElements).Select(r => r.RelatingBuildingElement)
                        .UnionAncestry(relation)
                    : Enumerable.Empty<IIfcObjectDefinition>(),
                PartOfRelation.Undefined => Enumerable.Empty<IIfcObjectDefinition>()
                    .Union(GetAncestryForEntity(obj, PartOfRelation.IfcRelAggregates))
                    .Union(GetAncestryForEntity(obj, PartOfRelation.IfcRelNests))
                    .Union(GetAncestryForEntity(obj, PartOfRelation.IfcRelAssignsToGroup))
                    .Union(GetAncestryForEntity(obj, PartOfRelation.IfcRelContainedInSpatialStructure))
                    .Union(GetAncestryForEntity(obj, PartOfRelation.IfcRelVoidsFillsElement))
                    ,

                _ => throw new NotImplementedException($"Relation {relation} is not supported")
            };
            return ancestry;
        }

        private static bool MatchesPart(IIfcObjectDefinition obj, PartOfFacet facet)
        {
            if (facet.EntityType == null) return true;
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            // Based on latest thinking at https://github.com/buildingSMART/IDS/pull/240#issuecomment-1929367078
            return (facet.EntityType?.IfcType?.IsSatisfiedBy(obj.GetType().Name, true) == true) &&
                IfcEntityTypeExtensions.MatchesPredefinedType(obj, facet.EntityType)
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
                PartOfRelation.IfcRelVoidsFillsElement => objs.Union(objs.OfType<IIfcElement>()
                    .SelectMany(el => el.FillsVoids.Select(r => r.RelatingOpeningElement).OfType<IIfcOpeningElement>().Select(o => o.VoidsElements).Select(r => r.RelatingBuildingElement)).UnionAncestry(relation))                        ,
                _ => Enumerable.Empty<IIfcObjectDefinition>()
            };


        }
    }
}
