// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;

namespace Microsoft.IIS.Administration.WebApiCompatShim
{
    public interface IHttpRequestMessageFeature
    {
        HttpRequestMessage HttpRequestMessage { get; set; }
    }
}
