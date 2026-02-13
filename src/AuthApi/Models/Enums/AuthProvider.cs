using System.Text.Json.Serialization;

namespace AuthApi.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AuthProvider
{
    Password
    //Google,
    //GitHub,
}