using Buyer.API.Controllers;
using Buyer.Application.Common.Addresses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Buyer.API.Filters;

public sealed class AddressExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not AddressException error) return;
        var status = error.Failure switch
        {
            AddressFailure.Invalid => 400,
            AddressFailure.NotFound => 404,
            AddressFailure.Unauthorized => 401,
            AddressFailure.Forbidden => 403,
            AddressFailure.Conflict => 409,
            AddressFailure.Unavailable => 503,
            _ => 500
        };
        context.Result = new ObjectResult(ApiEnvelope<object>.Failure(error.Code, error.Message, error.Field)) { StatusCode = status };
        context.ExceptionHandled = true;
    }
}
