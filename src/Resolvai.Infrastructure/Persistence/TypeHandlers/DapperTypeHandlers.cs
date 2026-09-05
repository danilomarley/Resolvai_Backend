using Dapper;

namespace Resolvai.Infrastructure.Persistence.TypeHandlers;

public static class DapperTypeHandlers
{
    private static bool _registered;

    /// <summary>
    /// O Dapper mantém os handlers em estado estático global, então o registro é feito uma única vez.
    /// </summary>
    public static void Register()
    {
        if (_registered)
        {
            return;
        }

        SqlMapper.AddTypeHandler(new EmailTypeHandler());
        _registered = true;
    }
}
