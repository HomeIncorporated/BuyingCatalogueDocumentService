using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NHSD.BuyingCatalogue.Documents.API.Controllers;
using NHSD.BuyingCatalogue.Documents.API.Repositories;
using NUnit.Framework;

namespace NHSD.BuyingCatalogue.Documents.API.UnitTests
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    internal sealed class SolutionsControllerTests
    {
        [Test]
        public async Task DownloadAsync_DocumentRepositoryException_IsLogged()
        {
            var exception = new DocumentRepositoryException(
                new InvalidOperationException(),
                StatusCodes.Status502BadGateway);

            var mockStorage = new Mock<IDocumentRepository>();
            mockStorage.Setup(s => s.DownloadAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(exception);

            var mockLogger = new Mock<ILogger<SolutionsController>>();
            mockLogger.Setup(GetLogExpression(exception)).Verifiable();

            var controller = new SolutionsController(mockStorage.Object, mockLogger.Object);

            await controller.DownloadAsync("ID", "directory");

            mockLogger.Verify();
        }

        [Test]
        public async Task DownloadAsync_DocumentRepositoryException_ReturnsStatusCodeResult()
        {
            var exception = new DocumentRepositoryException(
                new InvalidOperationException(),
                StatusCodes.Status502BadGateway);

            var mockStorage = new Mock<IDocumentRepository>();
            mockStorage.Setup(s => s.DownloadAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(exception);

            var controller = new SolutionsController(mockStorage.Object, Mock.Of<ILogger<SolutionsController>>());

            var result = await controller.DownloadAsync("ID", "directory") as StatusCodeResult;

            Assert.NotNull(result);
            result.StatusCode.Should().Be(exception.HttpStatusCode);
        }

        [Test]
        public void DownloadAsync_Exception_DoesNotSwallow()
        {
            var exception = new InvalidOperationException();

            var mockStorage = new Mock<IDocumentRepository>();
            mockStorage.Setup(s => s.DownloadAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(exception);

            var controller = new SolutionsController(mockStorage.Object, Mock.Of<ILogger<SolutionsController>>());

            Assert.ThrowsAsync<InvalidOperationException>(() => controller.DownloadAsync("ID", "directory"));
        }

        [Test]
        public async Task DownloadAsync_ReturnsFileStreamResult()
        {
            const string expectedContentType = "test/content-type";

            using var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes("Hello world!"));

            var downloadInfo = new Mock<IDocument>();
            downloadInfo.Setup(d => d.Content).Returns(expectedStream);
            downloadInfo.Setup(d => d.ContentType).Returns(expectedContentType);

            var mockStorage = new Mock<IDocumentRepository>();
            mockStorage.Setup(s => s.DownloadAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(downloadInfo.Object);

            var controller = new SolutionsController(mockStorage.Object, Mock.Of<ILogger<SolutionsController>>());

            var result = await controller.DownloadAsync("ID", "directory") as FileStreamResult;

            Assert.NotNull(result);
            result.FileStream.IsSameOrEqualTo(expectedStream);
            result.ContentType.Should().Be(expectedContentType);
        }

        [Test]
        public void GetFileNamesAsync_ReturnsStorageResult()
        {
            var mockEnumerable = new Mock<IAsyncEnumerable<string>>();

            var mockStorage = new Mock<IDocumentRepository>();
            mockStorage.Setup(s => s.GetFileNamesAsync(It.IsAny<string>()))
                .Returns(mockEnumerable.Object);

            var controller = new SolutionsController(mockStorage.Object, Mock.Of<ILogger<SolutionsController>>());
            var result = controller.GetDocumentsBySolutionId("Foobar");

            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = (OkObjectResult)result.Result;

            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().Be(mockEnumerable.Object);
            mockStorage.Verify(x => x.GetFileNamesAsync("Foobar"), Times.Once);
        }

        // This expression is necessary for mocking because we currently have a dependency
        // on ILogger<T> for logging.
        // Recommend that we refactor by defining our owning logging abstraction.
        private static Expression<Action<ILogger<SolutionsController>>> GetLogExpression(Exception ex) =>
            l => l.Log(
                It.Is<LogLevel>(ll => ll == LogLevel.Error),
                It.Is<EventId>(e => e == 0),
                It.Is<It.IsAnyType>((v, t) => true),
                It.Is<Exception>(e => e == ex),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true));
    }
}
