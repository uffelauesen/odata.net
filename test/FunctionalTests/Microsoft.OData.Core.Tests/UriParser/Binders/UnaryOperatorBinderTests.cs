﻿//---------------------------------------------------------------------
// <copyright file="UnaryOperatorBinderTests.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

using System;
using Microsoft.OData.UriParser;
using Microsoft.OData.Edm;
using Xunit;
using Microsoft.OData.Core;

namespace Microsoft.OData.Tests.UriParser.Binders
{
    /// <summary>
    /// Unit tests for the UnaryOperatorBinder.
    /// </summary>
    public class UnaryOperatorBinderTests
    {
        private readonly IEdmModel model = HardCodedTestModel.TestModel;
        private UnaryOperatorBinder unaryOperatorBinder;

        private SingleValueNode parameterSingleValueQueryNode;
        private SingleValueNode extensionSingleValueQueryNode;

        [Fact]
        public void NegateOperatorShouldResultInUnaryOperatorNode()
        {
            this.unaryOperatorBinder = new UnaryOperatorBinder(this.BindMethodThatReturnsSingleValueQueryNode);
            this.parameterSingleValueQueryNode = new UnaryOperatorNode(UnaryOperatorKind.Negate, new UnaryOperatorNode(UnaryOperatorKind.Not, new ConstantNode(null)));
            var unaryOperatorQueryToken = new UnaryOperatorToken(UnaryOperatorKind.Negate, new LiteralToken(true));
            var resultNode = this.unaryOperatorBinder.BindUnaryOperator(unaryOperatorQueryToken);
            resultNode.ShouldBeUnaryOperatorNode(UnaryOperatorKind.Negate);
        }

        [Fact]
        public void NegateOperatorOnNullLiteralShouldResultInUnaryOperatorNodeWithNullType()
        {
            this.unaryOperatorBinder = new UnaryOperatorBinder(this.BindMethodThatReturnsSingleValueQueryNode);
            this.parameterSingleValueQueryNode = new ConstantNode(null);
            var unaryOperatorQueryToken = new UnaryOperatorToken(UnaryOperatorKind.Negate, new LiteralToken(true));
            var resultNode = this.unaryOperatorBinder.BindUnaryOperator(unaryOperatorQueryToken);
            resultNode.ShouldBeUnaryOperatorNode(UnaryOperatorKind.Negate)
                      .Operand.ShouldBeConstantQueryNode<object>(null);
            var node = Assert.IsType<UnaryOperatorNode>(resultNode);
            Assert.Null(node.TypeReference);
        }

        [Fact]
        public void NegateOperatorOnOpenPropertyShouldResultInUnaryOperatorNodeWithNullType()
        {
            const string OpenPropertyName = "SomeProperty";
            this.unaryOperatorBinder = new UnaryOperatorBinder(this.BindMethodThatReturnsSingleValueQueryNode);
            this.parameterSingleValueQueryNode = new SingleValueOpenPropertyAccessNode(new ConstantNode(null), OpenPropertyName);
            var unaryOperatorQueryToken = new UnaryOperatorToken(UnaryOperatorKind.Negate, new LiteralToken(true));
            var resultNode = this.unaryOperatorBinder.BindUnaryOperator(unaryOperatorQueryToken);
            resultNode.ShouldBeUnaryOperatorNode(UnaryOperatorKind.Negate)
                      .Operand.ShouldBeSingleValueOpenPropertyAccessQueryNode(OpenPropertyName);
            var node = Assert.IsType<UnaryOperatorNode>(resultNode);
            Assert.Null(node.TypeReference);
        }

        [Fact]
        public void NotOperatorShouldResultInUnaryOperatorNode()
        {
            this.unaryOperatorBinder = new UnaryOperatorBinder(this.BindMethodThatReturnsSingleValueQueryNode);
            this.parameterSingleValueQueryNode = new SingleValueFunctionCallNode("func", null, EdmCoreModel.Instance.GetBoolean(false));
            var unaryOperatorQueryToken = new UnaryOperatorToken(UnaryOperatorKind.Not, new LiteralToken("foo"));
            var resultNode = this.unaryOperatorBinder.BindUnaryOperator(unaryOperatorQueryToken);
            var node = resultNode.ShouldBeUnaryOperatorNode(UnaryOperatorKind.Not);
            Assert.Equal("Edm.Boolean", node.TypeReference.FullName());
        }

        [Fact]
        public void ExtensionSingleValueQueryNodeWithValidTypeReferenceShouldResultInUnaryOperatorNode()
        {
            this.unaryOperatorBinder = new UnaryOperatorBinder(this.BindMethodThatReturnsSingleValueExtensionQueryNode);
            this.extensionSingleValueQueryNode = new SingleValueQueryNodeWithTypeReference();
            var unaryOperatorQueryToken = new UnaryOperatorToken(UnaryOperatorKind.Negate, new LiteralToken("foo"));
            var resultNode = this.unaryOperatorBinder.BindUnaryOperator(unaryOperatorQueryToken);
            var node = resultNode.ShouldBeUnaryOperatorNode(UnaryOperatorKind.Negate);
            Assert.Equal("Edm.Int32", node.TypeReference.FullName());
        }

        [Fact]
        public void NonSingleValueQueryNodeShouldFail()
        {
            this.unaryOperatorBinder = new UnaryOperatorBinder(this.BindMethodThatReturnsCollectionQueryNode);
            var unaryOperatorToken = new UnaryOperatorToken(UnaryOperatorKind.Negate, new LiteralToken("foo"));
            Action bind = () => this.unaryOperatorBinder.BindUnaryOperator(unaryOperatorToken);

            bind.Throws<ODataException>(Error.Format(SRResources.MetadataBinder_UnaryOperatorOperandNotSingleValue, UnaryOperatorKind.Negate.ToString()));
        }

        [Fact]
        public void SingleValueQueryNodeWithImcompatibleTypeReferenceShouldFail()
        {
            this.unaryOperatorBinder = new UnaryOperatorBinder(this.BindMethodThatReturnsSingleValueQueryNode);
            this.parameterSingleValueQueryNode = new SingleValueFunctionCallNode("func", null, EdmCoreModel.Instance.GetDateTimeOffset(false));
            var unaryOperatorQueryToken = new UnaryOperatorToken(UnaryOperatorKind.Negate, new LiteralToken("foo"));
            Action bind = () => this.unaryOperatorBinder.BindUnaryOperator(unaryOperatorQueryToken);

            bind.Throws<ODataException>(Error.Format(SRResources.MetadataBinder_IncompatibleOperandError, "Edm.DateTimeOffset", UnaryOperatorKind.Negate));
        }

        /// <summary>
        /// We substitute the following methods for the MetadataBinder.Bind method to keep the tests from growing too large in scope.
        /// In practice this does the same thing.
        /// </summary>
        private SingleValueNode BindMethodThatReturnsSingleValueQueryNode(QueryToken queryToken)
        {
            return this.parameterSingleValueQueryNode;
        }

        private SingleValueNode BindMethodThatReturnsSingleValueExtensionQueryNode(QueryToken queryToken)
        {
            return this.extensionSingleValueQueryNode;
        }

        private CollectionNode BindMethodThatReturnsCollectionQueryNode(QueryToken queryToken)
        {
            return null;
        }

        private class SingleValueQueryNodeWithTypeReference : SingleValueNode
        {
            internal override InternalQueryNodeKind InternalKind
            {
                get { throw new NotImplementedException(); }
            }

            public override T Accept<T>(QueryNodeVisitor<T> visitor)
            {
                throw new NotImplementedException();
            }

            public override IEdmTypeReference TypeReference
            {
                get
                {
                    return EdmCoreModel.Instance.GetInt32(false);
                }
            }
        }
    }
}
