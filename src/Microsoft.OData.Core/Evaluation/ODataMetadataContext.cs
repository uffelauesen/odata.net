//---------------------------------------------------------------------
// <copyright file="ODataMetadataContext.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

namespace Microsoft.OData.Evaluation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Microsoft.OData.Core;
    using Microsoft.OData.Edm;
    using Microsoft.OData.Json;
    using Microsoft.OData.Metadata;

    /// <summary>
    /// Interface used for substitutability of the metadata-centric responsibilities of <see cref="ODataJsonDeserializer"/>.
    /// </summary>
    internal interface IODataMetadataContext
    {
        /// <summary>
        /// Gets the Edm Model.
        /// </summary>
        IEdmModel Model { get; }

        /// <summary>
        /// Gets the service base Uri.
        /// </summary>
        Uri ServiceBaseUri { get; }

        /// <summary>
        /// Gets the metadata document uri.
        /// </summary>
        Uri MetadataDocumentUri { get; }

        /// <summary>
        /// Gets the OData uri.
        /// </summary>
        ODataUriSlim? ODataUri { get; }

        /// <summary>
        /// Gets an entity metadata builder for the given resource.
        /// </summary>
        /// <param name="resourceState">Resource state to use as reference for information needed by the builder.</param>
        /// <param name="useKeyAsSegment">true if keys should go in separate segments in auto-generated URIs, false if they should go in parentheses.</param>
        /// <param name="isDelta">true if the payload being read is a delta payload.</param>
        /// <returns>An entity metadata builder.</returns>
        ODataResourceMetadataBuilder GetResourceMetadataBuilderForReader(IODataJsonReaderResourceState resourceState, bool useKeyAsSegment, bool isDelta);

        /// <summary>
        /// Gets the list of operations that are bindable to a type.
        /// </summary>
        /// <param name="bindingType">The binding type in question.</param>
        /// <returns>The list of operations that are always bindable to a type.</returns>
        IEnumerable<IEdmOperation> GetBindableOperationsForType(IEdmType bindingType);

        /// <summary>
        /// Determines whether operations bound to this type must be qualified with the operation they belong to when appearing in a $select clause.
        /// </summary>
        /// <param name="structuredType">The structured type the operations are bound to.</param>
        /// <returns>True if the operations must be container qualified, otherwise false.</returns>
        bool OperationsBoundToStructuredTypeMustBeContainerQualified(IEdmStructuredType structuredType);
    }

    /// <summary>
    /// Default implementation of <see cref="IODataMetadataContext"/>.
    /// </summary>
    internal sealed class ODataMetadataContext : IODataMetadataContext
    {
        /// <summary>
        /// The Edm Model.
        /// </summary>
        private readonly IEdmModel model;

        /// <summary>
        /// EdmTypeResolver instance to resolve entity set base type.
        /// </summary>
        private readonly EdmTypeResolver edmTypeResolver;

        /// <summary>
        /// true if we are reading or writing a response payload, false otherwise.
        /// </summary>
        private readonly bool isResponse;

        /// <summary>
        /// Callback to determine whether operations bound to this type must be qualified with the operation they belong to when appearing in a $select clause.
        /// </summary>
        private readonly Func<IEdmStructuredType, bool> operationsBoundToStructuredTypeMustBeContainerQualified;

        /// <summary>
        /// The metadata document Uri.
        /// </summary>
        private readonly Uri metadataDocumentUri;

        /// <summary>
        /// The OData Uri.
        /// </summary>
        private readonly ODataUriSlim? odataUri;

        /// <summary>
        /// The service base Uri.
        /// </summary>
        private Uri serviceBaseUri;

        /// <summary>
        /// The MetadataLevel.
        /// </summary>
        private JsonMetadataLevel metadataLevel;

        /// <summary>
        /// Constructs an ODataMetadataContext.
        /// </summary>
        /// <param name="isResponse">true if we are writing a response payload, false otherwise.</param>
        /// <param name="model">The Edm model.</param>
        /// <param name="metadataDocumentUri">The metadata document uri.</param>
        /// <param name="odataUri">The request Uri.</param>
        /// <remarks>This overload should only be used by the writer.</remarks>
        public ODataMetadataContext(bool isResponse, IEdmModel model, Uri metadataDocumentUri, in ODataUriSlim? odataUri)
            : this(isResponse, /*OperationsBoundToEntityTypeMustBeContainerQualified*/ null, EdmTypeWriterResolver.Instance, model, metadataDocumentUri, in odataUri)
        {
        }

        /// <summary>
        /// Constructs an ODataMetadataContext.
        /// </summary>
        /// <param name="isResponse">true if we are reading a response payload, false otherwise.</param>
        /// <param name="operationsBoundToStructuredTypeMustBeContainerQualified">Callback to determine whether operations bound to this type must be qualified with the operation they belong to when appearing in a $select clause.</param>
        /// <param name="edmTypeResolver">EdmTypeResolver instance to resolve entity set base type.</param>
        /// <param name="model">The Edm model.</param>
        /// <param name="metadataDocumentUri">The metadata document Uri.</param>
        /// <param name="odataUri">The request Uri.</param>
        /// <remarks>This overload should only be used by the reader.</remarks>
        public ODataMetadataContext(
            bool isResponse,
            Func<IEdmStructuredType, bool> operationsBoundToStructuredTypeMustBeContainerQualified,
            EdmTypeResolver edmTypeResolver,
            IEdmModel model,
            Uri metadataDocumentUri,
            in ODataUriSlim? odataUri)
        {
            Debug.Assert(edmTypeResolver != null, "edmTypeResolver != null");
            Debug.Assert(model != null, "model != null");

            this.isResponse = isResponse;
            this.operationsBoundToStructuredTypeMustBeContainerQualified = operationsBoundToStructuredTypeMustBeContainerQualified ?? EdmLibraryExtensions.OperationsBoundToStructuredTypeMustBeContainerQualified;
            this.edmTypeResolver = edmTypeResolver;
            this.model = model;
            this.metadataDocumentUri = metadataDocumentUri;            
            this.odataUri = odataUri;
        }

        /// <summary>
        /// Constructs an ODataMetadataContext.
        /// </summary>
        /// <param name="isResponse">true if we are reading a response payload, false otherwise.</param>
        /// <param name="operationsBoundToEntityTypeMustBeContainerQualified">Callback to determine whether operations bound to this type must be qualified with the operation they belong to when appearing in a $select clause.</param>
        /// <param name="edmTypeResolver">EdmTypeResolver instance to resolve entity set base type.</param>
        /// <param name="model">The Edm model.</param>
        /// <param name="metadataDocumentUri">The metadata document Uri.</param>
        /// <param name="odataUri">The request Uri.</param>
        /// <param name="metadataLevel">Current Json MetadataLevel</param>
        /// <remarks>This overload should only be used by the reader.</remarks>
        public ODataMetadataContext(
            bool isResponse,
            Func<IEdmStructuredType, bool> operationsBoundToEntityTypeMustBeContainerQualified,
            EdmTypeResolver edmTypeResolver,
            IEdmModel model,
            Uri metadataDocumentUri,
            in ODataUriSlim? odataUri,
            JsonMetadataLevel metadataLevel)
            : this(isResponse, operationsBoundToEntityTypeMustBeContainerQualified, edmTypeResolver, model, metadataDocumentUri, in odataUri)
        {
            Debug.Assert(metadataLevel != null, "MetadataLevel != null");

            this.metadataLevel = metadataLevel;
        }

        /// <summary>
        /// Gets the Edm Model.
        /// </summary>
        public IEdmModel Model
        {
            get
            {
                Debug.Assert(this.model != null, "this.model != null");
                return this.model;
            }
        }

        /// <summary>
        /// Gets the service base Uri.
        /// </summary>
        public Uri ServiceBaseUri
        {
            get { return this.serviceBaseUri ?? (this.serviceBaseUri = new Uri(this.MetadataDocumentUri, "./")); }
        }

        /// <summary>
        /// Gets the metadata document uri.
        /// </summary>
        public Uri MetadataDocumentUri
        {
            get
            {
                if (this.metadataDocumentUri == null)
                {
                    throw new ODataException(Error.Format(SRResources.ODataJsonResourceMetadataContext_MetadataAnnotationMustBeInPayload, ODataAnnotationNames.ODataContext));
                }

                Debug.Assert(this.metadataDocumentUri.IsAbsoluteUri, "this.metadataDocumentUri.IsAbsoluteUri");
                return this.metadataDocumentUri;
            }
        }

        /// <summary>
        /// Gets the OData uri.
        /// </summary>
        public ODataUriSlim? ODataUri
        {
            get
            {
                return this.odataUri;
            }
        }

        /// <summary>
        /// Gets a resource metadata builder for the given resource.
        /// </summary>
        /// <param name="resourceState">Resource state to use as reference for information needed by the builder.</param>
        /// <param name="useKeyAsSegment">true if keys should go in separate segments in auto-generated URIs, false if they should go in parentheses.</param>
        /// <param name="isDelta">true if the payload being read is a delta payload</param>
        /// <returns>A resource metadata builder.</returns>
        public ODataResourceMetadataBuilder GetResourceMetadataBuilderForReader(IODataJsonReaderResourceState resourceState, bool useKeyAsSegment, bool isDelta = false)
        {
            Debug.Assert(resourceState != null, "resource != null");

            // Only apply the conventional template builder on response. On a request we would only report what's on the wire.
            if (resourceState.MetadataBuilder == null)
            {
                ODataResourceBase resource = resourceState.Resource;
                if (this.isResponse || (isDelta && this.metadataDocumentUri != null))
                {
                    ODataTypeAnnotation typeAnnotation = resource.TypeAnnotation;

                    IEdmStructuredType structuredType = null;
                    if (typeAnnotation != null)
                    {
                        if (typeAnnotation.Type != null)
                        {
                            // First try ODataTypeAnnotation.Type (for perf improvement)
                            structuredType = typeAnnotation.Type as IEdmStructuredType;
                        }
                        else if (typeAnnotation.TypeName != null)
                        {
                            // Then try ODataTypeAnnotation.TypeName
                            structuredType = this.model.FindType(typeAnnotation.TypeName) as IEdmStructuredType;
                        }
                    }

                    if (structuredType == null)
                    {
                        // No type name read from the payload. Use resource type from model.
                        structuredType = resourceState.ResourceType;
                    }

                    IEdmNavigationSource navigationSource = resourceState.NavigationSource;
                    IEdmEntityType navigationSourceElementType = this.edmTypeResolver.GetElementType(navigationSource);
                    IODataResourceTypeContext typeContext =
                        ODataResourceTypeContext.Create( /*serializationInfo*/
                            null, navigationSource, navigationSourceElementType, resourceState.ResourceTypeFromMetadata ?? resourceState.ResourceType);

                    IODataResourceMetadataContext resourceMetadataContext = ODataResourceMetadataContext.Create(
                        resource,
                        typeContext,
                        /*serializationInfo*/null,
                        structuredType,
                        this,
                        resourceState.SelectedProperties,
                        null,
                        /*requiresId*/ (this.isResponse || !isDelta || resource is ODataDeletedResource) // id is required except for non-deleted resource in delta request
                        );

                    ODataConventionalUriBuilder uriBuilder = new ODataConventionalUriBuilder(this.ServiceBaseUri,
                        useKeyAsSegment ? ODataUrlKeyDelimiter.Slash : ODataUrlKeyDelimiter.Parentheses);

                    if (structuredType.IsODataEntityTypeKind())
                    {
                        resourceState.MetadataBuilder = isDelta ?
                            new ODataConventionalIdMetadataBuilder(resourceMetadataContext, this, uriBuilder) :
                            new ODataConventionalEntityMetadataBuilder(resourceMetadataContext, this, uriBuilder);
                    }
                    else
                    {
                        resourceState.MetadataBuilder = isDelta ?
                            resourceState.MetadataBuilder = new NoOpResourceMetadataBuilder(resource) :
                            new ODataConventionalResourceMetadataBuilder(resourceMetadataContext, this, uriBuilder);
                    }
                }
                else
                {
                    resourceState.MetadataBuilder = new NoOpResourceMetadataBuilder(resource);
                }
            }

            return resourceState.MetadataBuilder;
        }

        /// <summary>
        /// Gets the list of operations that are always bindable to a type.
        /// </summary>
        /// <param name="bindingType">The binding type in question.</param>
        /// <returns>The list of operations that are always bindable to a type.</returns>
        public IEnumerable<IEdmOperation> GetBindableOperationsForType(IEdmType bindingType)
        {
            Debug.Assert(bindingType != null, "bindingType != null");            
            Debug.Assert(this.isResponse, "this.readingResponse");

            return MetadataUtils.CalculateBindableOperationsForType(bindingType, this.model, this.edmTypeResolver);
        }

        /// <summary>
        /// Determines whether operations bound to this type must be qualified with the operation they belong to when appearing in a $select clause.
        /// </summary>
        /// <param name="structuredType">The structured type the operations are bound to.</param>
        /// <returns>True if the operations must be container qualified, otherwise false.</returns>
        public bool OperationsBoundToStructuredTypeMustBeContainerQualified(IEdmStructuredType structuredType)
        {
            Debug.Assert(structuredType != null, "entityType != null");
            Debug.Assert(this.isResponse, "this.isResponse");
            Debug.Assert(this.operationsBoundToStructuredTypeMustBeContainerQualified != null, "this.operationsBoundToStructuredTypeMustBeContainerQualified != null");

            return this.operationsBoundToStructuredTypeMustBeContainerQualified(structuredType);
        }
    }
}
