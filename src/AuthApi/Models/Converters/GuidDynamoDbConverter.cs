using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;

namespace AuthApi.Models.Converters;

public class GuidDynamoDbConverter : IPropertyConverter
{
    public DynamoDBEntry ToEntry(object value)
    {
        return value is Guid guid ? new Primitive(guid.ToString()) : null;
    }

    public object FromEntry(DynamoDBEntry entry)
    {
        if (entry is Primitive primitive && Guid.TryParse(primitive.AsString(), out var guid)) return guid;

        throw new InvalidOperationException("Invalid Guid format in DynamoDB.");
    }
}