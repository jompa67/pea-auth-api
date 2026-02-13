using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;

namespace AuthApi.Models.Converters;

public class Iso8601DynamoDbConverter : IPropertyConverter
{
    private const string Format = "O";

    public DynamoDBEntry ToEntry(object value)
    {
        return value is DateTimeOffset dateTimeOffset
            ? new Primitive(dateTimeOffset.ToString("O"))
            : null;
    }

    public object FromEntry(DynamoDBEntry entry)
    {
        if (entry is not Primitive primitive || string.IsNullOrEmpty(primitive.AsString())) return null;

        if (DateTimeOffset.TryParse(primitive.AsString(), out var result)) return result;

        throw new FormatException($"Invalid DateTimeOffset format: {primitive.AsString()}");
    }
}