using Chronofoil.Common;
using Microsoft.AspNetCore.Mvc;

namespace Chronofoil.Web.Tests;

public static class TestHelpers
{
    public static ApiResult ToApiResult(this ActionResult<ApiResult> value)
    {
        var result = (value.Result as ObjectResult)?.Value as ApiResult;
        return result!;
    }

    public static ApiResult<T> ToApiResult<T>(this ActionResult<ApiResult<T>> value) where T : class
    {
        var result = (value.Result as ObjectResult)?.Value as ApiResult<T>;
        return result!;
    }
}