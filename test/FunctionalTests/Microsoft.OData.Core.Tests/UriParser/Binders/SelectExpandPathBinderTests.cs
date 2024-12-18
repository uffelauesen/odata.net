﻿//---------------------------------------------------------------------
// <copyright file="SelectExpandPathBinderTests.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

using System;
using Microsoft.OData.UriParser;
using Microsoft.OData.Edm;
using Xunit;
using System.Linq;
using Microsoft.OData.Core;

namespace Microsoft.OData.Tests.UriParser.Binders
{
    public class SelectExpandPathBinderTests
    {
        private static readonly ODataUriResolver DefaultUriResolver = ODataUriResolver.GetUriResolver(null);

        [Fact]
        public void SingleLevelTypeSegmentWorks()
        {
            NonSystemToken typeSegment = new NonSystemToken("Fully.Qualified.Namespace.Employee", null, new NonSystemToken("WorkEmail", null, null));
            PathSegmentToken firstNonTypeToken;
            IEdmStructuredType entityType = HardCodedTestModel.GetPersonType();
            var result = SelectExpandPathBinder.FollowTypeSegments(typeSegment, HardCodedTestModel.TestModel, 800, DefaultUriResolver, ref entityType, out firstNonTypeToken);
            var segment = Assert.Single(result);
            var caseSegment = Assert.IsType<TypeSegment>(segment);
            Assert.Same(HardCodedTestModel.GetEmployeeType(), caseSegment.EdmType);
            Assert.Same(HardCodedTestModel.GetEmployeeType(), entityType);
            firstNonTypeToken.ShouldBeNonSystemToken("WorkEmail");
        }

        [Fact]
        public void DeepPath()
        {
            NonSystemToken typeSegment = new NonSystemToken("Fully.Qualified.Namespace.Employee", null, new NonSystemToken("Fully.Qualified.Namespace.Manager", null, new NonSystemToken("NumberOfReports", null, null)));
            PathSegmentToken firstNonTypeToken;
            IEdmStructuredType entityType = HardCodedTestModel.GetPersonType();
            var result = SelectExpandPathBinder.FollowTypeSegments(typeSegment, HardCodedTestModel.TestModel, 800, DefaultUriResolver, ref entityType, out firstNonTypeToken);
            var castSegments = result.OfType<TypeSegment>();
            Assert.Equal(2, castSegments.Count());
            Assert.Contains(castSegments, x => x.EdmType == HardCodedTestModel.GetEmployeeType());
            Assert.Contains(castSegments, x => x.EdmType == HardCodedTestModel.GetManagerType());
            Assert.Same(HardCodedTestModel.GetManagerType(), entityType);
            firstNonTypeToken.ShouldBeNonSystemToken("NumberOfReports");
        }

        [Fact]
        public void InvalidTypeSegmentThrowsException()
        {
            NonSystemToken typeSegment = new NonSystemToken("Stuff", null, new NonSystemToken("stuff", null, null));
            PathSegmentToken firstNonTypeToken;
            IEdmStructuredType entityType = HardCodedTestModel.GetPersonType();
            Action followInvalidTypeSegment = () => SelectExpandPathBinder.FollowTypeSegments(typeSegment, HardCodedTestModel.TestModel, 800, DefaultUriResolver, ref entityType, out firstNonTypeToken);
            followInvalidTypeSegment.Throws<ODataException>(Error.Format(SRResources.SelectExpandPathBinder_FollowNonTypeSegment, "Stuff"));
        }

        [Fact]
        public void MaxRecursiveDepthIsRespected()
        {
            NonSystemToken typeSegment = new NonSystemToken("Fully.Qualified.Namespace.Employee", null, new NonSystemToken("Fully.Qualified.Namespace.Manager", null, new NonSystemToken("NumberOfReports", null, null)));
            PathSegmentToken firstNonTypeToken;
            IEdmStructuredType entityType = HardCodedTestModel.GetPersonType();
            Action followLongChain = () => SelectExpandPathBinder.FollowTypeSegments(typeSegment, HardCodedTestModel.TestModel, 1, DefaultUriResolver, ref entityType, out firstNonTypeToken);
            followLongChain.Throws<ODataException>(SRResources.ExpandItemBinder_PathTooDeep);
        }
    }
}
