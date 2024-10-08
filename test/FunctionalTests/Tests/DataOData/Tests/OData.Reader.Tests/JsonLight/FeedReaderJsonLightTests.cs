﻿//---------------------------------------------------------------------
// <copyright file="FeedReaderJsonTests.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

namespace Microsoft.Test.Taupo.OData.Reader.Tests.Json
{
    #region Namespaces
    using System.Collections.Generic;
    using Microsoft.OData.Edm;
    using Microsoft.Test.Taupo.Common;
    using Microsoft.Test.Taupo.Contracts.EntityModel;
    using Microsoft.Test.Taupo.Contracts.EntityModel.Edm;
    using Microsoft.Test.Taupo.Contracts.Types;
    using Microsoft.Test.Taupo.Execution;
    using Microsoft.Test.Taupo.OData.Common;
    using Microsoft.Test.Taupo.OData.Common.Annotations;
    using Microsoft.Test.Taupo.OData.Contracts.Json;
    using Microsoft.Test.Taupo.OData.Contracts.Json;
    using Microsoft.Test.Taupo.OData.Reader.Tests;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Test.Taupo.OData.Json;
    using Microsoft.Test.Taupo.Astoria.Contracts.OData;
    #endregion Namespaces

    /// <summary>
    /// Tests reading of feeds values in JSON Light.
    /// </summary>
    [TestClass, TestCase]
    public class FeedReaderJsonTests : ODataReaderTestCase
    {
        [InjectDependency]
        public PayloadReaderTestDescriptor.Settings Settings { get; set; }

        [TestMethod, TestCategory("Reader.Json"), Variation(Description = "Verifies correct reading of feeds in Json Light.")]
        public void FeedReaderTest()
        {
            IEdmModel model = Test.OData.Utils.Metadata.TestModels.BuildTestModel();
            IEdmEntityContainer container = model.FindEntityContainer("DefaultContainer");
            IEdmEntitySet citiesEntitySet = container.FindEntitySet("Cities");
            IEdmType cityType = model.FindType("TestModel.CityType") as IEdmType;

            IEnumerable<PayloadReaderTestDescriptor> testDescriptors = new[]
            {
                #region Test cases
                new PayloadReaderTestDescriptor(this.Settings)
                {
                    DebugDescription = "Empty feed without next link or inline count.",
                    PayloadElement = PayloadBuilder.EntitySet()
                        .JsonRepresentation("{" +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataContextAnnotationName + "\":\"http://odata.org/test/$metadata#TestModel.DefaultContainer.Cities\"," +
                            "\"" + JsonConstants.ODataValuePropertyName + "\":[]" +
                            "}")
                        .ExpectedEntityType(cityType, citiesEntitySet),
                    PayloadEdmModel = model
                },
                new PayloadReaderTestDescriptor(this.Settings)
                {
                    DebugDescription = "Empty feed with a next link annotation before the feed property.",
                    PayloadElement = PayloadBuilder.EntitySet("http://odata.org/test/nextlink")
                        .JsonRepresentation("{" +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataContextAnnotationName + "\":\"http://odata.org/test/$metadata#TestModel.DefaultContainer.Cities\"," +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataNextLinkAnnotationName + "\":\"http://odata.org/test/nextlink\"," +
                            "\"" + JsonConstants.ODataValuePropertyName + "\":[]" +
                            "}")
                        .ExpectedEntityType(cityType, citiesEntitySet),
                    PayloadEdmModel = model
                },
                new PayloadReaderTestDescriptor(this.Settings)
                {
                    DebugDescription = "Empty feed with a next link annotation after the feed property.",
                    PayloadElement = PayloadBuilder.EntitySet("http://odata.org/test/nextlink")
                        .JsonRepresentation("{" +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataContextAnnotationName + "\":\"http://odata.org/test/$metadata#TestModel.DefaultContainer.Cities\"," +
                            "\"" + JsonConstants.ODataValuePropertyName + "\":[]," +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataNextLinkAnnotationName + "\":\"http://odata.org/test/nextlink\"" +
                            "}")
                        .ExpectedEntityType(cityType, citiesEntitySet),
                    PayloadEdmModel = model
                },
                new PayloadReaderTestDescriptor(this.Settings)
                {
                    DebugDescription = "Empty feed with a inline count annotation before the feed property.",
                    PayloadElement = PayloadBuilder.EntitySet(null, 42)
                        .JsonRepresentation("{" +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataContextAnnotationName + "\":\"http://odata.org/test/$metadata#TestModel.DefaultContainer.Cities\"," +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataCountAnnotationName + "\":42," +
                            "\"" + JsonConstants.ODataValuePropertyName + "\":[]" +
                            "}")
                        .ExpectedEntityType(cityType, citiesEntitySet),
                    PayloadEdmModel = model
                },
                new PayloadReaderTestDescriptor(this.Settings)
                {
                    DebugDescription = "Empty feed with a inline count annotation after the feed property.",
                    PayloadElement = PayloadBuilder.EntitySet(null, 42)
                        .JsonRepresentation("{" +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataContextAnnotationName + "\":\"http://odata.org/test/$metadata#TestModel.DefaultContainer.Cities\"," +
                            "\"" + JsonConstants.ODataValuePropertyName + "\":[]," +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataCountAnnotationName + "\":42" +
                            "}")
                        .ExpectedEntityType(cityType, citiesEntitySet),
                    PayloadEdmModel = model
                },
                new PayloadReaderTestDescriptor(this.Settings)
                {
                    DebugDescription = "Empty feed with next link and inline count annotation before the feed property.",
                    PayloadElement = PayloadBuilder.EntitySet("http://odata.org/test/nextlink", 42)
                        .JsonRepresentation("{" +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataContextAnnotationName + "\":\"http://odata.org/test/$metadata#TestModel.DefaultContainer.Cities\"," +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataNextLinkAnnotationName + "\":\"http://odata.org/test/nextlink\"," +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataCountAnnotationName + "\":42," +
                            "\"" + JsonConstants.ODataValuePropertyName + "\":[]" +
                            "}")
                        .ExpectedEntityType(cityType, citiesEntitySet),
                    PayloadEdmModel = model
                },
                new PayloadReaderTestDescriptor(this.Settings)
                {
                    DebugDescription = "Empty feed with next link and inline count annotation after the feed property.",
                    PayloadElement = PayloadBuilder.EntitySet("http://odata.org/test/nextlink", 42)
                        .JsonRepresentation("{" +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataContextAnnotationName + "\":\"http://odata.org/test/$metadata#TestModel.DefaultContainer.Cities\"," +
                            "\"" + JsonConstants.ODataValuePropertyName + "\":[]," +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataCountAnnotationName + "\":42," +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataNextLinkAnnotationName + "\":\"http://odata.org/test/nextlink\"" +
                            "}")
                        .ExpectedEntityType(cityType, citiesEntitySet),
                    PayloadEdmModel = model
                },
                new PayloadReaderTestDescriptor(this.Settings)
                {
                    DebugDescription = "Empty feed with next link before and inline count after the feed property.",
                    PayloadElement = PayloadBuilder.EntitySet("http://odata.org/test/nextlink", 42)
                        .JsonRepresentation("{" +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataContextAnnotationName + "\":\"http://odata.org/test/$metadata#TestModel.DefaultContainer.Cities\"," +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataNextLinkAnnotationName + "\":\"http://odata.org/test/nextlink\"," +
                            "\"" + JsonConstants.ODataValuePropertyName + "\":[]," +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataCountAnnotationName + "\":42" +
                            "}")
                        .ExpectedEntityType(cityType, citiesEntitySet),
                    PayloadEdmModel = model
                },
                new PayloadReaderTestDescriptor(this.Settings)
                {
                    DebugDescription = "Empty feed with next link after and inline count before the feed property.",
                    PayloadElement = PayloadBuilder.EntitySet("http://odata.org/test/nextlink", 42)
                        .JsonRepresentation("{" +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataContextAnnotationName + "\":\"http://odata.org/test/$metadata#TestModel.DefaultContainer.Cities\"," +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataCountAnnotationName + "\":42," +
                            "\"" + JsonConstants.ODataValuePropertyName + "\":[]," +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataNextLinkAnnotationName + "\":\"http://odata.org/test/nextlink\"" +
                            "}")
                        .ExpectedEntityType(cityType, citiesEntitySet),
                    PayloadEdmModel = model
                },
                new PayloadReaderTestDescriptor(this.Settings)
                {
                    DebugDescription = "Empty feed with next link after and inline count before the feed property and custom annotations.",
                    PayloadElement = PayloadBuilder.EntitySet("http://odata.org/test/nextlink", 42)
                        .JsonRepresentation("{" +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataContextAnnotationName + "\":\"http://odata.org/test/$metadata#TestModel.DefaultContainer.Cities\"," +
                            "\"@my.annotation\":\"annotation-value\"," +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataNextLinkAnnotationName + "\":\"http://odata.org/test/nextlink\"," +
                            "\"@my.annotation2\":\"annotation-value2\"," +
                            "\"" + JsonConstants.ODataValuePropertyName + "\":[]," +
                            "\"@my.annotation3\":\"annotation-value3\"," +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataCountAnnotationName + "\":42," +
                            "\"@my.annotation4\":\"annotation-value4\"" +
                            "}")
                        .ExpectedEntityType(cityType, citiesEntitySet),
                    PayloadEdmModel = model
                },
                new PayloadReaderTestDescriptor(this.Settings)
                {
                    DebugDescription = "Feed with primitive item in feed property value.",
                    PayloadElement = PayloadBuilder.EntitySet()
                        .JsonRepresentation("{" +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataContextAnnotationName + "\":\"http://odata.org/test/$metadata#TestModel.DefaultContainer.Cities\"," +
                            "\"" + JsonConstants.ODataValuePropertyName + "\":[ 1 ]" +
                            "}")
                        .ExpectedEntityType(cityType, citiesEntitySet),               
                    PayloadEdmModel = model,
                    ExpectedException = ODataExpectedExceptions.ODataException("ODataJsonReader_CannotReadResourcesOfResourceSet","PrimitiveValue"),
                },
                new PayloadReaderTestDescriptor(this.Settings)
                {
                    DebugDescription = "Feed with array item in feed property value.",
                    PayloadElement = PayloadBuilder.EntitySet()
                        .JsonRepresentation("{" +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataContextAnnotationName + "\":\"http://odata.org/test/$metadata#Edm.Untyped\"," +
                            "\"" + JsonConstants.ODataValuePropertyName + "\":[ [ 1 ] ]" +
                            "}")
                        .ExpectedEntityType(EdmCoreModel.Instance.GetUntyped()),
                    ExpectedResultPayloadElement = (ReaderTestConfiguration t) => { return PayloadBuilder.EntitySet().Append(new ODataPayloadElement[] {PayloadBuilder.EntitySet().Append(new PrimitiveValue("Edm.Int32",1)) }); },
                    PayloadEdmModel = model,
                },
                #endregion Test cases
            };

            // TODO: all of the above for feeds in expanded navigation links

            // TODO: non-empty feed with entries of base type, of base + derived type, only derived type (with and without derived type specified as expected type)
            // TODO: non-empty feed at top-level and expanded navigation link

            this.CombinatorialEngineProvider.RunCombinations(
                testDescriptors,
                this.ReaderTestConfigurationProvider.JsonFormatConfigurations,
                (testDescriptor, testConfiguration) =>
                {
                    // These descriptors are already tailored specifically for Json Light and 
                    // do not require normalization.
                    testDescriptor.TestDescriptorNormalizers.Clear();
                    testDescriptor.RunTest(testConfiguration);
                });
        }

        [TestMethod, TestCategory("Reader.Json"), Variation(Description = "Verifies correct error behavior for invalid feeds in Json Light.")]
        public void FeedReaderErrorTest()
        {
            IEdmModel model = Test.OData.Utils.Metadata.TestModels.BuildTestModel();
            IEdmEntityContainer container = model.FindEntityContainer("DefaultContainer");
            IEdmEntitySet citiesEntitySet = container.FindEntitySet("Cities");
            IEdmType cityType = model.FindType("TestModel.CityType") as IEdmType;

            IEnumerable<PayloadReaderTestDescriptor> testDescriptors = new[]
            {
                #region Test cases
                new PayloadReaderTestDescriptor(this.Settings)
                {
                    DebugDescription = "Empty feed with OData property annotation before top-level feed property.",
                    PayloadElement = PayloadBuilder.EntitySet()
                        .JsonRepresentation("{" +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataContextAnnotationName + "\":\"http://odata.org/test/$metadata#TestModel.DefaultContainer.Cities\"," +
                            "\"" + JsonUtils.GetPropertyAnnotationName(JsonConstants.ODataValuePropertyName, JsonConstants.ODataCountAnnotationName) + "\":\"42\"," +
                            "\"" + JsonConstants.ODataValuePropertyName + "\":[]" +
                            "}")
                        .ExpectedEntityType(cityType, citiesEntitySet),
                    PayloadEdmModel = model,
                    ExpectedException = ODataExpectedExceptions.ODataException("ODataJsonPropertyAndValueDeserializer_UnexpectedAnnotationProperties", JsonConstants.ODataCountAnnotationName)
                },
                new PayloadReaderTestDescriptor(this.Settings)
                {
                    DebugDescription = "Empty feed with OData property annotation after top-level feed property.",
                    PayloadElement = PayloadBuilder.EntitySet()
                        .JsonRepresentation("{" +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataContextAnnotationName + "\":\"http://odata.org/test/$metadata#TestModel.DefaultContainer.Cities\"," +
                            "\"" + JsonConstants.ODataValuePropertyName + "\":[]," +
                            "\"" + JsonUtils.GetPropertyAnnotationName(JsonConstants.ODataValuePropertyName, JsonConstants.ODataCountAnnotationName) + "\":\"42\"" +
                            "}")
                        .ExpectedEntityType(cityType, citiesEntitySet),
                    PayloadEdmModel = model,
                    ExpectedException = ODataExpectedExceptions.ODataException("ODataJsonPropertyAndValueDeserializer_UnexpectedAnnotationProperties", JsonConstants.ODataCountAnnotationName)
                },
                new PayloadReaderTestDescriptor(this.Settings)
                {
                    DebugDescription = "Empty feed with invalid feed property value.",
                    PayloadElement = PayloadBuilder.EntitySet()
                        .JsonRepresentation("{" +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataContextAnnotationName + "\":\"http://odata.org/test/$metadata#TestModel.DefaultContainer.Cities\"," +
                            "\"" + JsonConstants.ODataValuePropertyName + "\":{}" +
                            "}")
                        .ExpectedEntityType(cityType, citiesEntitySet),
                    PayloadEdmModel = model,
                    ExpectedException = ODataExpectedExceptions.ODataException("ODataJsonResourceDeserializer_CannotReadResourceSetContentStart", "StartObject")
                },
                new PayloadReaderTestDescriptor(this.Settings)
                {
                    DebugDescription = "Feed with invalid property before the feed property.",
                    PayloadElement = PayloadBuilder.EntitySet()
                        .JsonRepresentation("{" +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataContextAnnotationName + "\":\"http://odata.org/test/$metadata#TestModel.DefaultContainer.Cities\"," +
                            "\"other\":\"42\"," +
                            "\"" + JsonConstants.ODataValuePropertyName + "\":[]" +
                            "}")
                        .ExpectedEntityType(cityType, citiesEntitySet),
                    PayloadEdmModel = model,
                    ExpectedException = ODataExpectedExceptions.ODataException("ODataJsonResourceDeserializer_InvalidPropertyInTopLevelResourceSet", "other", JsonConstants.ODataValuePropertyName)
                },
                new PayloadReaderTestDescriptor(this.Settings)
                {
                    DebugDescription = "Feed with invalid property annotation before the feed property.",
                    PayloadElement = PayloadBuilder.EntitySet()
                        .JsonRepresentation("{" +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataContextAnnotationName + "\":\"http://odata.org/test/$metadata#TestModel.DefaultContainer.Cities\"," +
                            "\"" + JsonUtils.GetPropertyAnnotationName("other", JsonConstants.ODataCountAnnotationName) + "\":\"42\"," +
                            "\"" + JsonConstants.ODataValuePropertyName + "\":[]" +
                            "}")
                        .ExpectedEntityType(cityType, citiesEntitySet),
                    PayloadEdmModel = model,
                    ExpectedException = ODataExpectedExceptions.ODataException("ODataJsonPropertyAndValueDeserializer_UnexpectedAnnotationProperties", JsonConstants.ODataCountAnnotationName)
                },
                new PayloadReaderTestDescriptor(this.Settings)
                {
                    DebugDescription = "Feed with invalid property after the feed property.",
                    PayloadElement = PayloadBuilder.EntitySet()
                        .JsonRepresentation("{" +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataContextAnnotationName + "\":\"http://odata.org/test/$metadata#TestModel.DefaultContainer.Cities\"," +
                            "\"" + JsonConstants.ODataValuePropertyName + "\":[]," +
                            "\"other\":\"42\"" +
                            "}")
                        .ExpectedEntityType(cityType, citiesEntitySet),
                    PayloadEdmModel = model,
                    ExpectedException = ODataExpectedExceptions.ODataException("ODataJsonResourceDeserializer_InvalidPropertyInTopLevelResourceSet", "other", JsonConstants.ODataValuePropertyName)
                },
                new PayloadReaderTestDescriptor(this.Settings)
                {
                    DebugDescription = "Feed with invalid property annotation after the feed property.",
                    PayloadElement = PayloadBuilder.EntitySet()
                        .JsonRepresentation("{" +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataContextAnnotationName + "\":\"http://odata.org/test/$metadata#TestModel.DefaultContainer.Cities\"," +
                            "\"" + JsonConstants.ODataValuePropertyName + "\":[]," +
                            "\"" + JsonUtils.GetPropertyAnnotationName("other", JsonConstants.ODataCountAnnotationName) + "\":\"42\"" +
                            "}")
                        .ExpectedEntityType(cityType, citiesEntitySet),
                    PayloadEdmModel = model,
                    ExpectedException = ODataExpectedExceptions.ODataException("ODataJsonPropertyAndValueDeserializer_UnexpectedAnnotationProperties", JsonConstants.ODataCountAnnotationName)
                },
                new PayloadReaderTestDescriptor(this.Settings)
                {
                    DebugDescription = "Feed with missing feed property.",
                    PayloadElement = PayloadBuilder.EntitySet()
                        .JsonRepresentation("{" +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataContextAnnotationName + "\":\"http://odata.org/test/$metadata#TestModel.DefaultContainer.Cities\"," +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataCountAnnotationName + "\":42" +
                            "}")
                        .ExpectedEntityType(cityType, citiesEntitySet),
                    PayloadEdmModel = model,
                    ExpectedException = ODataExpectedExceptions.ODataException("ODataJsonResourceDeserializer_ExpectedResourceSetPropertyNotFound", "value")
                },
                new PayloadReaderTestDescriptor(this.Settings)
                {
                    DebugDescription = "Empty feed with odata.count annotation with null value before top-level feed property.",
                    PayloadElement = PayloadBuilder.EntitySet()
                        .JsonRepresentation("{" +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataContextAnnotationName + "\":\"http://odata.org/test/$metadata#TestModel.DefaultContainer.Cities\"," +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataCountAnnotationName + "\":null," +
                            "\"" + JsonConstants.ODataValuePropertyName + "\":[]" +
                            "}")
                        .ExpectedEntityType(cityType, citiesEntitySet),
                    PayloadEdmModel = model,
                    ExpectedException = ODataExpectedExceptions.ODataException("ODataJsonReaderUtils_AnnotationWithNullValue", JsonConstants.ODataCountAnnotationName)
                },
                new PayloadReaderTestDescriptor(this.Settings)
                {
                    DebugDescription = "Empty feed with odata.nextLink annotation with null value before top-level feed property.",
                    PayloadElement = PayloadBuilder.EntitySet()
                        .JsonRepresentation("{" +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataContextAnnotationName + "\":\"http://odata.org/test/$metadata#TestModel.DefaultContainer.Cities\"," +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataNextLinkAnnotationName + "\":null," +
                            "\"" + JsonConstants.ODataValuePropertyName + "\":[]" +
                            "}")
                        .ExpectedEntityType(cityType, citiesEntitySet),
                    PayloadEdmModel = model,
                    ExpectedException = ODataExpectedExceptions.ODataException("ODataJsonReaderUtils_AnnotationWithNullValue", JsonConstants.ODataNextLinkAnnotationName)
                },
                new PayloadReaderTestDescriptor(this.Settings)
                {
                    DebugDescription = "Empty feed with odata.nextLink annotation with null value after top-level feed property.",
                    PayloadElement = PayloadBuilder.EntitySet()
                        .JsonRepresentation("{" +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataContextAnnotationName + "\":\"http://odata.org/test/$metadata#TestModel.DefaultContainer.Cities\"," +
                            "\"" + JsonConstants.ODataValuePropertyName + "\":[]," +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataNextLinkAnnotationName + "\":null" +
                            "}")
                        .ExpectedEntityType(cityType, citiesEntitySet),
                    PayloadEdmModel = model,
                    ExpectedException = ODataExpectedExceptions.ODataException("ODataJsonReaderUtils_AnnotationWithNullValue", JsonConstants.ODataNextLinkAnnotationName)
                },
                new PayloadReaderTestDescriptor(this.Settings)
                {
                    DebugDescription = "Empty feed with duplicate feed properties.",
                    PayloadElement = PayloadBuilder.EntitySet()
                        .JsonRepresentation("{" +
                            "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataContextAnnotationName + "\":\"http://odata.org/test/$metadata#TestModel.DefaultContainer.Cities\"," +
                            "\"" + JsonConstants.ODataValuePropertyName + "\":[]," +
                            "\"" + JsonConstants.ODataValuePropertyName + "\":[]" +
                            "}")
                        .ExpectedEntityType(cityType, citiesEntitySet),
                    PayloadEdmModel = model,
                    ExpectedException = ODataExpectedExceptions.ODataException("DuplicatePropertyNamesNotAllowed", JsonConstants.ODataValuePropertyName)
                },
                #endregion Test cases
            };

            // TODO: all of the above for feeds in expanded navigation links

            this.CombinatorialEngineProvider.RunCombinations(
                testDescriptors,
                this.ReaderTestConfigurationProvider.JsonFormatConfigurations,
                (testDescriptor, testConfiguration) =>
                {
                    // These descriptors are already tailored specifically for Json Light and 
                    // do not require normalization.
                    testDescriptor.TestDescriptorNormalizers.Clear();
                    testDescriptor.RunTest(testConfiguration);
                });
        }

        [TestMethod, TestCategory("Reader.Json"), Variation(Description = "Verifies correct reading of feed with sub context uri in json.")]
        public void FeedReaderWithSubContextUriTest()
        {
            IEdmModel model = Test.OData.Utils.Metadata.TestModels.BuildTestModel();
            IEdmEntityContainer container = model.FindEntityContainer("DefaultContainer");
            IEdmEntitySet citiesEntitySet = container.FindEntitySet("Cities");
            IEdmType cityType = model.FindType("TestModel.CityType") as IEdmType;
            IEdmEntitySet personsEntitySet = container.FindEntitySet("Persons");
            IEdmType personType = model.FindType("TestModel.Person") as IEdmType;

            IEnumerable<PayloadReaderTestDescriptor> testDescriptors = new[]
            {
                #region Test cases
                new PayloadReaderTestDescriptor(this.Settings)
                {
                    DebugDescription = "Feed containing entries with and without sub context uri",
                    PayloadElement = PayloadBuilder
                        .EntitySet( new EntityInstance[]
                            {
                                PayloadBuilder.Entity("TestModel.CityType").PrimitiveProperty("Id", 1).AddAnnotation(new SerializationTypeNameTestAnnotation() { TypeName = null }),
                                PayloadBuilder.Entity("TestModel.CityType").PrimitiveProperty("Id", 2).AddAnnotation(new SerializationTypeNameTestAnnotation() { TypeName = null }),
                            })
                        .JsonRepresentation(
                            "{" +
                                "\"@odata.context\":\"http://odata.org/test/$metadata#Cities(Id)\"," +
                                "\"value\":[" +
                                    "{" +
                                        "\"@odata.context\":\"http://odata.org/test/$metadata#Cities/$entity\"," +
                                        "\"Id\":1" +
                                    "}," +
                                    "{" +
                                        "\"Id\":2" +
                                    "}," +
                                "]" +
                            "}")
                        .ExpectedEntityType(cityType, citiesEntitySet),
                    PayloadEdmModel = model
                },
                new PayloadReaderTestDescriptor(this.Settings)
                {
                    DebugDescription = "Feed containing entries with expanded navigation properties with sub context uri",
                    PayloadElement = PayloadBuilder
                        .EntitySet( new EntityInstance[]{
                            PayloadBuilder
                                .Entity("TestModel.CityType")
                                .PrimitiveProperty("Id", 1)
                                .ExpandedNavigationProperty(
                                    "CityHall", 
                                    PayloadBuilder.EntitySet(new EntityInstance[]
                                    {
                                        PayloadBuilder.Entity("TestModel.OfficeType").PrimitiveProperty("Id", 1).AddAnnotation(new SerializationTypeNameTestAnnotation() { TypeName = null }),
                                    }))
                                .AddAnnotation(new SerializationTypeNameTestAnnotation() { TypeName = null })
                            })
                        .JsonRepresentation(
                            "{" +
                                "\"@odata.context\":\"http://odata.org/test/$metadata#Cities(Id)\"," +
                                "\"value\":[" +
                                    "{" +
                                        "\"@odata.context\":\"http://odata.org/test/$metadata#Cities(Id,CityHall(Id))/$entity\"," +
                                        "\"Id\":1," +
                                        "\"CityHall\":[" +
                                            "{" +
                                                "\"@odata.context\":\"http://odata.org/test/$metadata#Offices/$entity\"," +
                                                "\"Id\":1" +
                                            "}" +
                                        "]" +
                                    "}" +
                                "]" +
                            "}")
                        .ExpectedEntityType(cityType, citiesEntitySet),
                    PayloadEdmModel = model
                },
                new PayloadReaderTestDescriptor(this.Settings)
                {
                    DebugDescription = "Feed containg entries with mismatch sub context uri",
                    PayloadElement = PayloadBuilder
                        .EntitySet( new EntityInstance[]
                            {
                                PayloadBuilder.Entity("TestModel.CityType").PrimitiveProperty("Id", 1).AddAnnotation(new SerializationTypeNameTestAnnotation() { TypeName = null }),
                            })
                        .JsonRepresentation(
                            "{" +
                                "\"@odata.context\":\"http://odata.org/test/$metadata#Cities(Id)\"," +
                                "\"value\":[" +
                                    "{" +
                                        "\"@odata.context\":\"http://odata.org/test/$metadata#Offices/$entity\"," +
                                        "\"Id\":1" +
                                    "}," +
                                "]" +
                            "}")
                        .ExpectedEntityType(cityType, citiesEntitySet),
                    PayloadEdmModel = model,
                    ExpectedException = ODataExpectedExceptions.ODataException(
                        "ReaderValidationUtils_ContextUriValidationInvalidExpectedEntitySet",
                        "http://odata.org/test/$metadata#Offices/$entity",
                        "Offices",
                        "Cities")
                },
                /* TODO: Enable this case after updating SelectedPropertiesNode class to adapt to V4
                new PayloadReaderTestDescriptor(this.Settings)
                {
                    DebugDescription = "Empty feed without next link or inline count.",
                    PayloadElement = PayloadBuilder.EntitySet( new EntityInstance[]
                    {
                        PayloadBuilder.Entity("TestModel.CityType")
                            .PrimitiveProperty("Id", 1)
                            .NavigationProperty(new NavigationPropertyInstance("PoliceStation",null))
                            .AddAnnotation(new SerializationTypeNameTestAnnotation() { TypeName = null }),
                        PayloadBuilder.Entity("TestModel.CityType").PrimitiveProperty("Id", 2).AddAnnotation(new SerializationTypeNameTestAnnotation() { TypeName = null }),
                    })
                        .JsonRepresentation(
                            "{" +
                                "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataContextAnnotationName + "\":\"http://odata.org/test/$metadata#Collection(TestModel.CityType)\"," +
                                "\"" + JsonConstants.ODataValuePropertyName + "\":[" +
                                    "{" +
                                        "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataContextAnnotationName + "\":\"http://odata.org/test/$metadata#TestModel.DefaultContainer.Cities/$entity\"," +
                                        "\"Id\":1" +
                                    "}," +
                                    "{" +
                                        "\"" + JsonConstants.ODataPropertyAnnotationSeparator + JsonConstants.ODataContextAnnotationName + "\":\"http://odata.org/test/$metadata#TestModel.DefaultContainer.Cities/$entity\"," +
                                        "\"Id\":2" +                                        
                                    "}," +
                                "]" +
                            "}")
                        .ExpectedEntityType(cityType, citiesEntitySet),
                    PayloadEdmModel = model
                },
                 */
                new PayloadReaderTestDescriptor(this.Settings)
                {
                    DebugDescription = "Empty feed without next link or inline count.",
                    PayloadElement = PayloadBuilder.EntitySet( new EntityInstance[]
                    {
                        PayloadBuilder.Entity("TestModel.Person")
                            .PrimitiveProperty("Id", 1)
                            .ExpandedNavigationProperty(
                                    "Friend", 
                                    PayloadBuilder.EntitySet(new EntityInstance[]{}))
                            .AddAnnotation(new SerializationTypeNameTestAnnotation() { TypeName = null }),
                        PayloadBuilder.Entity("TestModel.Person")
                            .PrimitiveProperty("Id", 2)
                            .ExpandedNavigationProperty(
                                    "Friend", 
                                    PayloadBuilder.EntitySet(new EntityInstance[]{}))
                            .AddAnnotation(new SerializationTypeNameTestAnnotation() { TypeName = null }),
                        PayloadBuilder.Entity("TestModel.Person")
                            .PrimitiveProperty("Id", 3)
                            .ExpandedNavigationProperty(
                                    "Friend", 
                                    PayloadBuilder.EntitySet(new EntityInstance[]{}))
                            .AddAnnotation(new SerializationTypeNameTestAnnotation() { TypeName = null }),
                    })
                        .JsonRepresentation(
                            "{" +
                                "\"@odata.context\":\"http://odata.org/test/$metadata#Collection(TestModel.Person)\"," +
                                "\"value\":[" +
                                    "{" +
                                        "\"@odata.context\":\"$metadata#TestModel.DefaultContainer.Persons/$entity\"," +
                                        "\"Id\":1," +
                                        "\"Friend\":[]" +
                                    "}," +
                                    "{" +
                                        "\"@odata.context\":\"#Persons/$entity\"," +
                                        "\"Id\":2," +
                                        "\"Friend\":[]" +
                                    "}," +
                                    "{" +
                                        "\"@odata.context\":\"#TestModel.Person\"," +
                                        "\"Id\":3," +
                                        "\"Friend\":[]" +
                                    "}," +
                                "]" +
                            "}")
                        .ExpectedEntityType(personType, personsEntitySet),
                    PayloadEdmModel = model
                },
                #endregion Test cases
            };

            this.CombinatorialEngineProvider.RunCombinations(
                testDescriptors,
                this.ReaderTestConfigurationProvider.JsonFormatConfigurations,
                (testDescriptor, testConfiguration) =>
                {
                    if (testConfiguration.IsRequest && testDescriptor.ExpectedException != null)
                    {
                        return;
                    }

                    testDescriptor.TestDescriptorNormalizers.Clear();
                    testDescriptor.RunTest(testConfiguration);
                });
        }
    }
}