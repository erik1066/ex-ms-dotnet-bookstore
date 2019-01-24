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
using Foundation.Sdk.Services;
using Swashbuckle.AspNetCore.Annotations;
using Polly.CircuitBreaker;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace Foundation.Example.WebUI.Controllers
{
    /// <summary>
    /// Customer controller
    /// </summary>
    [Route("api/1.0/book")]
    [ApiController]
    [SwaggerResponse(400, "Client error, such as invalid inputs or malformed Json")]
    [SwaggerResponse(401, "HTTP header lacks a valid OAuth2 token")]
    [SwaggerResponse(403, "HTTP header has a valid OAuth2 token but lacks the appropriate scope to use this route")]
    public sealed class BookController : ControllerBase
    {
        #region Private Members
        private const string BOOK_MIME_TYPE = "application/vnd.mycompany.myapp.book+json; version=1.0";
        private const string CIRCUIT_BREAKER_ERROR = "Book Service is inoperative, please try later on. (Business message due to Circuit-Breaker)";
        private readonly IObjectService _bookRepository;
        private const string DB_NAME = "bookstore";
        private const string COLLECTION_NAME = "books";
        private readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings() 
        {
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        #endregion // Private Members

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="bookRepository">FDNS Object Book repository</param>
        public BookController(IObjectService bookRepository)
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
        [SwaggerResponse(404, "If the book with this ID was not found")]
        [Authorize(Common.READ_AUTHORIZATION_NAME)]
        public async Task<ActionResult<string>> Get([FromRoute] string id)
        {
            try
            {
                ServiceResult<string> result = await _bookRepository.GetAsync(DB_NAME, COLLECTION_NAME, id); // This is what calls the FDNS Object microservice through the FDNS .NET Core SDK
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
        /// Notes on behavior:
        /// - An `_id` string property will be added to the Json payload with the value specified in the `id` route parameter.
        /// - If the Json payload already has an `_id` property, the value for `_id` will be **overwritten** with the value specified in the `id` route paramter.
        /// - If there is already an object in the collection with the specified id, a 400 (bad request) will be returned.
        /// 
        /// Sample request to insert a new Json document with an id of 1:
        ///
        ///     POST /api/1.0/bookstore/books/1
        ///     {
        ///         "title": "War and Peace",
        ///         "author": "Leo Tolstoy",
        ///         "year": 1869,
        ///         "weight": 28.8
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
        [SwaggerResponse(406, "If the content type is invalid")]
        [SwaggerResponse(413, "If the Json payload is too large")]
        [SwaggerResponse(415, "If the media type is invalid")]
        [Authorize(Common.INSERT_AUTHORIZATION_NAME)]
        public async Task<ActionResult<string>> Post([FromRoute] string id, [FromBody] string payload)
        {
            try
            {
                ServiceResult<string> result = await _bookRepository.InsertAsync(DB_NAME, COLLECTION_NAME, id, payload);
                return HandleObjectResult<string>(result, id);
            }
            catch (BrokenCircuitException)
            {
                return StatusCode(500, CIRCUIT_BREAKER_ERROR);
            }
        }

        // POST api/1.0/find
        /// <summary>
        /// Finds one or more objects that match the specified criteria
        /// </summary>
        /// <remarks>
        /// An API for the MongoDB Find operation. See [MongoDB Find Syntax documentation](https://docs.mongodb.com/manual/reference/method/db.collection.find/) for comprehensive examples and a list of supported operations. Note that [MongoDB Projections](https://docs.mongodb.com/manual/reference/method/db.collection.find/#find-projection) are unsupported in version 1.0 of this API.
        /// <para/>
        /// Sample request to find all books with a page count greater than 400:
        ///
        ///     POST /api/1.0/bookstore/books/find
        ///     { pages: { $gt: 400 } }
        ///
        /// <para/>
        /// Sample request to find the book with an `_id` value of "5":
        ///
        ///     POST /api/1.0/bookstore/books/find
        ///     { _id: "5" }
        ///
        /// <para/>
        /// Sample request to find the book with an `_id` value of either "5" or ObjectId("507c35dd8fada716c89d0013"):
        ///
        ///     POST /api/1.0/bookstore/books/find
        ///     { _id: { $in: [ "5", ObjectId("507c35dd8fada716c89d0013") ] } }
        ///
        /// <para/>
        /// Sample request to find books with a publish date after January 1st, 1900:
        ///
        ///     POST /api/1.0/bookstore/books/find
        ///     { publishDate: { $gte: new Date('1900-01-01') } }
        ///
        /// <para/>
        /// Sample request to find books that start with either `the` or the letter `a` (case-insensitive).
        ///
        ///     POST /api/1.0/bookstore/books/find
        ///     { title: /^(the|a)/i }
        ///
        /// <para/>
        /// Sample request to find books that start with `the` or `a` (case-insensitive), that have more than 100 pages, and where the author is either John Steinbeck, Stephen Crane, or Miguel De Cervantes.
        ///
        ///     POST /api/1.0/bookstore/books/find
        ///     { 
        ///         title: /^(the|a)/i,
        ///         pages: { $gt: 100 },
        ///         author: { $in: [ "John Steinbeck", "Stephen Crane", "Miguel De Cervantes" ] }
        ///     }
        ///
        /// </remarks>
        /// <param name="findExpression">The Json find expression</param>
        /// <returns>Array of books</returns>
        [Produces("application/json")]
        [Consumes("text/plain")]
        [HttpPost("find")]
        [SwaggerResponse(200, "Returns the objects that match the inputs to the find operation")]
        [SwaggerResponse(406, "If the find expression is submitted as anything other than text/plain")]
        [SwaggerResponse(413, "If the find expression is too large")]
        [SwaggerResponse(415, "If the media type is invalid")]
        [Authorize(Common.READ_AUTHORIZATION_NAME)]
        public async Task<ActionResult> Find([FromBody] string findExpression)
        {
            try
            {
                FindCriteria options = new FindCriteria()
                {
                    Start = 0,
                    Limit = 10,
                    SortFieldName = "name",
                    SortDirection = System.ComponentModel.ListSortDirection.Ascending
                };

                ServiceResult<SearchResults> result = await _bookRepository.FindAsync(DB_NAME, COLLECTION_NAME, findExpression, options);
                return HandleSearchResult(result);
            }
            catch (BrokenCircuitException)
            {
                return StatusCode(500, CIRCUIT_BREAKER_ERROR);
            }
        }

        private ActionResult HandleSearchResult(ServiceResult<SearchResults> result)
        {
            switch (result.Status)
            {
                case 200:
                    return Ok(result.Value.StringifyItems());
                default:
                    return StatusCode(result.Status, result.Details);
            }
        }

        private ActionResult<T> HandleObjectResult<T>(ServiceResult<T> result, string id = "")
        {
            switch (result.Status)
            {
                case 200:
                    return Ok(result.Value);
                case 201:
                    return CreatedAtAction(nameof(Get), new { id = id }, result.Value);
                default:
                    return StatusCode(result.Status, result.Details);
            }
        }
    }
}
