﻿//---------------------------------------------------------------------
// <copyright file="ODataResourceMetadataContext.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

namespace Microsoft.OData.Evaluation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.OData;
    using Microsoft.OData.Core;
    using Microsoft.OData.Edm;
    using Microsoft.OData.Edm.Vocabularies;
    using Microsoft.OData.Edm.Vocabularies.V1;

    /// <summary>
    /// Default implementation of <see cref="IODataResourceMetadataContext"/>
    /// </summary>
    internal abstract class ODataResourceMetadataContext : IODataResourceMetadataContext
    {
        /// <summary>
        /// Empty array of properties.
        /// </summary>
        private static readonly IEnumerable<KeyValuePair<string, object>> EmptyProperties = Enumerable.Empty<KeyValuePair<string, object>>();

        /// <summary>
        /// The resource instance.
        /// </summary>
        private readonly ODataResourceBase resource;

        /// <summary>
        /// The context object to answer basic questions regarding the type of the resource.
        /// </summary>
        private readonly IODataResourceTypeContext typeContext;

        /// <summary>
        /// The key property name and value pairs of the resource.
        /// </summary>
        private IList<KeyValuePair<string, object>> keyProperties;

        /// <summary>
        /// The ETag property name and value pairs of the resource.
        /// </summary>
        private IEnumerable<KeyValuePair<string, object>> etagProperties;

        /// <summary>
        /// The selected navigation properties.
        /// </summary>
        private IEnumerable<IEdmNavigationProperty> selectedNavigationProperties;

        /// <summary>
        /// The selected stream properties.
        /// </summary>
        private IDictionary<string, IEdmStructuralProperty> selectedStreamProperties;

        /// <summary>
        /// The selected bindable operations.
        /// </summary>
        private IEnumerable<IEdmOperation> selectedBindableOperations;

        /// <summary>
        /// Constructs an instance of <see cref="ODataResourceMetadataContext"/>.
        /// </summary>
        /// <param name="resource">The resource instance.</param>
        /// <param name="typeContext">The context object to answer basic questions regarding the type of the resource.</param>
        protected ODataResourceMetadataContext(ODataResourceBase resource, IODataResourceTypeContext typeContext)
        {
            Debug.Assert(resource != null, "resource != null");
            Debug.Assert(typeContext != null, "typeContext != null");

            this.resource = resource;
            this.typeContext = typeContext;
        }

        /// <summary>
        /// The resource instance.
        /// </summary>
        public ODataResourceBase Resource
        {
            get { return this.resource; }
        }

        /// <summary>
        /// The context object to answer basic questions regarding the type of the resource.
        /// </summary>
        public IODataResourceTypeContext TypeContext
        {
            get { return this.typeContext; }
        }

        /// <summary>
        /// The actual structured type of the resource.
        /// </summary>
        public IEdmStructuredType ActualResourceType { get; set; }

        /// <summary>
        /// The actual structured type of the resource, i.e. ODataResource.TypeName.
        /// </summary>
        public abstract string ActualResourceTypeName { get; }

        /// <summary>
        /// The key property name and value pairs of the resource.
        /// </summary>
        public abstract ICollection<KeyValuePair<string, object>> KeyProperties { get; }

        /// <summary>
        /// The ETag property name and value pairs of the resource.
        /// </summary>
        public abstract IEnumerable<KeyValuePair<string, object>> ETagProperties { get; }

        /// <summary>
        /// The selected navigation properties.
        /// </summary>
        public abstract IEnumerable<IEdmNavigationProperty> SelectedNavigationProperties { get; }

        /// <summary>
        /// The selected stream properties.
        /// </summary>
        public abstract IDictionary<string, IEdmStructuralProperty> SelectedStreamProperties { get; }

        /// <summary>
        /// The selected bindable operations.
        /// </summary>
        public abstract IEnumerable<IEdmOperation> SelectedBindableOperations { get; }

        /// <summary>
        /// Creates an instance of <see cref="ODataResourceMetadataContext"/>.
        /// </summary>
        /// <param name="resource">The resource instance.</param>
        /// <param name="typeContext">The context object to answer basic questions regarding the type of the resource.</param>
        /// <param name="serializationInfo">The serialization info of the resource for writing without model.</param>
        /// <param name="actualResourceType">The structured type of the resource.</param>
        /// <param name="metadataContext">The metadata context to use.</param>
        /// <param name="selectedProperties">The selected properties.</param>
        /// <param name="metadataSelector">Informs the metadata builder which properties, functions, actions, links to omit.</param>
        /// <param name="requiresId">True if the resource requires an id, false if it is a non-deleted resource in a delta request payload.</param>
        /// <returns>A new instance of <see cref="ODataResourceMetadataContext"/>.</returns>
        internal static ODataResourceMetadataContext Create(
            ODataResourceBase resource,
            IODataResourceTypeContext typeContext,
            ODataResourceSerializationInfo serializationInfo,
            IEdmStructuredType actualResourceType,
            IODataMetadataContext metadataContext,
            SelectedPropertiesNode selectedProperties,
            ODataMetadataSelector metadataSelector,
            bool requiresId = true)
        {
            if (serializationInfo != null)
            {
                return new ODataResourceMetadataContextWithoutModel(resource, typeContext, serializationInfo, requiresId)
                {
                    ActualResourceType = actualResourceType
                };
            }

            return new ODataResourceMetadataContextWithModel(resource, typeContext, actualResourceType, metadataContext, selectedProperties, metadataSelector, requiresId)
            {
                ActualResourceType = actualResourceType
            };
        }

        /// <summary>
        /// Get key value pair array for specific odata resource using specific entity type
        /// </summary>
        /// <param name="resource">The resource instance.</param>
        /// <param name="serializationInfo">The serialization info of the resource for writing without model.</param>
        /// <param name="actualEntityType">The edm entity type of the resource</param>
        /// <param name="requiresId">Whether key properties are required to be returned</param>
        /// <returns>Key value pair list.</returns>
        internal static IList<KeyValuePair<string, object>> GetKeyProperties(
            ODataResourceBase resource,
            ODataResourceSerializationInfo serializationInfo,
            IEdmEntityType actualEntityType,
            bool requiresId)
        {
            Debug.Assert(resource != null, "GetKeyProperties called for a null resource.");

            IList<KeyValuePair<string, object>> keyProperties = null;

            // if we have serializationInfo, try that first
            if (serializationInfo != null)
            {
                keyProperties = ODataResourceMetadataContextWithoutModel.GetPropertiesBySerializationInfoPropertyKind(resource, ODataPropertyKind.Key, actualEntityType);
            }

            // if we didn't get any keys from serializationInfo, try using entity type
            if ((keyProperties == null || keyProperties.Count == 0) && actualEntityType != null)
            {
                keyProperties = GetPropertyValues(actualEntityType.Key(), resource, actualEntityType, requiresId);
            }

            if (!ValidateEntityTypeHasKeyProperties(keyProperties, resource, actualEntityType, requiresId))
            {
                return Enumerable.Empty<KeyValuePair<string, object>>().ToArray();
            }

            return keyProperties;
        }

        private static List<KeyValuePair<string, object>> GetPropertyValues(IEnumerable<IEdmStructuralProperty> properties, ODataResourceBase resource, IEdmStructuredType actualResourceType, bool isRequired)
        {
            object primitiveValue;
            // Initialize capacity to 1 because this method is mostly called to get values of key properties
            // and most entities have a single key property
            List<KeyValuePair<string, object>> keys = new List<KeyValuePair<string, object>>(1);
            foreach (IEdmStructuralProperty property in properties)
            {
                if (TryGetPrimitiveOrEnumPropertyValue(resource, property.Name, actualResourceType, isRequired, out primitiveValue))
                {
                    keys.Add(new KeyValuePair<string, object>(property.Name, primitiveValue));
                }
            }

            return keys;
        }

        /// <summary>
        /// Gets the value for a primitive or enum property.
        /// </summary>
        /// <param name="resource">The resource to get the property value.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="entityType">The entity type of the resource.</param>
        /// <param name="isRequired">true, if the property value is required to be non-null.</param>
        /// <param name="value">returned value, or null if no value is found.</param>
        /// <returns>true, if the primitive value is found, otherwise false.</returns>
        private static bool TryGetPrimitiveOrEnumPropertyValue(ODataResourceBase resource, string propertyName, IEdmStructuredType entityType, bool isRequired, out object value)
        {
            Debug.Assert(resource != null, "resource != null");

            ODataPropertyInfo propertyInfo = null;
            if (resource.NonComputedProperties != null)
            {
                // We use a manual loop instead of a for-loop to avoid
                // closure allocations given how frequently this method is called.
                foreach (ODataPropertyInfo item in resource.NonComputedProperties)
                {
                    if (item.Name == propertyName)
                    {
                        propertyInfo = item;
                        break;
                    }
                }
            }

            if (propertyInfo == null)
            {
                if (isRequired)
                {
                    throw new ODataException(Error.Format(SRResources.EdmValueUtils_PropertyDoesntExist, GetResourceTypeName(resource, entityType), propertyName));
                }
                else
                {
                    value = null;
                    return false;
                }
            }

            value = GetPrimitiveOrEnumPropertyValue(resource, entityType, propertyInfo, isRequired);
            return true;
        }

        /// <summary>
        /// Gets the value for a primitive or enum property. For primitive type, returns its CLR value; for enum type, returns OData enum value.
        /// </summary>
        /// <param name="resource">The resource from which the property was retrieved.</param>
        /// <param name="entityType">The entity type used to get the property value.</param>
        /// <param name="propertyInfo">The ODataProperty to get the value from.</param>
        /// <param name="validateNotNull">true if property must not be null, false otherwise.</param>
        /// <returns>The value of the property.</returns>
        private static object GetPrimitiveOrEnumPropertyValue(ODataResourceBase resource, IEdmStructuredType entityType, ODataPropertyInfo propertyInfo, bool validateNotNull)
        {
            object propertyValue = (propertyInfo as ODataProperty)?.Value;
            if (propertyValue == null && validateNotNull)
            {
                throw new ODataException(Error.Format(SRResources.ODataResourceMetadataContext_NullKeyValue, propertyInfo.Name, GetResourceTypeName(resource, entityType)));
            }

            if (propertyValue is ODataValue && !(propertyValue is ODataEnumValue))
            {
                if (propertyValue is ODataJsonElementValue)
                {
                    return propertyValue;
                }

                throw new ODataException(Error.Format(SRResources.ODataResourceMetadataContext_KeyOrETagValuesMustBePrimitiveValues, propertyInfo.Name, GetResourceTypeName(resource, entityType)));
            }

            return propertyValue;
        }

        /// <summary>
        /// Validates that the resource has key properties.
        /// </summary>
        /// <param name="keyProperties">Key properties of the resource.</param>
        /// <param name="resource">The resource that contains the properties.</param>
        /// <param name="actualEntityType">The type of the resource.</param>
        /// <param name="throwOnError">Whether to throw if validation fails.</param>
        /// <returns>True, if validation succeeds, or false if validation fails.</returns>
        private static bool ValidateEntityTypeHasKeyProperties(IList<KeyValuePair<string, object>> keyProperties, ODataResourceBase resource, IEdmStructuredType actualEntityType, bool throwOnError)
        {
            if (keyProperties == null || keyProperties.Count == 0)
            {
                if (throwOnError)
                {
                    throw new ODataException(Error.Format(SRResources.ODataResourceMetadataContext_EntityTypeWithNoKeyProperties, GetResourceTypeName(resource, actualEntityType)));
                }
                else
                {
                    return false;
                }
            }

            for (int keyProperty = 0; keyProperty < keyProperties.Count; keyProperty++)
            {
                object keyValue = keyProperties[keyProperty].Value;
                if (keyValue == null || (keyValue is ODataValue && !(keyValue is ODataEnumValue)))
                {
                    if (throwOnError)
                    {
                        throw new ODataException(Error.Format(SRResources.ODataResourceMetadataContext_NullKeyValue, keyProperties[keyProperty].Key, GetResourceTypeName(resource, actualEntityType)));
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the property name value pairs filtered by serialization property kind.
        /// </summary>
        /// <param name="resource">The resource to get the properties from.</param>
        /// <param name="propertyKind">The serialization info property kind.</param>
        /// <param name="actualEntityType">The entity type of the resource.</param>
        /// <returns>The property name value pairs filtered by serialization property kind.</returns>
        private static KeyValuePair<string, object>[] GetPropertiesBySerializationInfoPropertyKind(ODataResourceBase resource, ODataPropertyKind propertyKind, IEdmStructuredType actualEntityType)
        {
            Debug.Assert(resource != null, "resource != null");
            Debug.Assert(propertyKind == ODataPropertyKind.Key || propertyKind == ODataPropertyKind.ETag, "propertyKind == ODataPropertyKind.Key || propertyKind == ODataPropertyKind.ETag");

            List<KeyValuePair<string, object>> properties = new List<KeyValuePair<string, object>>();
            if (resource.NonComputedProperties != null)
            {
                foreach(ODataPropertyInfo propertyInfo in resource.NonComputedProperties)
                {
                    if(propertyInfo.SerializationInfo != null && propertyInfo.SerializationInfo.PropertyKind == propertyKind)
                    {
                        properties.Add(new KeyValuePair<string, object>(propertyInfo.Name, GetPrimitiveOrEnumPropertyValue(resource, actualEntityType, propertyInfo, false)));
                    }
                }                
            }

            return properties.ToArray();
        }



        /// <summary>
        /// Gets the resource type name of the specified resource.
        /// </summary>
        /// <param name="resource">The resource instance.</param>
        /// <param name="resourceType">The resource's type.</param>
        /// <returns>The name of the resource's type.</returns>
        private static string GetResourceTypeName(ODataResourceBase resource, IEdmStructuredType resourceType)
        {
            return string.IsNullOrEmpty(resource.TypeName) ? resourceType?.FullTypeName() : resource.TypeName;
        }

        /// <summary>
        /// Implementation of <see cref="IODataResourceMetadataContext"/> based on serialization info.
        /// </summary>
        private sealed class ODataResourceMetadataContextWithoutModel : ODataResourceMetadataContext
        {
            /// <summary>
            /// Empty array of navigation properties.
            /// </summary>
            private static readonly IEnumerable<IEdmNavigationProperty> EmptyNavigationProperties = Enumerable.Empty<IEdmNavigationProperty>();

            /// <summary>
            /// Empty dictionary of stream properties.
            /// </summary>
            private static readonly Dictionary<string, IEdmStructuralProperty> EmptyStreamProperties = new Dictionary<string, IEdmStructuralProperty>(StringComparer.Ordinal);

            /// <summary>
            /// Empty array of operations.
            /// </summary>
            private static readonly IEnumerable<IEdmOperation> EmptyOperations = Enumerable.Empty<IEdmOperation>();

            /// <summary>
            /// True if the resource requires an id, false if it is a non-deleted resource in a delta request payload.
            /// </summary>
            private bool requiresId;

            /// <summary>
            /// The serialization info of the resource for writing without model.
            /// </summary>
            private readonly ODataResourceSerializationInfo serializationInfo;

            /// <summary>
            /// Constructs an instance of <see cref="ODataResourceMetadataContextWithoutModel"/>.
            /// </summary>
            /// <param name="resource">The resource instance.</param>
            /// <param name="typeContext">The context object to answer basic questions regarding the type of the resource.</param>
            /// <param name="serializationInfo">The serialization info of the resource for writing without model.</param>
            /// <param name="requiresId">True if the resource requires an id, false if it is a non-deleted resource in a delta request payload.</param>
            internal ODataResourceMetadataContextWithoutModel(ODataResourceBase resource, IODataResourceTypeContext typeContext, ODataResourceSerializationInfo serializationInfo, bool requiresId)
                : base(resource, typeContext)
            {
                Debug.Assert(serializationInfo != null, "serializationInfo != null");
                this.serializationInfo = serializationInfo;
                this.requiresId = requiresId;
            }

            /// <summary>
            /// The key property name and value pairs of the resource.
            /// </summary>
            public override ICollection<KeyValuePair<string, object>> KeyProperties
            {
                get
                {
                    if (this.keyProperties == null)
                    {
                        IEdmEntityType entityType = this.ActualResourceType as IEdmEntityType;
                        this.keyProperties = GetKeyProperties(this.resource, this.serializationInfo, entityType, this.requiresId);
                    }

                    return this.keyProperties;
                }
            }

            /// <summary>
            /// The ETag property name and value pairs of the resource.
            /// </summary>
            public override IEnumerable<KeyValuePair<string, object>> ETagProperties
            {
                get { return this.etagProperties ?? (this.etagProperties = GetPropertiesBySerializationInfoPropertyKind(this.resource, ODataPropertyKind.ETag, this.ActualResourceType)); }
            }

            /// <summary>
            /// The actual structured type of the resource, i.e. ODataResource.TypeName.
            /// </summary>
            public override string ActualResourceTypeName
            {
                get
                {
                    if (String.IsNullOrEmpty(this.Resource.TypeName))
                    {
                        throw new ODataException(SRResources.ODataResourceTypeContext_ODataResourceTypeNameMissing);
                    }

                    return this.Resource.TypeName;
                }
            }

            /// <summary>
            /// The selected navigation properties.
            /// </summary>
            public override IEnumerable<IEdmNavigationProperty> SelectedNavigationProperties
            {
                get { return EmptyNavigationProperties; }
            }

            /// <summary>
            /// The selected stream properties.
            /// </summary>
            public override IDictionary<string, IEdmStructuralProperty> SelectedStreamProperties
            {
                get { return EmptyStreamProperties; }
            }

            /// <summary>
            /// The selected bindable operations.
            /// </summary>
            public override IEnumerable<IEdmOperation> SelectedBindableOperations
            {
                get { return EmptyOperations; }
            }
        }

        /// <summary>
        /// Implementation of <see cref="IODataResourceMetadataContext"/> based on the given model.
        /// </summary>
        private sealed class ODataResourceMetadataContextWithModel : ODataResourceMetadataContext
        {
            /// <summary>
            /// The structured type of the resource.
            /// </summary>
            private readonly IEdmStructuredType actualResourceType;

            /// <summary>
            /// The metadata context to use.
            /// </summary>
            private readonly IODataMetadataContext metadataContext;

            /// <summary>
            /// The selected properties.
            /// </summary>
            private readonly SelectedPropertiesNode selectedProperties;

            /// <summary>
            /// The metadata selector
            /// </summary>
            private readonly ODataMetadataSelector metadataSelector;

            /// <summary>
            /// True if the resource requires an id, false if it is a non-deleted resource in a delta request payload.
            /// </summary>
            private bool requiresId;

            /// <summary>
            /// Constructs an instance of <see cref="ODataResourceMetadataContextWithModel"/>.
            /// </summary>
            /// <param name="resource">The resource instance.</param>
            /// <param name="typeContext">The context object to answer basic questions regarding the type of the resource.</param>
            /// <param name="actualResourceType">The structured type of the resource.</param>
            /// <param name="metadataContext">The metadata context to use.</param>
            /// <param name="selectedProperties">The selected properties.</param>
            /// <param name="metadataSelector">The metadata selector to use when writing metadata.</param>
            /// <param name="requiresId">True if the resource requires an id, false if it is a non-deleted resource in a delta request payload.</param>
            internal ODataResourceMetadataContextWithModel(ODataResourceBase resource, IODataResourceTypeContext typeContext, IEdmStructuredType actualResourceType, IODataMetadataContext metadataContext, SelectedPropertiesNode selectedProperties, ODataMetadataSelector metadataSelector, bool requiresId)
                : base(resource, typeContext)
            {
                Debug.Assert(actualResourceType != null, "actualResourceType != null");
                Debug.Assert(metadataContext != null, "metadataContext != null");
                Debug.Assert(selectedProperties != null, "selectedProperties != null");

                this.actualResourceType = actualResourceType;
                this.metadataContext = metadataContext;
                this.selectedProperties = selectedProperties;
                this.metadataSelector = metadataSelector;
                this.requiresId = requiresId;
            }

            /// <summary>
            /// The key property name and value pairs of the resource.
            /// </summary>
            public override ICollection<KeyValuePair<string, object>> KeyProperties
            {
                get
                {
                    if (this.keyProperties == null)
                    {
                        IEdmEntityType entityType = this.actualResourceType as IEdmEntityType;
                        if (entityType != null)
                        {
                            this.keyProperties = GetPropertyValues(entityType.Key(), resource, entityType, this.requiresId);
                        }

                        if (!ValidateEntityTypeHasKeyProperties(this.keyProperties, resource, this.actualResourceType, this.requiresId))
                        {
                            return Enumerable.Empty<KeyValuePair<string, object>>().ToArray();
                        }
                    }

                    return this.keyProperties;
                }
            }

            /// <summary>
            /// The ETag property name and value pairs of the resource.
            /// </summary>
            public override IEnumerable<KeyValuePair<string, object>> ETagProperties
            {
                get
                {
                    if (this.etagProperties == null)
                    {
                        IEnumerable<IEdmStructuralProperty> properties = this.ComputeETagPropertiesFromAnnotation();
                        this.etagProperties = properties.Any()
                            ? GetPropertyValues(properties, resource, this.actualResourceType, isRequired: false)
                            : EmptyProperties;
                    }

                    return this.etagProperties;
                }
            }

            /// <summary>
            /// The actual structured type name of the resource.
            /// </summary>
            public override string ActualResourceTypeName
            {
                // Note that resource.TypeName can be null. When that happens, we use the expected structured type as the actual structured type.
                get { return this.actualResourceType.FullTypeName(); }
            }

            /// <summary>
            /// The selected navigation properties.
            /// </summary>
            public override IEnumerable<IEdmNavigationProperty> SelectedNavigationProperties
            {
                get
                {
                    if (this.selectedNavigationProperties == null)
                    {
                        this.selectedNavigationProperties = this.selectedProperties.GetSelectedNavigationProperties(this.actualResourceType);
                        if (this.metadataSelector != null)
                        {
                            this.selectedNavigationProperties =
                                this.metadataSelector.SelectNavigationProperties(this.actualResourceType, this.selectedNavigationProperties);
                        }
                    }

                    return this.selectedNavigationProperties;
                }
            }

            /// <summary>
            /// The selected stream properties.
            /// </summary>
            public override IDictionary<string, IEdmStructuralProperty> SelectedStreamProperties
            {
                get
                {
                    if (this.selectedStreamProperties == null)
                    {
                        this.selectedStreamProperties = this.selectedProperties.GetSelectedStreamProperties(this.actualResourceType as IEdmEntityType);

                        if (metadataSelector != null)
                        {
                            this.selectedStreamProperties = this.metadataSelector
                                .SelectStreamProperties(this.actualResourceType, this.selectedStreamProperties.Values).ToDictionary(v => v.Name);
                        }
                    }

                    return this.selectedStreamProperties;
                }
            }

            /// <summary>
            /// The selected bindable operations.
            /// </summary>
            public override IEnumerable<IEdmOperation> SelectedBindableOperations
            {
                get
                {
                    if (this.selectedBindableOperations == null)
                    {
                        bool mustBeContainerQualified = this.metadataContext.OperationsBoundToStructuredTypeMustBeContainerQualified(this.actualResourceType);
                        this.selectedBindableOperations = this.metadataContext.GetBindableOperationsForType(this.actualResourceType)
                            .Where(operation => this.selectedProperties.IsOperationSelected(this.actualResourceType, operation, mustBeContainerQualified));
                        if (this.metadataSelector != null)
                        {
                            this.selectedBindableOperations =
                                this.metadataSelector.SelectBindableOperations(this.actualResourceType, this.selectedBindableOperations);
                        }
                    }

                    return this.selectedBindableOperations;
                }
            }

            /// <summary>
            /// Compute ETag from Annotation Org.OData.Core.V1.OptimisticConcurrency on EntitySet
            /// </summary>
            /// <returns>Enumerable of IEdmStructuralProperty</returns>
            private IEnumerable<IEdmStructuralProperty> ComputeETagPropertiesFromAnnotation()
            {
                IEdmModel model = this.metadataContext.Model;
                IEdmEntitySet entitySet = model.FindDeclaredEntitySet(this.typeContext.NavigationSourceName);

                if (entitySet != null)
                {
                    IEdmVocabularyAnnotation annotation = model.FindDeclaredVocabularyAnnotations(entitySet)
                        .SingleOrDefault(t => t.Term.FullName().Equals(CoreVocabularyConstants.OptimisticConcurrency, StringComparison.Ordinal));
                    if (annotation != null)
                    {
                        IEdmExpression collectionExpression = annotation.Value;
                        if (collectionExpression is IEdmCollectionExpression)
                        {
                            IEnumerable<IEdmExpression> pathExpressions = (collectionExpression as IEdmCollectionExpression).Elements.Where(p => p is IEdmPathExpression);
                            foreach (IEdmPathExpression pathExpression in pathExpressions)
                            {
                                // TODO:
                                //  1. Add support for Complex type
                                //  2. Add new exception when collectionExpression is not IEdmCollectionExpression: CoreOptimisticConcurrency must be followed by collection expression
                                IEdmStructuralProperty property = this.actualResourceType.StructuralProperties().FirstOrDefault(p => p.Name == pathExpression.PathSegments.LastOrDefault());
                                if (property == null)
                                {
                                    throw new ODataException(Error.Format(SRResources.EdmValueUtils_PropertyDoesntExist, this.ActualResourceTypeName, pathExpression.PathSegments.LastOrDefault()));
                                }

                                yield return property;
                            }
                        }
                    }
                }
            }
        }
    }
}