using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;

namespace AuthApi.Models.Converters;

public class EnumStringConverter<TEnum> : IPropertyConverter where TEnum : struct, Enum
{
    public DynamoDBEntry ToEntry(object value)
    {
        return value is TEnum enumValue ? new Primitive(enumValue.ToString()) : null;
    }

    public object FromEntry(DynamoDBEntry entry)
    {
        if (entry is not Primitive primitive || string.IsNullOrEmpty(primitive.AsString())) return default(TEnum);

        return Enum.TryParse(primitive.AsString(), out TEnum result)
            ? result
            : throw new FormatException($"Invalid enum format: {primitive.AsString()}");
    }
}