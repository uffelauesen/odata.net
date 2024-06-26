﻿//---------------------------------------------------------------------
// <copyright file="ApplyQueryOptionExpression.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

namespace Microsoft.OData.Client
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Linq.Expressions;
    using Microsoft.OData.UriParser.Aggregation;

    /// <summary>
    /// A resource specific expression representing an apply query option.
    /// </summary>
    internal class ApplyQueryOptionExpression : QueryOptionExpression
    {
        /// <summary>
        /// The filter expressions that make the filter predicate
        /// </summary>
        private readonly List<Expression> filterExpressions;

        /// <summary>
        /// Creates an <see cref="ApplyQueryOptionExpression"/> expression.
        /// </summary>
        /// <param name="type">the return type of the expression.</param>
        internal ApplyQueryOptionExpression(Type type)
            : base(type)
        {
            this.Aggregations = new List<Aggregation>();
            this.filterExpressions = new List<Expression>();
            this.GroupingExpressions = new List<Expression>();
            this.KeySelectorMap = new Dictionary<string, Expression>(StringComparer.Ordinal);
        }

        /// <summary>
        /// The <see cref="ExpressionType"/> of the <see cref="Expression"/>.
        /// </summary>
        public override ExpressionType NodeType
        {
            get { return (ExpressionType)ResourceExpressionType.ApplyQueryOption; }
        }

        /// <summary>
        /// Gets the aggregations in the $apply expression
        /// </summary>
        internal List<Aggregation> Aggregations { get; private set; }

        /// <summary>
        /// Adds the conjuncts to the filter expressions
        /// </summary>
        internal void AddPredicateConjuncts(IEnumerable<Expression> predicates)
        {
            this.filterExpressions.AddRange(predicates);
        }

        internal ReadOnlyCollection<Expression> PredicateConjuncts
        {
            get
            {
                return new ReadOnlyCollection<Expression>(this.filterExpressions);
            }
        }

        /// <summary>
        /// Gets filter transformation predicate.
        /// </summary>
        /// <returns>A predicate with all conjuncts AND'd</returns>
        internal Expression GetPredicate()
        {
            return this.filterExpressions.Aggregate((leftExpr, rightExpr) => Expression.And(leftExpr, rightExpr));
        }

        /// <summary>
        /// The individual expressions that make up the GroupBy selector.
        /// </summary>
        internal List<Expression> GroupingExpressions { get; private set; }

        /// <summary>
        /// Gets a mapping of member names in the GroupBy key selector to their respective expressions.
        /// </summary>
        /// <remarks>
        /// This property will contain a mapping of the member {Name} to the expression {d1.Product.Category.Name} given the following GroupBy expression:
        /// dsc.CreateQuery&lt;Sale&gt;("Sales").GroupBy(d1 => d1.Product.Category.Name, (d2, d3) => new { CategoryName = d2, AverageAmount = d3.Average(d4 => d4.Amount) })
        /// </remarks>
        internal Dictionary<string, Expression> KeySelectorMap { get; private set; }

        /// <summary>
        /// Structure for an aggregation. Holds lambda expression plus enum indicating aggregation method
        /// </summary>
        internal struct Aggregation
        {
            /// <summary>
            /// Lambda expression for aggregation selector.
            /// </summary>
            internal readonly Expression Expression;

            /// <summary>
            /// Enum indicating aggregation method.
            /// </summary>
            internal readonly AggregationMethod AggregationMethod;

            /// <summary>
            /// Aggregation alias.
            /// </summary>
            internal readonly string AggregationAlias;

            /// <summary>
            /// Creates an aggregation.
            /// </summary>
            /// <param name="expr">Lambda expression for aggregation selector.</param>
            /// <param name="aggregationMethod">Enum indicating aggregation method.</param>
            internal Aggregation(Expression expr, AggregationMethod aggregationMethod)
                : this(expr, aggregationMethod, string.Empty)
            {
            }

            /// <summary>
            /// Creates an aggregation.
            /// </summary>
            /// <param name="expr">Lambda expression for aggregation selector.</param>
            /// <param name="aggregationMethod">Enum indicating aggregation method.</param>
            /// <param name="aggregationAlias">Aggregation alias.</param>
            internal Aggregation(Expression expr, AggregationMethod aggregationMethod, string aggregationAlias)
            {
                this.Expression = expr;
                this.AggregationMethod = aggregationMethod;
                this.AggregationAlias = aggregationAlias;
            }
        }
    }
}
