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
    public async Task CreateBookAsync_WithInvalidBookInput_ThrowsArgumentException(string? title, string? author, string reason)
    {
        // GIVEN an invalid BookInput object
        var invalidBookInput = new BookInput
        {
            Title = $"{title}",
            Author = $"{author}",
            Synopsis = $"{reason}"
        };

        // WHEN the service method is called
        // THEN an ArgumentException should be thrown
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _bookService.CreateBookAsync(invalidBookInput)
        );

        // AND an exception message should be present
        Assert.Contains("cannot be null or blank", exception.Message);
    }


    [Fact]
    public async Task UpdateBookAsync_WhenBookExists_UpdatesAndSavesBook()
    {
        // GIVEN a valid book ID and an updated BookInput DTO
        var bookId = Guid.NewGuid();
        var bookInput = new BookInput
        {
            Title = "Modified Book",
            Author = "Modified Author",
            Synopsis = "Modified Synopsis"
        };

        // AND a repository that will return an existing book for that ID
        var existingBook = new Book("Old Title", "Old Author", "Old Synopsis") { Id = bookId };
        _mockBookRepository
            .Setup(repo => repo.GetByIdAsync(bookId))
            .ReturnsAsync(existingBook);

        // WHEN the service method is called
        var result = await _bookService.UpdateBookAsync(bookInput, bookId);

        // THEN the repository's UpdateAsync method should be called once
        _mockBookRepository.Verify(repo => repo.UpdateAsync(It.Is<Book>(b =>
            b.Id == bookId &&
            b.Title == "Modified Book" &&
            b.Author == "Modified Author"
        )), Times.Once);

        // AND the method should return the updated book object
        Assert.NotNull(result);
        Assert.Equal("Modified Book", result.Title);
    }

    [Fact]
    public async Task DeleteBookAsync_WhenBookExists_CallsRepositoryDelete()
    {
        // GIVEN a valid book ID
        var bookId = Guid.NewGuid();

        // AND a mock repository that will report a successful deletion (returning true)
        _mockBookRepository
            .Setup(repo => repo.DeleteAsync(bookId))
            .ReturnsAsync(true);

        // WHEN the service method is called
        await _bookService.DeleteBookAsync(bookId);

        // THEN no exception should have been thrown (implicit)
        // AND the repository method should have been called once
        _mockBookRepository.Verify(repo => repo.DeleteAsync(bookId), Times.Once);
    }

    [Fact]
    public async Task DeleteBookAsync_WhenBookDoesNotExist_ThrowsBookNotFoundException()
    {
        // GIVEN a non-existent book ID
        var nonExistentId = Guid.NewGuid();

        // AND a mock repository that will report an unsuccessful deletion (returning false)
        _mockBookRepository
            .Setup(repo => repo.DeleteAsync(nonExistentId))
            .ReturnsAsync(false);

        // WHEN the service method is called
        // THEN a BookNotFoundException should be thrown
        await Assert.ThrowsAsync<BookNotFoundException>(
            () => _bookService.DeleteBookAsync(nonExistentId)
        );
    }

    [Fact]
    public async Task GetBooksAsync_WithValidParams_CallsRepositoryAndReturnsData()
    {
        // GIVEN valid offset and int parameters
        int offset = 5;
        int limit = 10;

        // AND a mock repository that will return a tuple of a list of books and a totalcount
        var expectedBooks = new List<Book> { new Book("Title", "Author", "Synopsis") };
        long expectedTotalCount = 25;
        var repositoryResult = (Books: expectedBooks, TotalCount: expectedTotalCount);
        _mockBookRepository
        .Setup(repo => repo.GetAllAsync(offset, limit))
        .ReturnsAsync(repositoryResult);

        // WHEN the service method is called
        var (resultBooks, resultTotalCount) = await _bookService.GetBooksAsync(offset, limit);

        // THEN the repository should have been called once
        _mockBookRepository.Verify(repo => repo.GetAllAsync(offset, limit), Times.Once);

        // AND the data returned by the service should match the data from the repository
        Assert.Equal(expectedBooks, resultBooks);
        Assert.Equal(expectedTotalCount, resultTotalCount);
    }
}