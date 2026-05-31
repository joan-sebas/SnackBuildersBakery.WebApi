using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api.IntegrationTests;

internal static class TestJsonOptions
{
    // Matches the API's ConfigureHttpJsonOptions — enums serialized as strings.
    internal static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
