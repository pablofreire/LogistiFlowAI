using System.Data;
using Dapper;

namespace LogistiFlow.BuildingBlocks.Infrastructure.Persistence.TypeHandlers;

/// <summary>
/// Npgsql materializes <c>timestamptz</c> as a UTC <see cref="DateTime"/>; the domain uses
/// <see cref="DateTimeOffset"/>. This handler converts on read and passes UTC offsets on write.
/// </summary>
internal sealed class DateTimeOffsetTypeHandler : SqlMapper.TypeHandler<DateTimeOffset>
{
    public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
    {
        parameter.Value = value.ToUniversalTime();
    }

    public override DateTimeOffset Parse(object value) => value switch
    {
        DateTimeOffset dateTimeOffset => dateTimeOffset,
        DateTime dateTime => new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)),
        _ => throw new DataException($"Cannot convert {value.GetType().Name} to DateTimeOffset."),
    };
}
