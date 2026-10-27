using System.Text.Json;
using System.Text.Json.Serialization;
using DialDen.RLS.Abstractions.Postings;

namespace DialDen.RLS.UI;

internal static class ApiJsonSerializerOptions
{
    public static JsonSerializerOptions Default { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter<PostingStatus>() }
    };
}
