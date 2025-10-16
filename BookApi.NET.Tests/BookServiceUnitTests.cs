using BookApi.NET.Models;
using BookApi.NET.Services;
using Moq;
using Xunit;
using System;
using System.Threading.Tasks;
using BookApi.NET.Controllers.Generated;

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

    [Fact]
    public async Task CreateBookAsync_WithValidBookInput_CreatesAndReturnsBook()
    {
        // GIVEN a valid BookInput object
        BookInput bookInput = new BookInput
        {
            Title = "Test Book",
            Author = "Test Author",
            Synopsis = "Test Synopsis"
        };

        // AND a variable to capture the created Book
        Book? createdBook = null;

        // AND a mock Book Repository that will both capture the Book sent to it and return the same Book
        _mockBookRepository
            .Setup(repo => repo.CreateAsync(It.IsAny<Book>()))
            .Callback<Book>(book => createdBook = book)
            .Returns(Task.CompletedTask);

        // WHEN the service method is called
        var result = await _bookService.CreateBookAsync(bookInput);

        // THEN the service should transform the BookInput to a Book
        Assert.NotNull(createdBook);
        Assert.Equal(createdBook.Title, result.Title);
        Assert.NotEqual(Guid.Empty, createdBook.Id);

        // AND the service should call the repository once
        _mockBookRepository.Verify(repo => repo.CreateAsync(It.IsAny<Book>()), Times.Once);

        // AND the service should return the Book object created
        Assert.NotNull(result);
        Assert.Equal(createdBook.Id, result.Id);
    }

    [Theory]
    [InlineData(null, "Valid Author", "Should fail with null title")]
    [InlineData("", "Valid Author", "Should fail with empty title")]
    [InlineData("   ", "Valid Author", "Should fail with whitespace title")]
    [InlineData("Valid Title", null, "Should fail with null author")]
    [InlineData("Valid Title", "", "Should fail with empty author")]
    [InlineData("Valid Title", "   ", "Should fail with whitespace author")]
    public async Task CreateBookAsync_WithInvalidBookInput_ThrowsArgumentException(string title, string author, string reason)
    {
        // GIVEN an invalid BookInput object
        var invalidBookInput = new BookInput
        {
            Title = $"{title}",
            Author = $"{author}",
            Synopsis = "Test Synopsis"
        };

        // WHEN the service method is called
        // THEN an ArgumentException should be thrown
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _bookService.CreateBookAsync(invalidBookInput)
        );

        // AND an exception message should be present
        Assert.Contains("cannot be null or blank", exception.Message);
    }
}