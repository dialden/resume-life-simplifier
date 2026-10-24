using Microsoft.AspNetCore.Mvc;

namespace DialDen.RLS.Tests.Core.Drivers.RLS;

public static class ValidationProblemDetailsExtensions
{
    public static SystemError MapError(this ValidationProblemDetails validationProblemDetails)
    {
        var message = (validationProblemDetails.Title ?? string.Empty) + (validationProblemDetails.Detail ?? string.Empty);
        if (validationProblemDetails.Errors != null && validationProblemDetails.Errors.Count > 0)
        {
            var fieldErrors = validationProblemDetails.Errors
                .SelectMany(e => (e.Value ?? [string.Empty])
                    .Select(fieldMessage => new SystemError.FieldError(e.Key ?? "unknown", fieldMessage)))
                .ToList();
            return SystemError.Of(message, fieldErrors.AsReadOnly());
        }
        return SystemError.Of(message);
    }
}
