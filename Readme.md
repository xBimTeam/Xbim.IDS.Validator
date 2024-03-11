# Xbim.IDS.Validator


Xbim.IDS.Validator is a library to help validate IFC models against the 
BuildingSMART [Information Delivery Specification](https://github.com/buildingSMART/IDS/blob/master/Documentation/README.md) (IDS) schema.

Powered by xbim Tookit, this library can be used to translate IDS files into an executable specification, 
which can be run against any IFC2x3 or IFC4 model and provide a detailed breakdown of the results.


## How do I use it?

Given an IDS file such as [example.ids](https://github.com/andyward/Xbim.IDS.Validator/blob/master/Xbim.IDS.Validator.Core.Tests/TestModels/Example.ids])


```csharp

// during startup:

serviceCollection.AddIdsValidation();

// Open a model

IModel model = IfcStore.Open(ifcModelFile);

// Get IdsModelValidator from DI provider / or inject to your service
var idsValidator = provider.GetRequiredService<IIdsModelValidator>();

ValidationOutcome outcome = await idsValidator.ValidateAgainstIdsAsync(model, "example.ids", logger)

foreach (ValidationRequirement requirement in results.ExecutedRequirements)
{
    // ApplicableResults contains details of the applicable IFC entities tested
    var entitiesTested = requirement.ApplicableResults.Count();
    var entitiesPassed = requirement.ApplicableResults.Count(e => e.ValidationStatus == ValidationStatus.Pass);
    Console.WriteLine("[{0,-8}] : [{1}/{2}] met {3} specification > '{4}' ", 
        requirement.Status, 
        entitiesPassed, entitiesTested,
        requirement.Specification.Cardinality.Description, 
        requirement.Specification.Name
        );

    // And now you could detail the failure reasons against entities.
}
```

## How do I install it?

```
dotnet add package Xbim.IDS.Validator.Core
```

## How much of the IDS spec does this support?

It currently supports:
- Applicability and Requirements of the following
    - [x] Entities & Predefined Types
    - [x] Attributes
    - [x] Classifications
        - Includes Classification hierarchies/ancestry
        - Includes inheriting from Type
    - [x] Properties
        - Includes inheriting from Type
        - Support for all IfcSimpleProperty implementations
        - Support for IfcElementQuantities
        - Support for Unit conversion
    - [x] Materials
    - [x] PartOf
    - [ ] Xbim.IDS custom extensions (Document, IfcRelation)
- [Complex Restrictions](https://github.com/buildingSMART/IDS/blob/master/Documentation/restrictions.md):
    - [x] Enumerations
    - [x] Patterns (Regex)
    - [x] Bounds
    - [x] Structure (Min/Max length)
- Edge cases
    - [x] Support for 1.e-6 precision tolerance
- Restrictions can be used in both Applicability filtering and Requirements verification
- Reading of IDS in v0.9 Schema in Xml and JSON formats
- Optionality of Facets
- Cardinality of Specification (Expected, Prohibited, Optional)
- Support for validating models in following IFC Schemas
    - [x] IFC2x3
    - [x] IFC4 schemas
    - [x] IFC4x3-ADD1
- Extensions
    - [x] Case Insensitivity testing
    - [x] Optional Support for Ifc Type Inheritance
    - [x] Optional Support for querying Derived Properties
    - [x] Optional support for running IDS against COBie (using Xbim.COBieExpress extensions)

The library has been tested against the [IDS test suite](https://github.com/buildingSMART/IDS/blob/master/Documentation/developer-guide.md#checking-ids-against-ifc)

Currently only one minor cases are unimplemented. (See PropertySet Skipped Tests).

## To-do list

- [ ] Support for Xbim.XIDS extensions (Documents)
    - [ ] IfcType SubClasses extension
- [x] Support for IFC4x3

- [ ] Testing Pre-defined Properties. e.g. IFCDOORPANELPROPERTIES.PanelOperation
