﻿//---------------------------------------------------------------------
// <copyright file="AvroMediaTypeResolver.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

#if ENABLE_AVRO
namespace Microsoft.Test.OData.PluggableFormat.Avro
{
    using System.Linq;
    using System.Collections.Generic;
    using Microsoft.OData.Core;

    public class AvroMediaTypeResolver : ODataMediaTypeResolver
    {
        private static readonly AvroMediaTypeResolver instance = new AvroMediaTypeResolver();
        private static readonly ODataMediaTypeFormat[] mediaTypeFormats =
            {
                new ODataMediaTypeFormat(
                    new ODataMediaType(AvroConstants.MimeType, AvroConstants.MimeSubType),
                    AvroFormat.Avro)
            };

        private static readonly ODataPayloadKind[] supportedPayloadKinds =
            {
                ODataPayloadKind.Entry,
                ODataPayloadKind.Feed,
                ODataPayloadKind.Property, 
                ODataPayloadKind.Collection, 
                ODataPayloadKind.Parameter, 
                ODataPayloadKind.Error, 
            };

        public static AvroMediaTypeResolver Instance { get { return instance; } }

        private AvroMediaTypeResolver() { }

        public override IEnumerable<ODataMediaTypeFormat> GetMediaTypeFormats(ODataPayloadKind payloadKind)
        {
            return supportedPayloadKinds.Contains(payloadKind) 
                ? base.GetMediaTypeFormats(payloadKind).Concat(mediaTypeFormats) 
                : base.GetMediaTypeFormats(payloadKind);
        }
    }
}
#endif
