namespace EIDServiceWithSignalRClient;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.Dynamic;

public static class DynamicObjectBuilder
{
    /// <summary>
    /// Builds a dynamic object (an ExpandoObject) whose properties are created
    /// by reading the provided JSON Schema and whose values are taken from the data JSON.
    /// </summary>
    /// <param name="schemaJson">A JSON string representing the schema.</param>
    /// <param name="dataJson">A JSON string representing the data.</param>
    /// <returns>A dynamic object with properties set from the data.</returns>
    public static dynamic BuildDynamicObject(string schemaJson, string dataJson)
    {
        // Parse the JSON Schema
        JSchema schema = JSchema.Parse(schemaJson);
        // Parse the JSON data to a JObject
        JObject dataObj = JObject.Parse(dataJson);

        // Use an ExpandoObject to build a dynamic object
        dynamic dynamicObj = new ExpandoObject();
        var dict = (IDictionary<string, object>)dynamicObj;

        // Iterate over each property defined in the schema
        foreach (var property in schema.Properties)
        {
            string propertyName = property.Key;
            JSchema propertySchema = property.Value;

            // Look for this property in the data object
            if (dataObj.TryGetValue(propertyName, out JToken token))
            {
                // Map the schema's type to a desired .NET type
                Type targetType = MapJSchemaTypeToDotNet(propertySchema.Type.Value);
                // Convert the JSON token to the proper .NET type
                object value = token.ToObject(targetType);
                dict[propertyName] = value;
            }
            else
            {
                // If the property is missing from the data, assign null
                dict[propertyName] = null;
            }
        }

        return dynamicObj;
    }

    /// <summary>
    /// Maps a JSchemaType to a corresponding .NET type.
    /// This is a basic mapping and can be extended for your needs.
    /// </summary>
    /// <param name="schemaType">The JSchemaType flag from the schema.</param>
    /// <returns>A .NET Type corresponding to the JSON Schema type.</returns>
    private static Type MapJSchemaTypeToDotNet(JSchemaType schemaType)
    {
        if (schemaType.HasFlag(JSchemaType.String))
            return typeof(string);
        if (schemaType.HasFlag(JSchemaType.Integer))
            return typeof(int);
        if (schemaType.HasFlag(JSchemaType.Number))
            return typeof(double);
        if (schemaType.HasFlag(JSchemaType.Boolean))
            return typeof(bool);
        if (schemaType.HasFlag(JSchemaType.Array))
            return typeof(JArray);
        if (schemaType.HasFlag(JSchemaType.Object))
            return typeof(JObject);

        // Fallback to object if none of the above match.
        return typeof(object);
    }
}
