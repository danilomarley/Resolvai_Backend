using System.Data;
using Dapper;
using Resolvai.Domain.ValueObjects;

namespace Resolvai.Infrastructure.Persistence.TypeHandlers;

/// <summary>
/// Permite passar o value object <see cref="Email"/> direto como parâmetro do Dapper.
/// </summary>
public sealed class EmailTypeHandler : SqlMapper.TypeHandler<Email>
{
    public override Email Parse(object value) => Email.Create(value.ToString());

    public override void SetValue(IDbDataParameter parameter, Email? value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = value is null ? DBNull.Value : value.Value;
    }
}
