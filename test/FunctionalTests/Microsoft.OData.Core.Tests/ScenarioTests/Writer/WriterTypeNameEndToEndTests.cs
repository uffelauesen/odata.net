﻿//---------------------------------------------------------------------
// <copyright file="WriterTypeNameEndToEndTests.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.OData.Edm;
using Xunit;

namespace Microsoft.OData.Tests.ScenarioTests.Writer
{
    /// <summary>
    /// These tests baseline the end-to-end behavior of when type names are written on the wire,
    /// based on the format and metadata level along with whether the AlwaysAddTypeAnnotationsForDerivedTypes 
    /// flag is set on the message writer settings. These tests are not meant to be exhaustive, but
    /// should catch major end-to-end problems. The unit tests for the individual components are more extensive.
    /// </summary>
    public class WriterTypeNameEndToEndTests : IDisposable
    {
        private Lazy<ODataMessageWriter> messageWriter;
        private ODataMessageWriterSettings settings;
        private Lazy<string> writerOutput;
        private static readonly Uri ServiceDocumentUri = new Uri("http://odata.org/");

        private const string JsonNoMetadata = "application/json;odata.metadata=none";
        private const string JsonMinimalMetadata = "application/json;odata.metadata=minimal";
        private const string JsonFullMetadata = "application/json;odata.metadata=full";
        private const string atom = "application/atom+xml";

        public WriterTypeNameEndToEndTests()
        {
            var model = new EdmModel();
            var type = new EdmEntityType("TestModel", "TestEntity", /* baseType */ null, /* isAbstract */ false, /* isOpen */ true);
            var keyProperty = type.AddStructuralProperty("DeclaredInt16", EdmPrimitiveTypeKind.Int16);
            type.AddKeys(new[] { keyProperty });

            // Note: DerivedPrimitive is declared as a Geography, but its value below will be set to GeographyPoint, which is derived from Geography.
            type.AddStructuralProperty("DerivedPrimitive", EdmPrimitiveTypeKind.Geography);
            var container = new EdmEntityContainer("TestModel", "Container");
            var set = container.AddEntitySet("Set", type);
            model.AddElement(type);
            model.AddElement(container);

            var writerStream = new MemoryStream();
            this.settings = new ODataMessageWriterSettings();
            this.settings.SetServiceDocumentUri(ServiceDocumentUri);

            // Make the message writer and entry writer lazy so that individual tests can tweak the settings before the message writer is created.
            this.messageWriter = new Lazy<ODataMessageWriter>(() =>
                new ODataMessageWriter(
                    (IODataResponseMessage)new InMemoryMessage { Stream = writerStream },
                    this.settings,
                    model));

            var entryWriter = new Lazy<ODataWriter>(() => this.messageWriter.Value.CreateODataResourceWriter(set, type));

            var valueWithAnnotation = new ODataPrimitiveValue(45);
            valueWithAnnotation.TypeAnnotation = new ODataTypeAnnotation("TypeNameFromSTNA");

            var propertiesToWrite = new List<ODataProperty>
            {
                new ODataProperty
                {
                    Name = "DeclaredInt16", Value = (Int16)42
                },
                new ODataProperty
                {
                    Name = "UndeclaredDecimal", Value = (Decimal)4.5
                },
                new ODataProperty
                {
                    // Note: value is more derived than the declared type.
                    Name = "DerivedPrimitive", Value = Microsoft.Spatial.GeographyPoint.Create(42, 45)
                },
                new ODataProperty()
                {
                    Name = "PropertyWithSTNA", Value = valueWithAnnotation
                }
            };

            this.writerOutput = new Lazy<string>(() =>
            {
                entryWriter.Value.WriteStart(new ODataResource { Properties = propertiesToWrite });
                entryWriter.Value.WriteEnd();
                entryWriter.Value.Flush();
                writerStream.Seek(0, SeekOrigin.Begin);
                return new StreamReader(writerStream).ReadToEnd();
            });
        }

        public void Dispose()
        {
            if (messageWriter.IsValueCreated)
            {
                messageWriter.Value.Dispose();
            }
        }

        [Fact]
        public void TypeNameShouldBeWrittenCorrectlyInMinimalMetadataWhenAlwaysAddTypeAnnotationsForDerivedTypesIsOff()
        {
            this.settings.SetContentType(JsonMinimalMetadata, null);
            this.settings.AlwaysAddTypeAnnotationsForDerivedTypes = false;

            Assert.DoesNotContain("DeclaredInt16@odata.type", this.writerOutput.Value);
            Assert.Contains("UndeclaredDecimal@odata.type\":\"#Decimal\"", this.writerOutput.Value);
            Assert.Contains("DerivedPrimitive@odata.type\":\"#GeographyPoint\"", this.writerOutput.Value);
            Assert.Contains("PropertyWithSTNA@odata.type\":\"#TypeNameFromSTNA\"", this.writerOutput.Value);
        }

        [Fact]
        public void TypeNameShouldBeWrittenCorrectlyInFullMetadataWhenAlwaysAddTypeAnnotationsForDerivedTypesIsOff()
        {
            this.settings.SetContentType(JsonFullMetadata, null);
            this.settings.AlwaysAddTypeAnnotationsForDerivedTypes = false;

            Assert.Contains("DeclaredInt16@odata.type", this.writerOutput.Value);
            Assert.Contains("UndeclaredDecimal@odata.type\":\"#Decimal\"", this.writerOutput.Value);
            Assert.Contains("DerivedPrimitive@odata.type\":\"#GeographyPoint\"", this.writerOutput.Value);
            Assert.Contains("PropertyWithSTNA@odata.type\":\"#TypeNameFromSTNA\"", this.writerOutput.Value);
        }

        [Fact]
        public void TypeNameShouldBeWrittenCorrectlyInNoMetadataWhenAlwaysAddTypeAnnotationsForDerivedTypesIsOff()
        {
            this.settings.SetContentType(JsonNoMetadata, null);
            this.settings.AlwaysAddTypeAnnotationsForDerivedTypes = false;

            Assert.DoesNotContain("DeclaredInt16@odata.type", this.writerOutput.Value);
            Assert.DoesNotContain("UndeclaredDecimal@odata.type\":\"#Decimal\"", this.writerOutput.Value);
            Assert.DoesNotContain("DerivedPrimitive@odata.type\":\"#GeographyPoint\"", this.writerOutput.Value);
            Assert.DoesNotContain("PropertyWithSTNA@odata.type\":\"#TypeNameFromSTNA\"", this.writerOutput.Value);
        }

        [Fact]
        public void TypeNameShouldBeWrittenCorrectlyInMinimalMetadataWhenAlwaysAddTypeAnnotationsForDerivedTypesIsSet()
        {
            this.settings.SetContentType(JsonMinimalMetadata, null);
            this.settings.AlwaysAddTypeAnnotationsForDerivedTypes = true;

            Assert.DoesNotContain("DeclaredInt16@odata.type", this.writerOutput.Value);
            Assert.Contains("UndeclaredDecimal@odata.type\":\"#Decimal\"", this.writerOutput.Value);
            Assert.Contains("DerivedPrimitive@odata.type\":\"#GeographyPoint\"", this.writerOutput.Value);
            Assert.Contains("PropertyWithSTNA@odata.type\":\"#TypeNameFromSTNA\"", this.writerOutput.Value);
        }

        [Fact]
        public void TypeNameShouldBeWrittenCorrectlyInNoMetadataWhenAlwaysAddTypeAnnotationsForDerivedTypesIsSet()
        {
            this.settings.SetContentType(JsonNoMetadata, null);
            this.settings.AlwaysAddTypeAnnotationsForDerivedTypes = true;

            Assert.DoesNotContain("DeclaredInt16@odata.type", this.writerOutput.Value);
            Assert.Contains("UndeclaredDecimal@odata.type\":\"#Decimal\"", this.writerOutput.Value);
            Assert.Contains("DerivedPrimitive@odata.type\":\"#GeographyPoint\"", this.writerOutput.Value);
            Assert.Contains("PropertyWithSTNA@odata.type\":\"#TypeNameFromSTNA\"", this.writerOutput.Value);
        }

        [Fact]
        public void TypeNameShouldBeWrittenCorrectlyInNoMetadataWhenAlwaysAddTypeAnnotationsForDerivedTypesIsSetWithJsonP()
        {
            this.settings.SetContentType(JsonNoMetadata, null);
            this.settings.JsonPCallback = "callback";
            this.settings.AlwaysAddTypeAnnotationsForDerivedTypes = true;

            Assert.DoesNotContain("DeclaredInt16@odata.type", this.writerOutput.Value);
            Assert.Contains("UndeclaredDecimal@odata.type\":\"#Decimal\"", this.writerOutput.Value);
            Assert.Contains("DerivedPrimitive@odata.type\":\"#GeographyPoint\"", this.writerOutput.Value);
            Assert.Contains("PropertyWithSTNA@odata.type\":\"#TypeNameFromSTNA\"", this.writerOutput.Value);
        }

        [Fact]
        public void TypeNameShouldBeWrittenCorrectlyInFullMetadataWhenAlwaysAddTypeAnnotationsForDerivedTypesIsSet()
        {
            this.settings.SetContentType(JsonFullMetadata, null);
            this.settings.AlwaysAddTypeAnnotationsForDerivedTypes = true;

            Assert.Contains("DeclaredInt16@odata.type", this.writerOutput.Value);
            Assert.Contains("UndeclaredDecimal@odata.type\":\"#Decimal\"", this.writerOutput.Value);
            Assert.Contains("DerivedPrimitive@odata.type\":\"#GeographyPoint\"", this.writerOutput.Value);
            Assert.Contains("PropertyWithSTNA@odata.type\":\"#TypeNameFromSTNA\"", this.writerOutput.Value);
        }
    }
}
