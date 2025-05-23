﻿//---------------------------------------------------------------------
// <copyright file="DollarSegmentTests.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

using Microsoft.OData.E2E.TestCommon.Common.Client.EndToEnd.Default;

namespace Microsoft.OData.Client.E2E.Tests.KeyAsSegmentTests.Tests;

public class KeyAsSegmentTestHelper
{
    private readonly Uri _baseUri;
    private readonly Container _context;

    public KeyAsSegmentTestHelper(Uri serviceBaseUri, Container context)
    {
        _baseUri = serviceBaseUri;
        _context = context;
    }

    public Container CreateWrappedContext()
    {
        var context = new Container(_baseUri)
        {
            HttpClientFactory = _context.HttpClientFactory,
            UrlKeyDelimiter = DataServiceUrlKeyDelimiter.Slash
        };

        return context;
    }
}
