using GolfClub.Application.Interfaces;

namespace GolfClub.Infrastructure;

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime Now => DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
}
