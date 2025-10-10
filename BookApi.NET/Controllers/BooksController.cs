using Microsoft.AspNetCore.Mvc;
using BookApi.NET.Controllers.Generated;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BookApi.NET.Controllers;

[ApiController]
public class BooksController : BooksControllerBase
{
    public override Task BooksDelete([BindRequired] Guid bookId)
    {
        throw new NotImplementedException();
    }

    public override Task<BookOutput> BooksGet([BindRequired] Guid bookId)
    {
        throw new NotImplementedException();
    }

    public override Task<Generated.BookListResponse> BooksGet([FromQuery] int? offset = 0, [FromQuery] int? limit = 20)
    {
        throw new NotImplementedException();
    }

    public override Task<Generated.BookOutput> BooksPost([BindRequired, FromBody] Generated.BookInput body)
    {
        throw new NotImplementedException();
    }

    public override Task<Generated.BookOutput> BooksPut([BindRequired, FromBody] Generated.BookInput body, [BindRequired] Guid bookId)
    {
        throw new NotImplementedException();
    }
}