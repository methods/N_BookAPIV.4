using BookApi.NET.Models;
using BookApi.NET.Services;
using Moq;
using Xunit;
using System;
using System.Threading.Tasks;

namespace BookApi.NET.Tests;

public class BookServiceUnitTests
{
    private readonly Mock<IBookRepository> _mockBookRepository;

    private readonly BookService _bookService;

    public BookServiceUnitTests()
    {
        _mockBookRepository = new Mock<IBookRepository>();

        _bookService = new BookService(_mockBookRepository.Object);
    }

    [Fact]
    public async Task GetBookById_WhenBookExists_ReturnsBook()
    {
        // GIVEN a valid Book object
        var bookId = Guid.NewGuid();
        var mockBook = new Book("Test Book", "Test Author", "Test Synopsis") { Id = bookId };

        // AND a mock Book Repository that will return it when called
        _mockBookRepository
            .Setup(repo => repo.GetByIdAsync(bookId))
            .ReturnsAsync(mockBook);

        // WHEN the service method is called
        var result = await _bookService.GetBookByIdAsync(bookId);

        // THEN the service should call the repository and return the mock Book object
        Assert.NotNull(result);
        Assert.Equal(mockBook.Id, result.Id);
        Assert.Equal(mockBook.Title, result.Title);
    }

    [Fact]
    public async Task GetBookById_WhenBookDoesNotExist_ThrowsBookNotFoundException()
    {
        // GIVEN a correctly formatted but non-existent bookId
        var nonExistentBookId = Guid.NewGuid();

        // AND a mock Book Repository that returns null when called
        _mockBookRepository
            .Setup(repo => repo.GetByIdAsync(nonExistentBookId))
            .ReturnsAsync((Book?)null);

        // WHEN we call the service method
        // THEN a BookNotFoundException should be thrown
        var exception = await Assert.ThrowsAsync<BookNotFoundException>(
            () => _bookService.GetBookByIdAsync(nonExistentBookId)
        );

        // AND the correct error message should be sent
        Assert.Equal($"Book not found with id: {nonExistentBookId}", exception.Message);
    }
}