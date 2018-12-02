using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Foundation.Example.WebUI.Models;
using Foundation.Example.WebUI.Importers;
using Foundation.Example.WebUI.Converters;
using Foundation.Sdk;
using Foundation.Sdk.Data;
using Foundation.Sdk.Services;
using Swashbuckle.AspNetCore.Annotations;
using Polly.CircuitBreaker;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Foundation.Example.WebUI.Controllers
{
    /// <summary>
    /// Customer controller
    /// </summary>
    [Route("api/1.0/book")]
    [ApiController]
    public sealed class BookController : ControllerBase
    {
        #region Private Members
        private const string BOOK_MIME_TYPE = "application/vnd.mycompany.myapp.book+json; version=1.0";
        private const string CIRCUIT_BREAKER_ERROR = "Book Service is inoperative, please try later on. (Business message due to Circuit-Breaker)";
        private readonly IObjectRepository<string> _bookRepository;
        #endregion // Private Members

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="bookRepository">FDNS Object Book repository</param>
        public BookController(IObjectRepository<string> bookRepository)
        {
            _bookRepository = bookRepository;
        }

        // GET api/1.0/5
        /// <summary>
        /// Gets the Book with a specified ID
        /// </summary>
        /// <param name="id">The ID of the book</param>
        /// <returns>Book</returns>
        [Produces(BOOK_MIME_TYPE)]
        [HttpGet("{id}")]
        [SwaggerResponse(200, "If the book was found successfully", typeof(string))]
        [SwaggerResponse(400, "If there was a client error handling this request")]
        [SwaggerResponse(401, "If the HTTP header lacks a valid OAuth2 token")]
        [SwaggerResponse(403, "If the HTTP header has a valid OAuth2 token but lacks the appropriate scope to use this route")]
        [SwaggerResponse(404, "If the book with this ID was not found")]
        [Authorize(Common.READ_AUTHORIZATION_NAME)]
        public async Task<ActionResult<string>> Get([FromRoute] string id)
        {
            try
            {
                ServiceResult<string> result = await _bookRepository.GetAsync(id); // This is what calls the FDNS Object microservice through the FDNS .NET Core SDK
                return HandleObjectResult<string>(result); // Determine what to do based on the HTTP response code
            }
            catch (BrokenCircuitException)
            {
                return StatusCode(500, CIRCUIT_BREAKER_ERROR);
            }
        }

        // POST api/1.0/6
        /// <summary>
        /// Inserts a Book with a specified ID
        /// </summary>
        /// <remarks>
        /// Sample request to insert a new book with an id of 6:
        ///
        ///     POST /api/1.0/6
        ///     {
        ///         "id": "6",
        ///         "title": "Red Storm Rising",
        ///     }
        ///
        /// </remarks>
        /// <param name="id">The ID of the book</param>
        /// <param name="payload">The Json representation of the book</param>
        /// <returns>Book that was inserted</returns>
        [Produces(BOOK_MIME_TYPE)]
        [Consumes(BOOK_MIME_TYPE)]
        [HttpPost("{id}")]
        [SwaggerResponse(201, "Returns the book that was just created", typeof(string))]
        [SwaggerResponse(400, "If the route parameters or json payload contain invalid data")]
        [SwaggerResponse(401, "If the HTTP header lacks a valid OAuth2 token")]
        [SwaggerResponse(403, "If the HTTP header has a valid OAuth2 token but lacks the appropriate scope to use this route")]
        [SwaggerResponse(406, "If the content type is invalid")]
        [SwaggerResponse(413, "If the Json payload is too large")]
        [SwaggerResponse(415, "If the media type is invalid")]
        [Authorize(Common.INSERT_AUTHORIZATION_NAME)]
        public async Task<ActionResult<string>> Post([FromRoute] string id, [FromBody] string payload)
        {
            try
            {
                ServiceResult<string> result = await _bookRepository.InsertAsync(id, payload);
                return HandleObjectResult<string>(result, id);
            }
            catch (BrokenCircuitException)
            {
                return StatusCode(500, CIRCUIT_BREAKER_ERROR);
            }
        }

        // POST api/1.0/find
        /// <summary>
        /// Finds matching Customers based on the MongoDB find syntax
        /// </summary>
        /// <remarks>
        /// Sample request to find one or more customers with an age between 25 and 35 and a name of either "John" or "Jane":
        ///
        ///     POST /api/1.0/find
        ///     {
        ///         firstName:
        ///         {
        ///             $in: [ "Jane", "John" ]
        ///         },
        ///         age: { $gte: 25, $lte: 35 }
        ///     }
        ///
        /// </remarks>
        /// <param name="findCriteria">The MongoDB find syntax to use</param>
        /// <returns>Array of Customers that match the provided regular expression and inputs</returns>
        [Consumes("text/plain")]
        [Produces("application/json")]
        [HttpPost("find")]
        [SwaggerResponse(200, "Returns the objects that match the inputs to the find operation", typeof(List<Customer>))]
        [SwaggerResponse(400, "If the find expression contains any invalid inputs")]
        [SwaggerResponse(401, "If the HTTP header lacks a valid OAuth2 token")]
        [SwaggerResponse(403, "If the HTTP header has a valid OAuth2 token but lacks the appropriate scope to use this route")]
        [SwaggerResponse(406, "If the find expression is submitted as anything other than text/plain")]
        [SwaggerResponse(413, "If the find expression is too large")]
        [SwaggerResponse(415, "If the media type is invalid")]
        [Authorize(Common.READ_AUTHORIZATION_NAME)]
        public async Task<ActionResult<SearchResults<string>>> Find([FromBody] string findCriteria)
        {
            try
            {
                ServiceResult<SearchResults<string>> result = await _bookRepository.FindAsync(0, 10, "name", findCriteria, false);
                return HandleObjectResult<SearchResults<string>>(result);
            }
            catch (BrokenCircuitException)
            {
                return StatusCode(500, CIRCUIT_BREAKER_ERROR);
            }
        }

        private ActionResult<T> HandleObjectResult<T>(ServiceResult<T> result, string id = "")
        {
            switch (result.Code)
            {
                case HttpStatusCode.OK:
                    return Ok(result.Response);
                 case HttpStatusCode.Created:
                    return CreatedAtAction(nameof(Get), new { id = id }, result.Response);
                default:
                    return StatusCode((int)result.Code, result.Message);
            }
        }
    }
}
