﻿//---------------------------------------------------------------------
// <copyright file="DurationTests.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

namespace Microsoft.Test.OData.Tests.Client.PrimitiveTypes
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Xml;
    using Microsoft.OData;
    using Microsoft.OData.Edm;
    using Microsoft.Test.OData.Services.TestServices;
    using Microsoft.Test.OData.Services.TestServices.ODataWCFServiceReference;
    using Microsoft.Test.OData.Tests.Client.Common;
    using Xunit;
    using ODataClient = Microsoft.OData.Client;

    /// <summary>
    /// Tests for Edm.Duration primitive type
    /// Send query and verify the results from the service implemented using ODataLib and EDMLib.
    /// </summary>
    public class DurationTests : ODataWCFServiceTestsBase<InMemoryEntities>, IDisposable
    {
        public DurationTests() : base(ServiceDescriptors.ODataWCFServiceDescriptor)
        {

        }

        [Fact]
        public void QueryEntitySet()
        {
            ODataMessageReaderSettings readerSettings = new ODataMessageReaderSettings() { BaseUri = ServiceBaseUri };
            foreach (var mimeType in mimeTypes)
            {
                var requestMessage = new HttpWebRequestMessage(new Uri(ServiceBaseUri.AbsoluteUri + "Customers", UriKind.Absolute));
                requestMessage.SetHeader("Accept", mimeType);

                var responseMessage = requestMessage.GetResponse();
                Assert.Equal(200, responseMessage.StatusCode);

                if (!mimeType.Contains(MimeTypes.ODataParameterNoMetadata))
                {
                    using (var messageReader = new ODataMessageReader(responseMessage, readerSettings, Model))
                    {
                        var reader = messageReader.CreateODataResourceSetReader();

                        while (reader.Read())
                        {
                            if (reader.State == ODataReaderState.ResourceEnd)
                            {
                                ODataResource entry = reader.Item as ODataResource;
                                if (entry != null && entry.TypeName.EndsWith("Customer"))
                                {
                                    Assert.NotNull(Assert.IsType<ODataProperty>(entry.Properties.Single(p => p.Name == "TimeBetweenLastTwoOrders")).Value);
                                }
                            }
                        }
                    }
                }
            }
        }

        [Fact]
        public void QueryEntityInstance()
        {
            ODataMessageReaderSettings readerSettings = new ODataMessageReaderSettings() { BaseUri = ServiceBaseUri };
            foreach (var mimeType in mimeTypes)
            {
                var requestMessage = new HttpWebRequestMessage(new Uri(ServiceBaseUri.AbsoluteUri + "Customers(1)", UriKind.Absolute));
                requestMessage.SetHeader("Accept", mimeType);

                var responseMessage = requestMessage.GetResponse();
                Assert.Equal(200, responseMessage.StatusCode);

                if (!mimeType.Contains(MimeTypes.ODataParameterNoMetadata))
                {
                    using (var messageReader = new ODataMessageReader(responseMessage, readerSettings, Model))
                    {
                        var reader = messageReader.CreateODataResourceReader();

                        while (reader.Read())
                        {
                            if (reader.State == ODataReaderState.ResourceEnd)
                            {
                                ODataResource entry = reader.Item as ODataResource;
                                if (entry != null && entry.TypeName.EndsWith("Customer"))
                                {
                                    Assert.Equal(new TimeSpan(1), Assert.IsType<ODataProperty>(entry.Properties.Single(p => p.Name == "TimeBetweenLastTwoOrders")).Value);
                                }
                            }
                        }
                    }
                }
            }
        }

        [Fact]
        public void QueryEntityProperty()
        {
            ODataMessageReaderSettings readerSettings = new ODataMessageReaderSettings() { BaseUri = ServiceBaseUri };
            foreach (var mimeType in mimeTypes)
            {
                var requestMessage = new HttpWebRequestMessage(new Uri(ServiceBaseUri.AbsoluteUri + "Customers(1)/TimeBetweenLastTwoOrders", UriKind.Absolute));

                requestMessage.SetHeader("Accept", mimeType);
                if (mimeType == MimeTypes.ApplicationAtomXml)
                {
                    requestMessage.SetHeader("Accept", "text/html, application/xhtml+xml, */*");
                }

                var responseMessage = requestMessage.GetResponse();
                Assert.Equal(200, responseMessage.StatusCode);

                if (!mimeType.Contains(MimeTypes.ODataParameterNoMetadata))
                {
                    ODataProperty property = null;
                    using (var messageReader = new ODataMessageReader(responseMessage, readerSettings, Model))
                    {
                        property = messageReader.ReadProperty();
                    }

                    Assert.Equal(new TimeSpan(1), property.Value);
                }
            }
        }

        [Fact]
        public void QueryEntityNavigation()
        {
            ODataMessageReaderSettings readerSettings = new ODataMessageReaderSettings() { BaseUri = ServiceBaseUri };
            foreach (var mimeType in mimeTypes)
            {
                var requestMessage = new HttpWebRequestMessage(new Uri(ServiceBaseUri.AbsoluteUri + "Orders(7)/CustomerForOrder", UriKind.Absolute));
                requestMessage.SetHeader("Accept", mimeType);

                var responseMessage = requestMessage.GetResponse();
                Assert.Equal(200, responseMessage.StatusCode);

                if (!mimeType.Contains(MimeTypes.ODataParameterNoMetadata))
                {
                    using (var messageReader = new ODataMessageReader(responseMessage, readerSettings, Model))
                    {
                        var reader = messageReader.CreateODataResourceReader();

                        while (reader.Read())
                        {
                            if (reader.State == ODataReaderState.ResourceEnd)
                            {
                                ODataResource entry = reader.Item as ODataResource;
                                if (entry != null && entry.TypeName.EndsWith("Customer"))
                                {
                                    Assert.Equal(new TimeSpan(2), Assert.IsType<ODataProperty>(entry.Properties.Single(p => p.Name == "TimeBetweenLastTwoOrders")).Value);
                                }
                            }
                        }
                    }
                }
            }
        }

        [Fact]
        public void QueryEntityPropertyValue()
        {
            ODataMessageReaderSettings readerSettings = new ODataMessageReaderSettings() { BaseUri = ServiceBaseUri };

            var requestMessage = new HttpWebRequestMessage(new Uri(ServiceBaseUri.AbsoluteUri + "Customers(1)/TimeBetweenLastTwoOrders/$value", UriKind.Absolute));
            requestMessage.SetHeader("Accept", "*/*");

            var responseMessage = requestMessage.GetResponse();
            Assert.Equal(200, responseMessage.StatusCode);

            using (var messageReader = new ODataMessageReader(responseMessage, readerSettings, Model))
            {
                var propertyValue = messageReader.ReadValue(EdmCoreModel.Instance.GetString(false));
                Assert.Equal("PT0.0000001S", propertyValue);
            }
        }

#if !(NETCOREAPP1_0 || NETCOREAPP2_0)
        [Fact]
        public void InsertAndUpdatePropertyValueUsingLinq()
        {
            TimeSpan timespan = new TimeSpan((new Random()).Next());
            var queryable = TestClientContext.Orders.Where(c => c.ShelfLife == timespan) as ODataClient.DataServiceQuery<Order>;
            Assert.EndsWith("/Orders?$filter=ShelfLife eq duration'" + XmlConvert.ToString(timespan) + "'", queryable.RequestUri.OriginalString, StringComparison.Ordinal);

            var result1 = queryable.ToList();
            Assert.True(result1.Count == 0);

            int orderID = (new Random()).Next();

            // create an entity
            Order order = new Order()
            {
                OrderID = orderID,
                OrderDate = new DateTimeOffset(new DateTime(2011, 3, 4, 16, 3, 57)),
                ShelfLife = timespan,
                OrderShelfLifes = new ObservableCollection<TimeSpan>() { timespan }
            };
            TestClientContext.AddToOrders(order);
            TestClientContext.SaveChanges();

            // query and verify
            var result2 = queryable.ToList();
            Assert.Single(result2);
            Assert.Equal(orderID, result2[0].OrderID);

            // update the Duration properties
            timespan = new TimeSpan((new Random()).Next());
            order.ShelfLife = timespan;
            order.OrderShelfLifes = new ObservableCollection<TimeSpan>() { timespan };
            TestClientContext.UpdateObject(order);
            TestClientContext.SaveChanges(ODataClient.SaveChangesOptions.ReplaceOnUpdate);

            // query Duration property
            var queryable2 = TestClientContext.Orders.Where(c => c.OrderID == orderID).Select(c => c.ShelfLife).FirstOrDefault();
            Assert.True(queryable2 != null);
            Assert.Equal(timespan, queryable2);

            // query collection of Duration property
            var queryable3 = (from c in TestClientContext.Orders
                              where c.OrderID == orderID
                              select c.OrderShelfLifes).FirstOrDefault();

            Assert.True(queryable3.Count == 1);
            Assert.Equal(timespan, queryable3[0]);

            // delete entity and validate
            TestClientContext.DeleteObject(order);
            TestClientContext.SaveChanges(ODataClient.SaveChangesOptions.ReplaceOnUpdate);
            var queryable4 = TestClientContext.Execute<Order>(new Uri("Orders()?$filter=ShelfLife eq duration'" + XmlConvert.ToString(timespan) + "'", UriKind.Relative));
            Assert.True(queryable4.Count() == 0);
        }
#endif

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}