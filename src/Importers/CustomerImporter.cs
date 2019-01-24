#pragma warning disable 1591 // disables the warnings about missing Xml code comments
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Foundation.Example.WebUI.Models;
using Foundation.Sdk;
using Foundation.Sdk.Services;
using Swashbuckle.AspNetCore.Annotations;
using Polly.CircuitBreaker;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Foundation.Example.WebUI.Importers
{
    /// <summary>
    /// Class for importing a list of customers into an Object repository
    /// </summary>
    public sealed class HttpCustomerImporter : ICustomerImporter
    {
        private readonly IObjectService _customerService;
        private const string DB_NAME = "bookstore";
        private const string COLLECTION_NAME = "customers";
        private readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings() 
        { 
            NullValueHandling = NullValueHandling.Ignore, 
            ContractResolver = new CamelCasePropertyNamesContractResolver() 
        };

        public HttpCustomerImporter(IObjectService customerService)
        {
            _customerService = customerService;
        }

        public async Task<ImportResult> ImportAsync(List<Customer> customers)
        {
            var importedIds = new Dictionary<string, string>();
            var skippedIds = new Dictionary<string, string>();

            var distinctResult = await _customerService.GetDistinctAsync(DB_NAME, COLLECTION_NAME, "id", "{}");
            var ids = distinctResult.Value;

            foreach (var customer in customers)
            {
                string payload = JsonConvert.SerializeObject(customer, _jsonSerializerSettings);

                ServiceResult<string> result = null;
                try
                {
                    if (ids.Contains(customer.Id))
                    {
                        result = await _customerService.ReplaceAsync(DB_NAME, COLLECTION_NAME, customer.Id, payload);
                    }
                    else
                    {
                        result = await _customerService.InsertAsync(DB_NAME, COLLECTION_NAME, customer.Id, payload);
                    }

                    if (result.IsSuccess)
                    {
                        importedIds.Add(customer.Id, result.Status == 201 ? "inserted" : "updated");
                    }
                    else
                    {
                        skippedIds.Add(customer.Id, result.Details.Detail);
                    }
                }
                catch (Exception ex)
                {
                    skippedIds.Add(customer.Id, ex.Message);
                }
            }

            var importResult = new ImportResult()
            {
                SkippedIds = skippedIds,
                ImportedIds = importedIds
            };

            return importResult;
        }
    }
}

#pragma warning restore 1591