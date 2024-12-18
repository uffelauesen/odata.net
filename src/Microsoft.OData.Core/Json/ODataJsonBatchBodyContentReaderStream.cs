﻿//---------------------------------------------------------------------
// <copyright file="ODataJsonBatchBodyContentReaderStream.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

namespace Microsoft.OData.Json
{
    using Microsoft.OData.Core;
    #region Namespaces

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    #endregion Namespaces

    /// <summary>
    /// Wrapper stream backed by memory stream containing body content of request or response in Json batch.
    /// </summary>
    internal sealed class ODataJsonBatchBodyContentReaderStream : MemoryStream
    {
        /// <summary>Listener interface to be notified of operation changes.</summary>
        private IODataStreamListener listener;

        /// <summary>
        /// Cached body content which needs to be processed later when we have information
        /// about the content-type value.
        /// </summary>
        private string cachedBodyContent = null;

        /// <summary>
        /// Constructor using default encoding (Base64Url without the BOM preamble).
        /// </summary>
        /// <param name="listener">The batch operation listener.</param>
        internal ODataJsonBatchBodyContentReaderStream(IODataStreamListener listener)
        {
            this.listener = listener;
        }

        /// <summary>
        /// Enum type for data type of body content.
        /// </summary>
        private enum BatchPayloadBodyContentType
        {
            // The body content contains Json data.
            Json,

            // The body content contains textual data.
            Textual,

            // The body content contains binary data, e.g. JPEG image raw data.
            Binary
        }

        /// <summary>
        /// Populates the body content the Json reader is referencing.
        /// Since the content-type header might not be available at this point (when "headers" attribute
        /// is read after the "body" attribute), if the content-type is not json the body content is
        /// first stored into a string which will be used to populate the stream when the content-type
        /// header is read later.
        /// </summary>
        /// <param name="jsonReader">The Json reader providing access to the data.</param>
        /// <param name="contentTypeHeader">The request's content-type header value.</param>
        /// <returns>True if body content is written to stream; false otherwise.</returns>
        internal bool PopulateBodyContent(IJsonReader jsonReader, string contentTypeHeader)
        {
            bool isStreamPopulated = false;

            BatchPayloadBodyContentType? contentType = DetectBatchPayloadBodyContentType(jsonReader, contentTypeHeader);

            if (contentType == null)
            {
                // We don't have deterministic content-type, cached the string content.
                Debug.Assert(jsonReader.NodeType == JsonNodeType.PrimitiveValue, "jsonReader.NodeType == JsonNodeType.PrimitiveValue");
                this.cachedBodyContent = jsonReader.ReadStringValue();
                Debug.Assert(isStreamPopulated == false, "isStreamPopulated == false");
            }
            else
            {
                // We have content-type figured out and should be able to populate the stream.
                switch (contentType)
                {
                    case BatchPayloadBodyContentType.Json:
                    {
                        WriteJsonContent(jsonReader);
                    }

                    break;

                    case BatchPayloadBodyContentType.Textual:
                    {
                        string bodyContent = string.Format(CultureInfo.InvariantCulture,
                            "\"{0}\"", jsonReader.ReadStringValue());
                        WriteBytes(Encoding.UTF8.GetBytes(bodyContent));
                    }

                    break;

                    case BatchPayloadBodyContentType.Binary:
                    {
                        // Body content is a base64url encoded string. We could have used HttpServerUtility.UrlTokenDecode(string)
                        // directly but it would introduce new dependency of System.Web.dll.
                        string encoded = jsonReader.ReadStringValue();
                        WriteBinaryContent(encoded);
                    }

                    break;

                    default:
                        throw new ODataException(Error.Format(SRResources.ODataJsonBatchBodyContentReaderStream_UnsupportedContentTypeInHeader, contentType));
                }

                isStreamPopulated = true;
            }

            return isStreamPopulated;
        }

        /// <summary>
        /// Populates the stream with the cached body content according to the content-type specified.
        /// </summary>
        /// <param name="contentTypeHeader">The content-type header value.</param>
        internal void PopulateCachedBodyContent(string contentTypeHeader)
        {
            Debug.Assert(this.cachedBodyContent != null);

            ODataMediaType mediaType = GetMediaType(contentTypeHeader);

            // Content-Type can be either textual or binary, since json content is not cached.
            if (mediaType != null && mediaType.Type.Equals(MimeConstants.MimeTextType, StringComparison.Ordinal))
            {
                // Explicit check for matching of textual content-type.
                string bodyContent = string.Format(CultureInfo.InvariantCulture,
                    "\"{0}\"", this.cachedBodyContent);
                WriteBytes(Encoding.UTF8.GetBytes(bodyContent));
            }
            else
            {
                // Anything else, treated as binary content-type.
                WriteBinaryContent(cachedBodyContent);
            }
        }

        /// <summary>
        /// Asynchronously populates the body content the Json reader is referencing.
        /// Since the content-type header might not be available at this point (when "headers" attribute
        /// is read after the "body" attribute), if the content-type is not json the body content is
        /// first stored into a string which will be used to populate the stream when the content-type
        /// header is read later.
        /// </summary>
        /// <param name="jsonReader">The Json reader providing access to the data.</param>
        /// <param name="contentTypeHeader">The request's content-type header value.</param>
        /// <returns>
        /// A task that represents the asynchronous write operation.
        /// The value of the TResult parameter contains true if body content is written to stream; false otherwise.
        /// </returns>
        internal async Task<bool> PopulateBodyContentAsync(IJsonReader jsonReader, string contentTypeHeader)
        {
            bool isStreamPopulated = false;

            BatchPayloadBodyContentType? contentType = DetectBatchPayloadBodyContentType(jsonReader, contentTypeHeader);

            if (contentType == null)
            {
                // We don't have deterministic content-type, cache the string content.
                Debug.Assert(jsonReader.NodeType == JsonNodeType.PrimitiveValue, "jsonReader.NodeType == JsonNodeType.PrimitiveValue");
                this.cachedBodyContent = await jsonReader.ReadStringValueAsync()
                    .ConfigureAwait(false);
                Debug.Assert(isStreamPopulated == false, "isStreamPopulated == false");
            }
            else
            {
                // We have content-type figured out and should be able to populate the stream.
                switch (contentType)
                {
                    case BatchPayloadBodyContentType.Json:
                        await WriteJsonContentAsync(jsonReader)
                            .ConfigureAwait(false);

                        break;

                    case BatchPayloadBodyContentType.Textual:
                        string bodyContent = string.Format(CultureInfo.InvariantCulture,
                            "\"{0}\"", jsonReader.ReadStringValue());
                        await WriteBytesAsync(Encoding.UTF8.GetBytes(bodyContent))
                            .ConfigureAwait(false);

                        break;

                    case BatchPayloadBodyContentType.Binary:
                        // Body content is a base64url encoded string. We could have used HttpServerUtility.UrlTokenDecode(string)
                        // directly but it would introduce new dependency of System.Web.dll.
                        string encoded = await jsonReader.ReadStringValueAsync()
                            .ConfigureAwait(false);
                        await WriteBinaryContentAsync(encoded)
                            .ConfigureAwait(false);

                        break;

                    default:
                        throw new ODataException(Error.Format(SRResources.ODataJsonBatchBodyContentReaderStream_UnsupportedContentTypeInHeader, contentType));
                }

                isStreamPopulated = true;
            }

            return isStreamPopulated;
        }

        /// <summary>
        /// Asynchronously populates the stream with the cached body content according to the content-type specified.
        /// </summary>
        /// <param name="contentTypeHeader">The content-type header value.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        internal async Task PopulateCachedBodyContentAsync(string contentTypeHeader)
        {
            Debug.Assert(this.cachedBodyContent != null);

            ODataMediaType mediaType = GetMediaType(contentTypeHeader);

            // Content-Type can be either textual or binary, since json content is not cached.
            if (mediaType != null && mediaType.Type.Equals(MimeConstants.MimeTextType, StringComparison.Ordinal))
            {
                // Explicit check for matching of textual content-type.
                string bodyContent = string.Format(CultureInfo.InvariantCulture,
                    "\"{0}\"", this.cachedBodyContent);
                await WriteBytesAsync(Encoding.UTF8.GetBytes(bodyContent))
                    .ConfigureAwait(false);
            }
            else
            {
                // Anything else, treated as binary content-type.
                await WriteBinaryContentAsync(cachedBodyContent)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Disposes the object.
        /// </summary>
        /// <param name="disposing">True if called from Dispose; false if called form the finalizer.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.listener != null)
                {
                    // Tell the listener that the stream is being disposed; we expect
                    // that no asynchronous action is triggered by doing so.
                    this.listener.StreamDisposed();
                    this.listener = null;
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Detects batch item (request or response) body content's data type.
        /// The content of the "body" property can be either Json type or binary type.
        /// </summary>
        /// <param name="jsonReader">The json reader that provides access to the json object.</param>
        /// <param name="contentTypeHeader">The request's content-type header value.</param>
        /// <returns>The detected batch operation payload type. Can be null if content-type header information
        /// is not yet available and json reader is reading primitive type.</returns>
        private static BatchPayloadBodyContentType? DetectBatchPayloadBodyContentType(
            IJsonReader jsonReader, string contentTypeHeader)
        {
            BatchPayloadBodyContentType? result = null;
            ODataMediaType mediaType = GetMediaType(contentTypeHeader);

            if (jsonReader.NodeType == JsonNodeType.StartObject)
            {
                result = BatchPayloadBodyContentType.Json;
            }
            else if (jsonReader.NodeType == JsonNodeType.PrimitiveValue
                  && (mediaType != null || contentTypeHeader != null))
            {
                // We have processed the content-type header and should determine batch operation request
                // content type now.
                if (mediaType != null && mediaType.Type.Equals(MimeConstants.MimeTextType, StringComparison.Ordinal))
                {
                    // Explicit check for matching of textual content-type.
                    result = BatchPayloadBodyContentType.Textual;
                }
                else
                {
                    // Anything else, treated as binary content-type.
                    result = BatchPayloadBodyContentType.Binary;
                }
            }

            return result;
        }

        /// <summary>
        /// Parses the content-type header to get the media type without parameters.
        /// </summary>
        /// <param name="contentTypeHeader">The content-type header value.</param>
        /// <returns>The media type object without parameters, or null if value is null or empty.</returns>
        private static ODataMediaType GetMediaType(string contentTypeHeader)
        {
            if (String.IsNullOrEmpty(contentTypeHeader))
            {
                return null;
            }

            contentTypeHeader = contentTypeHeader.Trim();
            int idx = contentTypeHeader.IndexOf(';', StringComparison.Ordinal);
            string fullType = idx != -1
                ? contentTypeHeader.Substring(0, idx)
                : contentTypeHeader;

            int idxSlash = fullType.IndexOf('/', StringComparison.Ordinal);
            string type = null;
            string subType = null;
            if (idxSlash != -1)
            {
                type = fullType.Substring(0, idxSlash);
                subType = fullType.Substring(idxSlash + 1);
            }
            else
            {
                type = fullType;
            }

            return new ODataMediaType(type, subType);
        }

        /// <summary>
        /// Reads off the data of the starting Json object from the Json reader,
        /// and populate the data into the memory stream.
        /// </summary>
        /// <param name="reader"> The json reader pointing at the json structure whose data needs to
        /// be populated into an memory stream.
        /// </param>
        private void WriteJsonContent(IJsonReader reader)
        {
            // Reader is on the value node after the "body" property name node.
            IJsonWriter jsonWriter = new JsonWriter(
                new StreamWriter(this),
                reader.IsIeee754Compatible);

            WriteCurrentJsonObject(reader, jsonWriter);
            this.Flush();
            this.Position = 0;
        }

        /// <summary>
        /// Decodes the base64url-encoded string and writes the binary bytes to the underlying memory stream.
        /// </summary>
        /// <param name="encodedContent">The base64url-encoded content.</param>
        private void WriteBinaryContent(string encodedContent)
        {
            byte[] bytes = Convert.FromBase64String(encodedContent.Replace('-', '+').Replace('_', '/'));
            WriteBytes(bytes);
        }

        /// <summary>
        /// Writes the binary bytes to the underlying memory stream.
        /// </summary>
        /// <param name="bytes">The raw bytes to be written into the stream.</param>
        private void WriteBytes(byte[] bytes)
        {
            this.Write(bytes, 0, bytes.Length);
            this.Flush();
            this.Position = 0;
        }

        /// <summary>
        /// Writes the current Json object.
        /// </summary>
        /// <param name="reader">The Json reader providing the data.</param>
        /// <param name="jsonWriter">The Json writer writes data into memory stream.</param>
        private static void WriteCurrentJsonObject(IJsonReader reader, IJsonWriter jsonWriter)
        {
            Stack<JsonNodeType> nodeTypes = new Stack<JsonNodeType>();

            do
            {
                switch (reader.NodeType)
                {
                    case JsonNodeType.PrimitiveValue:
                        {
                            object value = reader.GetValue();
                            if (value != null)
                            {
                                jsonWriter.WritePrimitiveValue(value);
                            }
                            else
                            {
                                jsonWriter.WriteValue((string)null);
                            }
                        }

                        break;

                    case JsonNodeType.Property:
                        {
                            jsonWriter.WriteName(reader.GetValue().ToString());
                        }

                        break;

                    case JsonNodeType.StartObject:
                        {
                            nodeTypes.Push(reader.NodeType);
                            jsonWriter.StartObjectScope();
                        }

                        break;

                    case JsonNodeType.StartArray:
                        {
                            nodeTypes.Push(reader.NodeType);
                            jsonWriter.StartArrayScope();
                        }

                        break;

                    case JsonNodeType.EndObject:
                        {
                            Debug.Assert(nodeTypes.Peek() == JsonNodeType.StartObject);
                            nodeTypes.Pop();
                            jsonWriter.EndObjectScope();
                        }

                        break;

                    case JsonNodeType.EndArray:
                        {
                            Debug.Assert(nodeTypes.Peek() == JsonNodeType.StartArray);
                            nodeTypes.Pop();
                            jsonWriter.EndArrayScope();
                        }

                        break;

                    default:
                        {
                            throw new ODataException(Error.Format(SRResources.ODataJsonBatchBodyContentReaderStream_UnexpectedNodeType, reader.NodeType));
                        }
                }

                reader.ReadNext(); // This can be EndOfInput, where nodeTypes should be empty.
            }
            while (nodeTypes.Count != 0);

            jsonWriter.Flush();
        }

        /// <summary>
        /// Asynchronously reads off the data of the starting Json object from the Json reader,
        /// and populate the data into the memory stream.
        /// </summary>
        /// <param name="reader"> The json reader pointing at the json structure whose data needs to
        /// be populated into an memory stream.
        /// </param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        private async Task WriteJsonContentAsync(IJsonReader reader)
        {
            // Reader is on the value node after the "body" property name node.
            IJsonWriter jsonWriter = new JsonWriter(
                new StreamWriter(this),
                reader.IsIeee754Compatible);

            await WriteCurrentJsonObjectAsync(reader, jsonWriter)
                .ConfigureAwait(false);
            await this.FlushAsync()
                .ConfigureAwait(false);
            this.Position = 0;
        }

        /// <summary>
        /// Asynchronously decodes the base64url-encoded string and writes the binary bytes to the underlying memory stream.
        /// </summary>
        /// <param name="encodedContent">The base64url-encoded content.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        private Task WriteBinaryContentAsync(string encodedContent)
        {
            byte[] bytes = Convert.FromBase64String(encodedContent.Replace('-', '+').Replace('_', '/'));
            
            return WriteBytesAsync(bytes);
        }

        /// <summary>
        /// Asynchronously writes the binary bytes to the underlying memory stream.
        /// </summary>
        /// <param name="bytes">The raw bytes to be written into the stream.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        private async Task WriteBytesAsync(byte[] bytes)
        {
            await this.WriteAsync(bytes, 0, bytes.Length)
                .ConfigureAwait(false);
            await this.FlushAsync()
                .ConfigureAwait(false);
            this.Position = 0;
        }

        /// <summary>
        /// Asynchronously writes the current Json object.
        /// </summary>
        /// <param name="reader">The Json reader providing the data.</param>
        /// <param name="jsonWriter">The Json writer writes data into memory stream.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        private static async Task WriteCurrentJsonObjectAsync(IJsonReader reader, IJsonWriter jsonWriter)
        {
            Stack<JsonNodeType> nodeTypes = new Stack<JsonNodeType>();

            do
            {
                switch (reader.NodeType)
                {
                    case JsonNodeType.PrimitiveValue:
                        object primitiveValue;

                        if ((primitiveValue = await reader.GetValueAsync().ConfigureAwait(false)) != null)
                        {
                            await jsonWriter.WritePrimitiveValueAsync(primitiveValue)
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            await jsonWriter.WriteValueAsync((string)null)
                                .ConfigureAwait(false);
                        }

                        break;

                    case JsonNodeType.Property:
                        object propertyName = await reader.GetValueAsync()
                                .ConfigureAwait(false);
                        await jsonWriter.WriteNameAsync(propertyName.ToString())
                            .ConfigureAwait(false);

                        break;

                    case JsonNodeType.StartObject:
                        nodeTypes.Push(reader.NodeType);
                        await jsonWriter.StartObjectScopeAsync()
                            .ConfigureAwait(false);

                        break;

                    case JsonNodeType.StartArray:
                        nodeTypes.Push(reader.NodeType);
                        await jsonWriter.StartArrayScopeAsync()
                            .ConfigureAwait(false);

                        break;

                    case JsonNodeType.EndObject:
                        Debug.Assert(nodeTypes.Peek() == JsonNodeType.StartObject);
                        nodeTypes.Pop();
                        await jsonWriter.EndObjectScopeAsync()
                            .ConfigureAwait(false);

                        break;

                    case JsonNodeType.EndArray:
                        Debug.Assert(nodeTypes.Peek() == JsonNodeType.StartArray);
                        nodeTypes.Pop();
                        await jsonWriter.EndArrayScopeAsync()
                            .ConfigureAwait(false);

                        break;

                    default:
                        throw new ODataException(Error.Format(SRResources.ODataJsonBatchBodyContentReaderStream_UnexpectedNodeType, reader.NodeType));
                }

                await reader.ReadNextAsync()
                    .ConfigureAwait(false); // This can be EndOfInput, where nodeTypes should be empty.
            }
            while (nodeTypes.Count != 0);

            await jsonWriter.FlushAsync()
                .ConfigureAwait(false);
        }
    }
}
