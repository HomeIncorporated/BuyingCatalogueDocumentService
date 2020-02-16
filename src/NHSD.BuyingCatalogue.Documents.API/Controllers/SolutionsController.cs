using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NHSD.BuyingCatalogue.Documents.API.Repositories;

namespace NHSD.BuyingCatalogue.Documents.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    [AllowAnonymous]
    public sealed class SolutionsController : ControllerBase
    {
        private readonly IDocumentRepository documentRepository;
        private readonly ILogger logger;

        [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter", Justification = "MS DI requires closed-generic ILogger type")]
        public SolutionsController(
            IDocumentRepository documentRepository,
            ILogger<SolutionsController> logger)
        {
            this.documentRepository = documentRepository;
            this.logger = logger;
        }

        [HttpGet]
        [Route("{id}/documents/{name}")]
        [Produces("application/octet-stream", "application/json")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status206PartialContent)]
        [ProducesErrorResponseType(typeof(void))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DownloadAsync(string id, string name)
        {
            IDocument downloadInfo;

            try
            {
                downloadInfo = await documentRepository.DownloadAsync(id, name);
            }
            catch (DocumentRepositoryException e)
            {
                logger.LogError(e, null);
                return StatusCode(e.HttpStatusCode);
            }

            return File(downloadInfo.Content, downloadInfo.ContentType);
        }

        [HttpGet]
        [Route("{id}/documents")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<IAsyncEnumerable<string>> GetDocumentsBySolutionId(string id)
            => Ok(documentRepository.GetFileNamesAsync(id));
    }
}
