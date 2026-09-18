using System.ComponentModel;
using System.Reflection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Fluently.API.Filters;

/// <summary>
/// Valores numéricos e descrições dos enums no Swagger.
/// </summary>
public sealed class EnumSchemaFilter : ISchemaFilter
{
    /// <summary>
    /// Complementa o esquema de um enum com seus valores aceitos.
    /// </summary>
    /// <param name="schema">Esquema que será complementado.</param>
    /// <param name="context">Contexto utilizado na geração do esquema.</param>
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        var enumType = Nullable.GetUnderlyingType(context.Type) ?? context.Type;

        if (!enumType.IsEnum)
        {
            return;
        }

        var descriptions = Enum.GetValues(enumType).Cast<object>().Select(value => FormatValue(enumType, value));
        var acceptedValues = string.Join("<br />", descriptions);
        var prefix = string.IsNullOrWhiteSpace(schema.Description) ? string.Empty : $"{schema.Description}<br /><br />";

        schema.Description = $"{prefix}Valores aceitos:<br />{acceptedValues}";
    }

    /// <summary>
    /// Formata um valor do enum para exibição na documentação.
    /// </summary>
    /// <param name="enumType">Tipo do enum.</param>
    /// <param name="value">Valor que será formatado.</param>
    /// <returns>Valor numérico acompanhado de sua descrição.</returns>
    private static string FormatValue(Type enumType, object value)
    {
        var name = Enum.GetName(enumType, value)!;
        var field = enumType.GetField(name)!;
        var description = field.GetCustomAttribute<DescriptionAttribute>()?.Description ?? name;

        return $"{Convert.ToInt32(value)} = {description}";
    }
}
