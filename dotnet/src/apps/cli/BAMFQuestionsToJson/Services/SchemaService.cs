using System.Text.Json;

namespace LanguageLearningTools.BAMFQuestionsToJson.Services;

/// <summary>
/// Service for handling JSON schema operations.
/// </summary>
public class SchemaService
{
    /// <summary>
    /// Cleans a JSON schema by removing additionalProperties fields and other adjustments 
    /// to make it compatible with AI service requirements.
    /// </summary>
    /// <typeparam name="T">The type to create a schema for.</typeparam>
    /// <returns>A cleaned JsonElement representing the schema.</returns>
    public JsonElement CreateAndCleanSchema<T>()
    {
        var schema = Microsoft.Extensions.AI.AIJsonUtilities.CreateJsonSchema(typeof(T));
        var schemaJson = JsonSerializer.Serialize(schema);
        var schemaDoc = JsonDocument.Parse(schemaJson);

        return CleanSchema(schemaDoc.RootElement);
    }

    /// <summary>
    /// Recursively cleans a schema JsonElement by removing additionalProperties.
    /// </summary>
    /// <param name="element">The JsonElement to clean.</param>
    /// <returns>A cleaned JsonElement.</returns>
    private static JsonElement CleanSchema(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return element;

        var mutableObj = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(element.ToString())!;
        mutableObj.Remove("additionalProperties");

        if (mutableObj.ContainsKey("properties"))
        {
            var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(mutableObj["properties"].ToString())!;
            var cleanedProperties = properties.ToDictionary(
                kvp => kvp.Key,
                kvp => CleanSchema(kvp.Value));
            mutableObj["properties"] = JsonSerializer.SerializeToElement(cleanedProperties);
        }

        return JsonSerializer.SerializeToElement(mutableObj);
    }
}