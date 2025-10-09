using Microsoft.AspNetCore.Mvc;
using BookApi.NET.Controllers.Generated;
using BookApi.NET.DTOs;
using System;
using System.Threading.Tasks;

namespace BookApi.NET.Controllers;

[ApiController]
public class BooksController : BooksControllerBase
{
        public override Task<ActionResult<BookOutput>> AddBook(BookInput body)
    {
        throw new NotImplementedException();
    }

    public override Task<ActionResult> DeleteBookById(Guid bookId)
    {
        throw new NotImplementedException();
    }

    public override Task<ActionResult<BookListResponse>> GetAllBooks(int? offset, int? limit)
    {
        throw new NotImplementedException();
    }

    public override Task<ActionResult<BookOutput>> GetBookById(Guid bookId)
    {
        throw new NotImplementedException();
    }

    public override Task<ActionResult<BookOutput>> UpdateBook(Guid bookId, BookInput body)
    {
        throw new NotImplementedException();
    }
}