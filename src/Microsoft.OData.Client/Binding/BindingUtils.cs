﻿//---------------------------------------------------------------------
// <copyright file="BindingUtils.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

namespace Microsoft.OData.Client
{
#region Namespaces

    using System;
    using System.Diagnostics;
#endregion

    /// <summary>Utilities for binding related operations</summary>
    internal static class BindingUtils
    {
        /// <summary>
        /// Throw if the entity set name is null or empty
        /// </summary>
        /// <param name="entitySetName">entity set name.</param>
        /// <param name="entity">entity instance for which the entity set name is generated.</param>
        internal static void ValidateEntitySetName(string entitySetName, object entity)
        {
            if (String.IsNullOrEmpty(entitySetName))
            {
                throw new InvalidOperationException(Error.Format(SRResources.DataBinding_Util_UnknownEntitySetName, entity.GetType().FullName));
            }
        }

        /// <summary>
        /// Given a collection type, gets it's entity type
        /// </summary>
        /// <param name="collectionType">Input collection type</param>
        /// <returns>Generic type argument for the collection</returns>
        internal static Type GetCollectionEntityType(Type collectionType)
        {
            while (collectionType != null)
            {
                if (collectionType.IsGenericType() && WebUtil.IsDataServiceCollectionType(collectionType.GetGenericTypeDefinition()))
                {
                    return collectionType.GetGenericArguments()[0];
                }

                collectionType = collectionType.GetBaseType();
            }

            return null;
        }

#if DEBUG
        /// <summary>Verifies the absence of observer for an DataServiceCollection</summary>
        /// <typeparam name="T">Type of DataServiceCollection</typeparam>
        /// <param name="oec">Non-typed collection object</param>
        /// <param name="sourceProperty">Collection property of the source object which is being assigned to</param>
        /// <param name="sourceType">Type of the source object</param>
        /// <param name="model">The client model.</param>
        internal static void VerifyObserverNotPresent<T>(object oec, string sourceProperty, Type sourceType, ClientEdmModel model)
#else
        /// <summary>Verifies the absence of observer for an DataServiceCollection</summary>
        /// <typeparam name="T">Type of DataServiceCollection</typeparam>
        /// <param name="oec">Non-typed collection object</param>
        /// <param name="sourceProperty">Collection property of the source object which is being assigned to</param>
        /// <param name="sourceType">Type of the source object</param>
        internal static void VerifyObserverNotPresent<T>(object oec, string sourceProperty, Type sourceType)
#endif
        {
#if DEBUG
            Debug.Assert(BindingEntityInfo.IsDataServiceCollection(oec.GetType(), model), "Must be an DataServiceCollection.");
#endif
            DataServiceCollection<T> typedCollection = oec as DataServiceCollection<T>;

            if (typedCollection.Observer != null)
            {
                throw new InvalidOperationException(Error.Format(SRResources.DataBinding_CollectionPropertySetterValueHasObserver, sourceProperty, sourceType));
            }
        }
    }
}
