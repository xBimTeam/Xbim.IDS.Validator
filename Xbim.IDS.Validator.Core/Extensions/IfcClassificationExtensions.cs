using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Xbim.IDS.Validator.Core.Helpers;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Extensions
{
    /// <summary>
    /// Extension methods to help build xbim queries for <see cref="IfcClassificationFacet"/>
    /// </summary>
    internal static class IfcClassificationExtensions
    {


        /// <summary>
        /// Selects all objects using a matching classification, inferring from Types where appropriate
        /// </summary>
        /// <remarks>The filtering also accounts for hierarchical classification schemes</remarks>
        /// <param name="relAssociates"></param>
        /// <param name="facet"></param>
        /// <returns></returns>
        public static IEnumerable<IIfcObjectDefinition> GetIfcObjectsUsingClassification(this IEnumerable<IIfcRelAssociatesClassification> relAssociates, IfcClassificationFacet facet)
        {
            if (facet is null)
            {
                throw new ArgumentNullException(nameof(facet));
            }

            // When Getting objects we cast the net wide, starting at IIfcRelAssociatesClassification and traversing from Types to instances
            // This differs to filtering (WhereAssociatedToClassification), where we determine if existing selection meets classification criteria

            var matchingRels = relAssociates.FilterByClassificationFacet(facet);
            // Find objects with matching classifications
            IEnumerable<IIfcObjectDefinition> results = matchingRels
                .SelectMany(r => r.RelatedObjects).OfType<IIfcObject>();

            // Append Types with matching classifications
            var typeResults = matchingRels.SelectMany(r => r.RelatedObjects).OfType<IIfcTypeObject>();

            results = results.Union(typeResults);
            // Append instances of objects defined by matching types
            results = results.Union(typeResults.SelectMany(t => t.Types.SelectMany(tt => tt.RelatedObjects)));

            return results;

        }

        /// <summary>
        /// Filters a set of <see cref="IIfcObjectDefinition"/> entries to those satisifying the classification facet
        /// </summary>
        /// <param name="ent"></param>
        /// <param name="facet"></param>
        /// <returns></returns>
        public static IEnumerable<IIfcObjectDefinition> WhereAssociatedWithClassification(this IEnumerable<IIfcObjectDefinition> ent, IfcClassificationFacet facet)
        {
            // Where classification matches directly, or an object's Type matches classification
            IEnumerable<IIfcObjectDefinition> results = ent.Where(e => e.HasAssociations.OfType<IIfcRelAssociatesClassification>()
                .FilterByClassificationFacet(facet).Any() ||
                e is IIfcObject t && t.IsTypedBy.Any(r => r.RelatingType.HasAssociations.OfType<IIfcRelAssociatesClassification>()
                .FilterByClassificationFacet(facet).Any()));


            return results;
        }


        /// <summary>
        /// Selects all classificationReferences matching criteria for the supplied element (relation)
        /// </summary>
        /// <param name="relAssociates"></param>
        /// <param name="facet"></param>
        /// <returns></returns>
        public static IEnumerable<IIfcClassificationSelect> GetClassificationReferences(this IEnumerable<IIfcRelAssociatesClassification> relAssociates, IfcClassificationFacet facet)
        {
            if (facet is null)
            {
                throw new ArgumentNullException(nameof(facet));
            }
            return relAssociates.FilterByClassificationFacet(facet)
                .Select(r => r.RelatingClassification).OfType<IIfcClassificationSelect>();

        }

        private static IEnumerable<IIfcRelAssociatesClassification> FilterByClassificationFacet(this IEnumerable<IIfcRelAssociatesClassification> relAssociates, IfcClassificationFacet facet)
        {
            return relAssociates.Where(r => 
                (facet.Identification == null || r.RelatingClassification.GetClassificationIdentifiers().Any(id => facet.Identification.IsSatisfiedBy(id, true))) &&
                (facet.ClassificationSystem == null || facet.ClassificationSystem.IsSatisfiedBy(r.RelatingClassification.GetSystemName(), true)));

        }

        /// <summary>
        /// Gets the set of all classification Identifiers for the initial <see cref="IIfcClassificationSelect"/>, traversing up the hierarchy 
        /// as required
        /// </summary>
        /// <param name="classSelect"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetClassificationIdentifiers(this IIfcClassificationSelect classSelect, ILogger? logger = null)
        {
            if (classSelect == null) yield break;

            switch (classSelect)
            {
                case IIfcClassificationReference classRef:
                    foreach(var item in GetClassificationIdentifiers((IIfcClassificationReferenceSelect)classRef, logger))
                    {
                        yield return item;
                    }
                    break;

                case IIfcClassification classification:
                    if(classification.HasReferences != null)
                    {
                        foreach(var reference in classification.HasReferences)
                        {
                            foreach(var item in GetClassificationIdentifiers((IIfcClassificationReferenceSelect)reference, logger))
                            {
                                yield return item;
                            }
                        }
                    }
                    break;

                default:
                    logger.LogNotImplemented($"ClassificationSelect not implemented: {classSelect.GetType().Name}");
                    yield break;
            }
        }

        private static IEnumerable<string> GetClassificationIdentifiers(this IIfcClassificationReferenceSelect select, ILogger? logger = null)
        {
            if(select == null) yield break;

            switch (select) 
            {
                case IIfcClassificationReference classRef:
                    if(IsFilled(classRef.Identification))
                    {
                        yield return classRef.Identification.Value.ToString();
                    }
                    // TODO: Should we look at Name as well as Identifier
                    // Recurse up hierarchy to find parent identifiers
                    foreach(var item in GetClassificationIdentifiers(classRef.ReferencedSource, logger))
                    {
                        yield return item;
                    }
                    break;
                case IIfcClassification _:  // Not relevant to Identifiers - just the System
                    yield break;
                default:
                    logger.LogNotImplemented($"ClassificationReferenceSelect not implemented: {select.GetType().Name}");
                    yield break;
            }

        }

        /// <summary>
        /// Gets the name the classification system <see cref="IIfcClassificationSelect"/> ultimately belongs to
        /// </summary>
        /// <param name="select"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static string? GetSystemName(this IIfcClassificationSelect select, ILogger? logger = null)
        {
            switch (select)
            {
                case IIfcClassificationReference reference:
                    return GetSystemName(reference.ReferencedSource, logger);

                case IIfcClassification classification:
                    return classification.Name.Value?.ToString();

                default:
                    logger.LogNotImplemented($"ClassificationSelect not implemented: {select.GetType().Name}");
                    return null;
            }
        }

        private static string? GetSystemName(this IIfcClassificationReferenceSelect select, ILogger? logger = null)
        {
            switch(select)
            {
                case IIfcClassificationReference reference:
                    return GetSystemName(reference.ReferencedSource, logger);

                case IIfcClassification classification:
                    return classification.Name.Value?.ToString();
                default:
                    logger.LogNotImplemented($"ClassificationReferenceSelect not implemented: {select.GetType().Name}");
                    return null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsFilled([NotNullWhen(true)] IfcIdentifier? value)
        {
            return (!string.IsNullOrEmpty(value?.Value?.ToString()));
        }

    }
}
