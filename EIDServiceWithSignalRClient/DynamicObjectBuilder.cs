namespace EIDServiceWithSignalRClient;

using NJsonSchema;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text.Json.Nodes;

public static class DynamicObjectBuilder
{
    /// <summary>
    /// Builds a dynamic object (ExpandoObject or List<ExpandoObject>) based on the provided JSON schema and data.
    /// </summary>
    /// <param name="schemaJson">A JSON string representing the schema.</param>
    /// <param name="dataJson">A JSON string representing the data.</param>
    /// <returns>A dynamic object or list of dynamic objects populated by the data.</returns>
    public static dynamic BuildDynamicObject(string schemaJson, string dataJson)
    {
        // Parse the JSON Schema using NJsonSchema
        var schema = JsonSchema.FromJsonAsync(schemaJson).Result;

        // Parse the JSON data into a JsonNode
        var dataToken = JsonNode.Parse(dataJson);

        // Determine if the data is a single object or a list of objects
        if (dataToken is JsonArray dataArray)
        {
            var dynamicList = new List<ExpandoObject>();

            foreach (var item in dataArray)
            {
                if (item is JsonObject itemObj)
                {
                    dynamic dynamicObj = CreateDynamicObjectFromSchema(schema, itemObj);
                    dynamicList.Add(dynamicObj);
                }
            }

            return dynamicList;
        }
        else if (dataToken is JsonObject dataObj)
        {
            return CreateDynamicObjectFromSchema(schema, dataObj);
        }
        else
        {
            throw new InvalidOperationException("Unsupported JSON data format. Expected object or array.");
        }
    }

    /// <summary>
    /// Creates a single dynamic object from the schema and data.
    /// </summary>
    /// <param name="schema">The JSON schema describing the structure.</param>
    /// <param name="dataObj">The JSON data containing values.</param>
    /// <returns>A dynamic object with properties populated by the data.</returns>
    private static ExpandoObject CreateDynamicObjectFromSchema(JsonSchema schema, JsonObject dataObj)
    {
        dynamic dynamicObj = new ExpandoObject();
        var dict = (IDictionary<string, object>)dynamicObj;

        // Iterate over the schema's property definitions
        foreach (var property in schema.Properties)
        {
            string propertyName = property.Key;

            // Check if the property exists in the data
            if (dataObj.TryGetPropertyValue(propertyName, out JsonNode? value))
            {
                // Dynamically assign the value from the data
                dict[propertyName] = value.GetValue<object>();
            }
            else
            {
                dict[propertyName] = null; // Assign null if the property is missing
            }
        }

        return dynamicObj;
    }
}
