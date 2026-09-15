using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fluently.API.Helpers;

/// <summary>
/// Remoção de espaços externos em textos JSON.
/// </summary>
public sealed class TrimStringJsonConverterHelper : JsonConverter<string>
{
    /// <summary>
    /// Lê e normaliza um valor textual do conteúdo JSON.
    /// </summary>
    /// <param name="reader">Leitor posicionado no valor JSON.</param>
    /// <param name="typeToConvert">Tipo que será convertido.</param>
    /// <param name="options">Opções utilizadas na desserialização.</param>
    /// <returns>Texto sem espaços externos ou valor nulo.</returns>
    public override string? Read(ref Utf8JsonReader reader,
                                 Type typeToConvert,
                                 JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null
            ? null
            : reader.GetString()?.Trim();

    /// <summary>
    /// Escreve um valor textual no conteúdo JSON.
    /// </summary>
    /// <param name="writer">Escritor utilizado na serialização.</param>
    /// <param name="value">Texto que será serializado.</param>
    /// <param name="options">Opções utilizadas na serialização.</param>
    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
