//---------------------------------------------------------------------
// <copyright file="ODataBatchReader.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

namespace Microsoft.OData
{
    using Microsoft.OData.Core;
    #region Namespaces

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Threading.Tasks;

    #endregion Namespaces

    /// <summary>
    /// Abstract class for reading OData batch messages; also verifies the proper sequence of read calls on the reader.
    /// </summary>
    public abstract class ODataBatchReader : IODataStreamListener
    {
        /// <summary>The batch-specific URL converter that stores the content IDs found in a changeset and supports resolving cross-referencing URLs.</summary>
        internal readonly ODataBatchPayloadUriConverter PayloadUriConverter;

        /// <summary>The input context to read the content from.</summary>
        private readonly ODataInputContext inputContext;

        /// <summary>True if the writer was created for synchronous operation; false for asynchronous.</summary>
        private readonly bool synchronous;

        /// <summary>The dependency injection container to get related services.</summary>
        private readonly IServiceProvider container;

        /// <summary>The current state of the batch reader.</summary>
        private ODataBatchReaderState batchReaderState;

        /// <summary>The current size of the batch message, i.e., how many query operations and changesets have been read.</summary>
        private uint currentBatchSize;

        /// <summary>The current size of the active changeset, i.e., how many operations have been read for the changeset.</summary>
        private uint currentChangeSetSize;

        /// <summary>An enumeration tracking the state of the current batch operation.</summary>
        private OperationState operationState;

        /// <summary>Whether the reader is currently reading within a changeset.</summary>
        private bool isInChangeset;

        /// <summary>
        /// Content-ID header value for request part with associated entity, which can be referenced by subsequent requests
        /// in the same changeset or other changesets.
        /// </summary>
        private string contentIdToAddOnNextRead;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="inputContext">The input context to read the content from.</param>
        /// <param name="synchronous">true if the reader is created for synchronous operation; false for asynchronous.</param>
        protected ODataBatchReader(ODataInputContext inputContext, bool synchronous)
        {
            Debug.Assert(inputContext != null, "inputContext != null");

            this.inputContext = inputContext;
            this.container = inputContext.Container;
            this.synchronous = synchronous;
            this.PayloadUriConverter = new ODataBatchPayloadUriConverter(inputContext.PayloadUriConverter);
        }

        /// <summary>
        /// An enumeration to track the state of a batch operation.
        /// </summary>
        private enum OperationState
        {
            /// <summary>No action has been performed on the operation.</summary>
            None,

            /// <summary>The batch message for the operation has been created and returned to the caller.</summary>
            MessageCreated,

            /// <summary>The stream of the batch operation message has been requested.</summary>
            StreamRequested,

            /// <summary>The stream of the batch operation message has been disposed.</summary>
            StreamDisposed,
        }

        /// <summary>Gets the current state of the batch reader.</summary>
        /// <returns>The current state of the batch reader.</returns>
        public ODataBatchReaderState State
        {
            get
            {
                this.inputContext.VerifyNotDisposed();
                return this.batchReaderState;
            }

            private set
            {
                this.batchReaderState = value;
            }
        }

        /// <summary>
        /// Public property for the current group id the reader is processing.
        /// The primary usage of this to correlate atomic group id in request and
        /// response operation messages as needed.
        /// </summary>
        public string CurrentGroupId
        {
            get
            {
                return GetCurrentGroupIdImplementation();
            }
        }

        /// <summary>
        /// The input context to read the content from.
        /// </summary>
        protected ODataInputContext InputContext
        {
            get { return this.inputContext; }
        }

        /// <summary>
        /// The reader's Operation state
        /// </summary>
        private OperationState ReaderOperationState
        {
            get { return this.operationState; }
            set { this.operationState = value; }
        }

        /// <summary> Reads the next part from the batch message payload. </summary>
        /// <returns>True if more items were read; otherwise false.</returns>
        public bool Read()
        {
            this.VerifyCanRead(true);
            return this.InterceptException((thisParam) => thisParam.ReadSynchronously());
        }

        /// <summary>Asynchronously reads the next part from the batch message payload.</summary>
        /// <returns>A task that when completed indicates whether more items were read.</returns>
        public Task<bool> ReadAsync()
        {
            this.VerifyCanRead(false);
            return this.InterceptExceptionAsync((thisParam) => thisParam.ReadAsynchronously());
        }

        /// <summary>Returns an <see cref="ODataBatchOperationRequestMessage" /> for reading the content of a batch operation.</summary>
        /// <returns>A request message for reading the content of a batch operation.</returns>
        public ODataBatchOperationRequestMessage CreateOperationRequestMessage()
        {
            this.VerifyCanCreateOperationRequestMessage(synchronousCall: true);
            ODataBatchOperationRequestMessage result =
                this.InterceptException((thisParam) => thisParam.CreateOperationRequestMessageImplementation());
            this.ReaderOperationState = OperationState.MessageCreated;
            this.contentIdToAddOnNextRead = result.ContentId;
            return result;
        }

        /// <summary>Asynchronously returns an <see cref="ODataBatchOperationRequestMessage" /> for reading the content of a batch operation.</summary>
        /// <returns>A task that when completed returns a request message for reading the content of a batch operation.</returns>
        public async Task<ODataBatchOperationRequestMessage> CreateOperationRequestMessageAsync()
        {
            this.VerifyCanCreateOperationRequestMessage(synchronousCall: false);
            ODataBatchOperationRequestMessage result = await this.InterceptExceptionAsync(
                (thisParam) => thisParam.CreateOperationRequestMessageImplementationAsync()).ConfigureAwait(false);
            this.ReaderOperationState = OperationState.MessageCreated;
            this.contentIdToAddOnNextRead = result.ContentId;

            return result;
        }

        /// <summary>Returns an <see cref="ODataBatchOperationResponseMessage" /> for reading the content of a batch operation.</summary>
        /// <returns>A response message for reading the content of a batch operation.</returns>
        public ODataBatchOperationResponseMessage CreateOperationResponseMessage()
        {
            this.VerifyCanCreateOperationResponseMessage(synchronousCall: true);
            ODataBatchOperationResponseMessage result =
                this.InterceptException((thisParam) => thisParam.CreateOperationResponseMessageImplementation());
            this.ReaderOperationState = OperationState.MessageCreated;
            return result;
        }

        /// <summary>Asynchronously returns an <see cref="ODataBatchOperationResponseMessage" /> for reading the content of a batch operation.</summary>
        /// <returns>A task that when completed returns a response message for reading the content of a batch operation.</returns>
        public async Task<ODataBatchOperationResponseMessage> CreateOperationResponseMessageAsync()
        {
            this.VerifyCanCreateOperationResponseMessage(synchronousCall: false);
            ODataBatchOperationResponseMessage result = await this.InterceptExceptionAsync(
                (thisParam) => thisParam.CreateOperationResponseMessageImplementationAsync()).ConfigureAwait(false);

            this.ReaderOperationState = OperationState.MessageCreated;

            return result;
        }

        /// <summary>
        /// This method is called to notify that the content stream for a batch operation has been requested.
        /// </summary>
        void IODataStreamListener.StreamRequested()
        {
            this.operationState = OperationState.StreamRequested;
        }

        /// <summary>
        /// This method is called to notify that the content stream for a batch operation has been requested.
        /// </summary>
        /// <returns>
        /// A task representing any action that is running as part of the status change of the reader;
        /// null if no such action exists.
        /// </returns>
        Task IODataStreamListener.StreamRequestedAsync()
        {
            this.operationState = OperationState.StreamRequested;
            return TaskUtils.CompletedTask;
        }

        /// <summary>
        /// This method is called to notify that the content stream of a batch operation has been disposed.
        /// </summary>
        void IODataStreamListener.StreamDisposed()
        {
            this.operationState = OperationState.StreamDisposed;
        }

        /// <summary>
        /// This method is called asynchronously to notify that the content stream of a batch operation has been disposed.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task IODataStreamListener.StreamDisposedAsync()
        {
            this.operationState = OperationState.StreamDisposed;
            return TaskUtils.CompletedTask;
        }

        /// <summary>
        /// Sets the 'Exception' state and then throws an ODataException with the specified error message.
        /// </summary>
        /// <param name="errorMessage">The error message for the exception.</param>
        protected void ThrowODataException(string errorMessage)
        {
            this.State = ODataBatchReaderState.Exception;
            throw new ODataException(errorMessage);
        }

        /// <summary>
        /// Gets the group id for the current request.
        /// Default implementation here is provided returning null.
        /// </summary>
        /// <returns>The group id for the current request.</returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "A method for consistency with the rest of the API.")]
        protected virtual string GetCurrentGroupIdImplementation()
        {
            return null;
        }

        /// <summary>
        /// Returns the cached <see cref="ODataBatchOperationRequestMessage"/> for reading the content of an operation
        /// in a batch request.
        /// </summary>
        /// <returns>The message that can be used to read the content of the batch request operation from.</returns>
        protected abstract ODataBatchOperationRequestMessage CreateOperationRequestMessageImplementation();

        /// <summary>
        /// Returns the cached <see cref="ODataBatchOperationResponseMessage"/> for reading the content of an operation
        /// in a batch request.
        /// </summary>
        /// <returns>The message that can be used to read the content of the batch response operation from.</returns>
        protected abstract ODataBatchOperationResponseMessage CreateOperationResponseMessageImplementation();

        /// <summary>
        /// Implementation of the reader logic when in state 'Start'.
        /// </summary>
        /// <returns>The batch reader state after the read.</returns>
        protected abstract ODataBatchReaderState ReadAtStartImplementation();

        /// <summary>
        /// Implementation of the reader logic when in state 'Operation'.
        /// </summary>
        /// <returns>The batch reader state after the read.</returns>
        protected abstract ODataBatchReaderState ReadAtOperationImplementation();

        /// <summary>
        /// Implementation of the reader logic when in state 'ChangesetStart'.
        /// </summary>
        /// <returns>The batch reader state after the read.</returns>
        protected abstract ODataBatchReaderState ReadAtChangesetStartImplementation();

        /// <summary>
        /// Implementation of the reader logic when in state 'ChangesetEnd'.
        /// </summary>
        /// <returns>The batch reader state after the read.</returns>
        protected abstract ODataBatchReaderState ReadAtChangesetEndImplementation();

        /// <summary>
        /// Validate the dependsOnIds.
        /// </summary>
        /// <param name="contentId">The request Id</param>
        /// <param name="dependsOnIds">The dependsOn ids specifying current request's prerequisites.</param>
        protected abstract void ValidateDependsOnIds(string contentId, IEnumerable<string> dependsOnIds);

        /// <summary>
        /// Instantiate an <see cref="ODataBatchOperationRequestMessage"/> instance.
        /// </summary>
        /// <param name="streamCreatorFunc">The function for stream creation.</param>
        /// <param name="method">The HTTP method used for this request message.</param>
        /// <param name="requestUri">The request Url for this request message.</param>
        /// <param name="headers">The headers for this request message.</param>
        /// <param name="contentId">The contentId of this request message.</param>
        /// <param name="groupId">The group id that this request belongs to. Can be null.</param>
        /// <param name="dependsOnRequestIds">
        /// The prerequisite request Ids of this request message that could be specified by caller
        /// explicitly.
        /// </param>
        /// <param name="dependsOnIdsValidationRequired">
        /// Whether the <code>dependsOnIds</code> value needs to be validated.</param>
        /// <returns>The <see cref="ODataBatchOperationRequestMessage"/> instance.</returns>
        protected ODataBatchOperationRequestMessage BuildOperationRequestMessage(
            Func<Stream> streamCreatorFunc,
            string method,
            Uri requestUri,
            ODataBatchOperationHeaders headers,
            string contentId,
            string groupId,
            IEnumerable<string> dependsOnRequestIds,
            bool dependsOnIdsValidationRequired)
        {
            if (dependsOnRequestIds != null && dependsOnIdsValidationRequired)
            {
                ValidateDependsOnIds(contentId, dependsOnRequestIds);
            }

            Uri uri = ODataBatchUtils.CreateOperationRequestUri(
                requestUri, this.inputContext.MessageReaderSettings.BaseUri, this.PayloadUriConverter);

            ODataBatchUtils.ValidateReferenceUri(requestUri, dependsOnRequestIds,
                this.inputContext.MessageReaderSettings.BaseUri);

            return new ODataBatchOperationRequestMessage(streamCreatorFunc, method, uri, headers, this,
                contentId, this.PayloadUriConverter, /*writing*/ false, this.container, dependsOnRequestIds, groupId);
        }

        /// <summary>
        /// Instantiate an <see cref="ODataBatchOperationResponseMessage"/> instance and set the status code.
        /// </summary>
        /// <param name="streamCreatorFunc">The function for stream creation.</param>
        /// <param name="statusCode">The status code for the response.</param>
        /// <param name="headers">The headers for this response message.</param>
        /// <param name="contentId">The contentId of this request message.</param>
        /// <param name="groupId">The groupId of this request message.</param>
        /// <returns>The <see cref="ODataBatchOperationResponseMessage"/> instance.</returns>
        protected ODataBatchOperationResponseMessage BuildOperationResponseMessage(
            Func<Stream> streamCreatorFunc,
            int statusCode,
            ODataBatchOperationHeaders headers,
            string contentId,
            string groupId)
        {
            ODataBatchOperationResponseMessage responseMessage = new ODataBatchOperationResponseMessage(
                streamCreatorFunc, headers, this,
                contentId,
                this.PayloadUriConverter.BatchMessagePayloadUriConverter, /*writing*/ false, this.container, groupId)
            {
                StatusCode = statusCode
            };
            return responseMessage;
        }

        /// <summary>
        /// Returns the cached <see cref="ODataBatchOperationRequestMessage"/> for reading the content of an operation
        /// in a batch request.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous read operation.
        /// The value of the TResult parameter contains an <see cref="ODataBatchOperationRequestMessage"/>
        /// that can be used to read the content of the batch request operation from.
        /// </returns>
        protected virtual Task<ODataBatchOperationRequestMessage> CreateOperationRequestMessageImplementationAsync()
        {
            return TaskUtils.GetTaskForSynchronousOperation(this.CreateOperationRequestMessageImplementation);
        }

        /// <summary>
        /// Returns the cached <see cref="ODataBatchOperationResponseMessage"/> for reading the content of an operation
        /// in a batch response.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous read operation.
        /// The value of the TResult parameter contains an <see cref="ODataBatchOperationResponseMessage"/>
        /// that can be used to read the content of the batch response operation from.
        /// </returns>
        protected virtual Task<ODataBatchOperationResponseMessage> CreateOperationResponseMessageImplementationAsync()
        {
            return TaskUtils.GetTaskForSynchronousOperation(this.CreateOperationResponseMessageImplementation);
        }

        /// <summary>
        /// Asynchronous implementation of the reader logic when in state 'Start'.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous read operation.
        /// The value of the TResult parameter contains the batch reader state after the read.
        /// </returns>
        protected virtual Task<ODataBatchReaderState> ReadAtStartImplementationAsync()
        {
            return TaskUtils.GetTaskForSynchronousOperation(this.ReadAtStartImplementation);
        }

        /// <summary>
        /// Asynchronous implementation of the reader logic when in state 'Operation'.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous read operation.
        /// The value of the TResult parameter contains the batch reader state after the read.
        /// </returns>
        protected virtual Task<ODataBatchReaderState> ReadAtOperationImplementationAsync()
        {
            return TaskUtils.GetTaskForSynchronousOperation(this.ReadAtOperationImplementation);
        }

        /// <summary>
        /// Asynchronous implementation of the reader logic when in state 'ChangesetStart'.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous read operation.
        /// The value of the TResult parameter contains the batch reader state after the read.
        /// </returns>
        protected virtual Task<ODataBatchReaderState> ReadAtChangesetStartImplementationAsync()
        {
            return TaskUtils.GetTaskForSynchronousOperation(this.ReadAtChangesetStartImplementation);
        }

        /// <summary>
        /// Asynchronous implementation of the reader logic when in state 'ChangesetEnd'.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous read operation.
        /// The value of the TResult parameter contains the batch reader state after the read.
        /// </returns>
        protected virtual Task<ODataBatchReaderState> ReadAtChangesetEndImplementationAsync()
        {
            return TaskUtils.GetTaskForSynchronousOperation(this.ReadAtChangesetEndImplementation);
        }

        /// <summary>
        /// Increases the size of the current batch message; throws if the allowed limit is exceeded.
        /// </summary>
        private void IncreaseBatchSize()
        {
            if (this.currentBatchSize == this.inputContext.MessageReaderSettings.MessageQuotas.MaxPartsPerBatch)
            {
                throw new ODataException(Error.Format(SRResources.ODataBatchReader_MaxBatchSizeExceeded, this.inputContext.MessageReaderSettings.MessageQuotas.MaxPartsPerBatch));
            }

            this.currentBatchSize++;
        }

        /// <summary>
        /// Increases the size of the current change set; throws if the allowed limit is exceeded.
        /// </summary>
        private void IncreaseChangesetSize()
        {
            if (this.currentChangeSetSize == this.inputContext.MessageReaderSettings.MessageQuotas.MaxOperationsPerChangeset)
            {
                throw new ODataException(Error.Format(SRResources.ODataBatchReader_MaxChangeSetSizeExceeded, this.inputContext.MessageReaderSettings.MessageQuotas.MaxOperationsPerChangeset));
            }

            this.currentChangeSetSize++;
        }

        /// <summary>
        /// Resets the size of the current change set to 0.
        /// </summary>
        private void ResetChangesetSize()
        {
            this.currentChangeSetSize = 0;
        }

        /// <summary>
        /// Reads the next part from the batch message payload.
        /// </summary>
        /// <returns>true if more information was read; otherwise false.</returns>
        private bool ReadSynchronously()
        {
            return this.ReadImplementation();
        }

        /// <summary>
        /// Asynchronously reads the next part from the batch message payload.
        /// </summary>
        /// <returns>A task that when completed indicates whether more information was read.</returns>
        [SuppressMessage("Microsoft.MSInternal", "CA908:AvoidTypesThatRequireJitCompilationInPrecompiledAssemblies", Justification = "API design calls for a bool being returned from the task here.")]
        private Task<bool> ReadAsynchronously()
        {
            return this.ReadImplementationAsync();
        }

        /// <summary>
        /// Continues reading from the batch message payload.
        /// </summary>
        /// <returns>true if more items were read; otherwise false.</returns>
        private bool ReadImplementation()
        {
            Debug.Assert(this.ReaderOperationState != OperationState.StreamRequested, "Should have verified that no operation stream is still active.");

            switch (this.State)
            {
                case ODataBatchReaderState.Initial:
                    // The stream should be positioned at the beginning of the batch content,
                    // that is before the first boundary (or the preamble if there is one).
                    this.State = this.ReadAtStartImplementation();
                    break;

                case ODataBatchReaderState.Operation:
                    // When reaching this state we already read the MIME headers of the operation.
                    // Clients MUST call CreateOperationRequestMessage
                    // or CreateOperationResponseMessage to read at least the headers of the operation.
                    // This is important since we need to read the ContentId header (if present) and
                    // add it to the URL resolver.
                    if (this.ReaderOperationState == OperationState.None)
                    {
                        // No message was created; fail
                        throw new ODataException(SRResources.ODataBatchReader_NoMessageWasCreatedForOperation);
                    }

                    // Reset the operation state; the operation state only
                    // tracks the state of a batch operation while in state Operation.
                    this.ReaderOperationState = OperationState.None;

                    // Also add a pending ContentId header to the URL resolver now. We ensured above
                    // that a message has been created for this operation and thus the headers (incl.
                    // a potential content ID header) have been read.
                    if (this.contentIdToAddOnNextRead != null)
                    {
                        if (this.PayloadUriConverter.ContainsContentId(this.contentIdToAddOnNextRead))
                        {
                            throw new ODataException(
                                Error.Format(SRResources.ODataBatchReader_DuplicateContentIDsNotAllowed, this.contentIdToAddOnNextRead));
                        }

                        this.PayloadUriConverter.AddContentId(this.contentIdToAddOnNextRead);
                        this.contentIdToAddOnNextRead = null;
                    }

                    // When we are done with an operation, we have to skip ahead to the next part
                    // when Read is called again. Note that if the operation stream was never requested
                    // and the content of the operation has not been read, we'll skip it here.
                    Debug.Assert(this.ReaderOperationState == OperationState.None, "Operation state must be 'None' at the end of the operation.");

                    if (this.isInChangeset)
                    {
                        this.IncreaseChangesetSize();
                    }
                    else
                    {
                        this.IncreaseBatchSize();
                    }

                    this.State = this.ReadAtOperationImplementation();

                    break;

                case ODataBatchReaderState.ChangesetStart:
                    // When at the start of a changeset, skip ahead to the first operation in the
                    // changeset (or the end boundary of the changeset).
                    if (this.isInChangeset)
                    {
                        ThrowODataException(SRResources.ODataBatchReaderStream_NestedChangesetsAreNotSupported);
                    }

                    // Increment the batch size at the start of the changeset since we haven't counted it yet
                    // when this state was transitioned into upon detection of this sub-batch.
                    this.IncreaseBatchSize();

                    this.State = this.ReadAtChangesetStartImplementation();
                    if (this.inputContext.MessageReaderSettings.Version <= ODataVersion.V4)
                    {
                        this.PayloadUriConverter.Reset();
                    }

                    this.isInChangeset = true;
                    break;

                case ODataBatchReaderState.ChangesetEnd:
                    // When at the end of a changeset, reset the changeset boundary and the
                    // changeset size and then skip to the next part.
                    this.ResetChangesetSize();
                    this.isInChangeset = false;
                    this.State = this.ReadAtChangesetEndImplementation();
                    break;

                case ODataBatchReaderState.Exception:    // fall through
                case ODataBatchReaderState.Completed:
                    Debug.Assert(false, "Should have checked in VerifyCanRead that we are not in one of these states.");
                    throw new ODataException(Error.Format(SRResources.General_InternalError, InternalErrorCodes.ODataBatchReader_ReadImplementation));

                default:
                    Debug.Assert(false, "Unsupported reader state " + this.State + " detected.");
                    throw new ODataException(Error.Format(SRResources.General_InternalError, InternalErrorCodes.ODataBatchReader_ReadImplementation));
            }

            return this.State != ODataBatchReaderState.Completed && this.State != ODataBatchReaderState.Exception;
        }

        /// <summary>
        /// Asynchronously continues reading from the batch message payload.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous read operation.
        /// The value of the TResult parameter contains true if more items were read; otherwise false.
        /// </returns>
        private async Task<bool> ReadImplementationAsync()
        {
            Debug.Assert(this.ReaderOperationState != OperationState.StreamRequested, "Should have verified that no operation stream is still active.");

            switch (this.State)
            {
                case ODataBatchReaderState.Initial:
                    // The stream should be positioned at the beginning of the batch content,
                    // that is before the first boundary (or the preamble if there is one).
                    this.State = await this.ReadAtStartImplementationAsync()
                        .ConfigureAwait(false);
                    break;

                case ODataBatchReaderState.Operation:
                    // When reaching this state we already read the MIME headers of the operation.
                    // Clients MUST call CreateOperationRequestMessageAsync
                    // or CreateOperationResponseMessageAsync to read at least the headers of the operation.
                    // This is important since we need to read the ContentId header (if present) and
                    // add it to the URL resolver.
                    if (this.ReaderOperationState == OperationState.None)
                    {
                        // No message was created; fail
                        throw new ODataException(SRResources.ODataBatchReader_NoMessageWasCreatedForOperation);
                    }

                    // Reset the operation state; the operation state only
                    // tracks the state of a batch operation while in state Operation.
                    this.ReaderOperationState = OperationState.None;

                    // Also add a pending ContentId header to the URL resolver now. We ensured above
                    // that a message has been created for this operation and thus the headers (incl.
                    // a potential content ID header) have been read.
                    if (this.contentIdToAddOnNextRead != null)
                    {
                        if (this.PayloadUriConverter.ContainsContentId(this.contentIdToAddOnNextRead))
                        {
                            throw new ODataException(
                                Error.Format(SRResources.ODataBatchReader_DuplicateContentIDsNotAllowed, this.contentIdToAddOnNextRead));
                        }

                        this.PayloadUriConverter.AddContentId(this.contentIdToAddOnNextRead);
                        this.contentIdToAddOnNextRead = null;
                    }

                    // When we are done with an operation, we have to skip ahead to the next part
                    // when ReadAsync is called again. Note that if the operation stream was never requested
                    // and the content of the operation has not been read, we'll skip it here.
                    Debug.Assert(this.ReaderOperationState == OperationState.None, "Operation state must be 'None' at the end of the operation.");

                    if (this.isInChangeset)
                    {
                        this.IncreaseChangesetSize();
                    }
                    else
                    {
                        this.IncreaseBatchSize();
                    }

                    this.State = await this.ReadAtOperationImplementationAsync()
                        .ConfigureAwait(false);

                    break;

                case ODataBatchReaderState.ChangesetStart:
                    // When at the start of a changeset, skip ahead to the first operation in the
                    // changeset (or the end boundary of the changeset).
                    if (this.isInChangeset)
                    {
                        ThrowODataException(SRResources.ODataBatchReaderStream_NestedChangesetsAreNotSupported);
                    }

                    // Increment the batch size at the start of the changeset since we haven't counted it yet
                    // when this state was transitioned into upon detection of this sub-batch.
                    this.IncreaseBatchSize();

                    this.State = await this.ReadAtChangesetStartImplementationAsync()
                        .ConfigureAwait(false);
                    if (this.inputContext.MessageReaderSettings.Version <= ODataVersion.V4)
                    {
                        this.PayloadUriConverter.Reset();
                    }

                    this.isInChangeset = true;
                    break;

                case ODataBatchReaderState.ChangesetEnd:
                    // When at the end of a changeset, reset the changeset boundary and the
                    // changeset size and then skip to the next part.
                    this.ResetChangesetSize();
                    this.isInChangeset = false;
                    this.State = await this.ReadAtChangesetEndImplementationAsync()
                        .ConfigureAwait(false);
                    break;

                case ODataBatchReaderState.Exception:    // fall through
                case ODataBatchReaderState.Completed:
                    Debug.Assert(false, "Should have checked in VerifyCanRead that we are not in one of these states.");
                    throw new ODataException(Error.Format(SRResources.General_InternalError, InternalErrorCodes.ODataBatchReader_ReadImplementation));

                default:
                    Debug.Assert(false, "Unsupported reader state " + this.State + " detected.");
                    throw new ODataException(Error.Format(SRResources.General_InternalError, InternalErrorCodes.ODataBatchReader_ReadImplementation));
            }

            return this.State != ODataBatchReaderState.Completed && this.State != ODataBatchReaderState.Exception;
        }

        /// <summary>
        /// Verifies that calling CreateOperationRequestMessage if valid.
        /// </summary>
        /// <param name="synchronousCall">true if the call is to be synchronous; false otherwise.</param>
        private void VerifyCanCreateOperationRequestMessage(bool synchronousCall)
        {
            this.VerifyReaderReady();
            this.VerifyCallAllowed(synchronousCall);

            if (this.inputContext.ReadingResponse)
            {
                this.ThrowODataException(SRResources.ODataBatchReader_CannotCreateRequestOperationWhenReadingResponse);
            }

            if (this.State != ODataBatchReaderState.Operation)
            {
                this.ThrowODataException(Error.Format(SRResources.ODataBatchReader_InvalidStateForCreateOperationRequestMessage, this.State));
            }

            if (this.operationState != OperationState.None)
            {
                this.ThrowODataException(SRResources.ODataBatchReader_OperationRequestMessageAlreadyCreated);
            }
        }

        /// <summary>
        /// Verifies that calling CreateOperationResponseMessage if valid.
        /// </summary>
        /// <param name="synchronousCall">true if the call is to be synchronous; false otherwise.</param>
        private void VerifyCanCreateOperationResponseMessage(bool synchronousCall)
        {
            this.VerifyReaderReady();
            this.VerifyCallAllowed(synchronousCall);

            if (!this.inputContext.ReadingResponse)
            {
                this.ThrowODataException(SRResources.ODataBatchReader_CannotCreateResponseOperationWhenReadingRequest);
            }

            if (this.State != ODataBatchReaderState.Operation)
            {
                this.ThrowODataException(Error.Format(SRResources.ODataBatchReader_InvalidStateForCreateOperationResponseMessage, this.State));
            }

            if (this.operationState != OperationState.None)
            {
                this.ThrowODataException(SRResources.ODataBatchReader_OperationResponseMessageAlreadyCreated);
            }
        }

        /// <summary>
        /// Verifies that calling Read is valid.
        /// </summary>
        /// <param name="synchronousCall">true if the call is to be synchronous; false otherwise.</param>
        private void VerifyCanRead(bool synchronousCall)
        {
            this.VerifyReaderReady();
            this.VerifyCallAllowed(synchronousCall);

            if (this.State == ODataBatchReaderState.Exception || this.State == ODataBatchReaderState.Completed)
            {
                throw new ODataException(Error.Format(SRResources.ODataBatchReader_ReadOrReadAsyncCalledInInvalidState, this.State));
            }
        }

        /// <summary>
        /// Validates that the batch reader is ready to process a new read or create message request.
        /// </summary>
        private void VerifyReaderReady()
        {
            this.inputContext.VerifyNotDisposed();

            // If the operation stream was requested but not yet disposed, the batch reader can't be used to do anything.
            if (this.operationState == OperationState.StreamRequested)
            {
                throw new ODataException(SRResources.ODataBatchReader_CannotUseReaderWhileOperationStreamActive);
            }
        }

        /// <summary>
        /// Verifies that a call is allowed to the reader.
        /// </summary>
        /// <param name="synchronousCall">true if the call is to be synchronous; false otherwise.</param>
        private void VerifyCallAllowed(bool synchronousCall)
        {
            if (synchronousCall)
            {
                if (!this.synchronous)
                {
                    throw new ODataException(SRResources.ODataBatchReader_SyncCallOnAsyncReader);
                }
            }
            else
            {
                if (this.synchronous)
                {
                    throw new ODataException(SRResources.ODataBatchReader_AsyncCallOnSyncReader);
                }
            }
        }

        /// <summary>
        /// Catch any exception thrown by the action passed in; in the exception case move the writer into
        /// state Exception and then rethrow the exception.
        /// </summary>
        /// <typeparam name="T">The type of the result returned from the <paramref name="action"/>.</typeparam>
        /// <param name="action">The action to execute.</param>
        /// <returns>The result of the <paramref name="action"/>.</returns>
        private T InterceptException<T>(Func<ODataBatchReader, T> action)
        {
            try
            {
                return action(this);
            }
            catch (Exception e)
            {
                if (ExceptionUtils.IsCatchableExceptionType(e))
                {
                    this.State = ODataBatchReaderState.Exception;
                }

                throw;
            }
        }

        /// <summary>
        /// Catch any exception thrown by the action passed in; in the exception case move the writer into
        /// state Exception and then rethrow the exception.
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned from the <paramref name="action"/>.</typeparam>
        /// <param name="action">The action to execute.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The value of the TResult parameter contains the result of executing the <paramref name="action"/>.
        /// </returns>
        private async Task<TResult> InterceptExceptionAsync<TResult>(Func<ODataBatchReader, Task<TResult>> action)
        {
            try
            {
                return await action(this).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                if (ExceptionUtils.IsCatchableExceptionType(e))
                {
                    this.State = ODataBatchReaderState.Exception;
                }

                throw;
            }
        }
    }
}
