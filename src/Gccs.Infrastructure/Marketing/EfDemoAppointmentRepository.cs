using System.Data;
using Gccs.Application.Audit;
using Gccs.Application.Marketing;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gccs.Infrastructure.Marketing;

public sealed class EfDemoAppointmentRepository(
    GccsDbContext dbContext,
    IAuditRequestMetadata requestMetadata) : IDemoAppointmentRepository
{
    private const int MaximumSerializationAttempts = 3;

    public async Task<DemoAppointmentConfirmationWriteResult> ConfirmAsync(
        DemoAppointmentConfirmationCommand command,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await ConfirmOnceAsync(command, cancellationToken);
            }
            catch (Exception exception) when (HasPostgresState(exception, PostgresErrorCodes.UniqueViolation))
            {
                dbContext.ChangeTracker.Clear();
                return await ResolveConcurrentConflictAsync(command, cancellationToken);
            }
            catch (Exception exception) when (HasPostgresState(exception, PostgresErrorCodes.SerializationFailure))
            {
                dbContext.ChangeTracker.Clear();
                if (attempt >= MaximumSerializationAttempts) throw;
                await Task.Delay(TimeSpan.FromMilliseconds(25 * attempt), cancellationToken);
            }
        }
    }

    private async Task<DemoAppointmentConfirmationWriteResult> ConfirmOnceAsync(
        DemoAppointmentConfirmationCommand command,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        if (!await dbContext.DemoRequests.AsNoTracking()
            .AnyAsync(request => request.Id == command.DemoRequestId, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return new DemoAppointmentConfirmationWriteResult(
                DemoAppointmentConfirmationDisposition.DemoRequestNotFound);
        }

        if (await dbContext.DemoAppointments.AsNoTracking()
            .AnyAsync(appointment => appointment.DemoRequestId == command.DemoRequestId, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return new DemoAppointmentConfirmationWriteResult(
                DemoAppointmentConfirmationDisposition.AlreadyConfirmed);
        }

        if (await HasHostConflictAsync(command, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return new DemoAppointmentConfirmationWriteResult(
                DemoAppointmentConfirmationDisposition.HostConflict);
        }

        dbContext.DemoAppointments.Add(new DemoAppointmentEntity
        {
            Id = command.AppointmentId,
            DemoRequestId = command.DemoRequestId,
            Status = DemoAppointmentCatalog.Confirmed,
            ConfirmedStartAt = command.ConfirmedStartAt,
            ConfirmedEndAt = command.ConfirmedEndAt,
            ConfirmedTimeZone = command.TimeZone,
            DurationMinutes = command.DurationMinutes,
            HostUserId = command.HostUserId,
            MeetingMethod = command.MeetingMethod,
            MeetingJoinUrl = command.MeetingJoinUrl,
            ConfirmedByUserId = command.HostUserId,
            ConfirmedAt = command.ConfirmedAt,
            UpdatedAt = command.ConfirmedAt
        });

        dbContext.DemoAppointmentEvents.Add(new DemoAppointmentEventEntity
        {
            Id = command.EventId,
            DemoAppointmentId = command.AppointmentId,
            DemoRequestId = command.DemoRequestId,
            EventType = "Confirmed",
            PreviousStatus = "Requested",
            NewStatus = DemoAppointmentCatalog.Confirmed,
            ConfirmedStartAt = command.ConfirmedStartAt,
            ConfirmedEndAt = command.ConfirmedEndAt,
            ConfirmedTimeZone = command.TimeZone,
            DurationMinutes = command.DurationMinutes,
            HostUserId = command.HostUserId,
            MeetingMethod = command.MeetingMethod,
            MeetingJoinUrl = command.MeetingJoinUrl,
            ActorUserId = command.HostUserId,
            OccurredAt = command.ConfirmedAt,
            IpAddress = Truncate(requestMetadata.IpAddress, 120),
            UserAgent = Truncate(requestMetadata.UserAgent, 500),
            CorrelationId = Truncate(requestMetadata.CorrelationId, 120)
        });

        dbContext.DemoRequestDeliveries.Add(new DemoRequestDeliveryEntity
        {
            Id = Guid.NewGuid(),
            DemoRequestId = command.DemoRequestId,
            DemoAppointmentEventId = command.EventId,
            DeliveryKind = $"AppointmentConfirmed:{command.EventId:N}",
            Status = "Queued",
            RequestedByUserId = command.HostUserId,
            CreatedAt = command.ConfirmedAt,
            UpdatedAt = command.ConfirmedAt
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new DemoAppointmentConfirmationWriteResult(
            DemoAppointmentConfirmationDisposition.Confirmed,
            command.AppointmentId);
    }

    private Task<bool> HasHostConflictAsync(
        DemoAppointmentConfirmationCommand command,
        CancellationToken cancellationToken) =>
        dbContext.DemoAppointments.AsNoTracking().AnyAsync(
            appointment => appointment.HostUserId == command.HostUserId &&
                appointment.Status == DemoAppointmentCatalog.Confirmed &&
                appointment.ConfirmedStartAt < command.ConfirmedEndAt &&
                appointment.ConfirmedEndAt > command.ConfirmedStartAt,
            cancellationToken);

    private async Task<DemoAppointmentConfirmationWriteResult> ResolveConcurrentConflictAsync(
        DemoAppointmentConfirmationCommand command,
        CancellationToken cancellationToken)
    {
        if (await dbContext.DemoAppointments.AsNoTracking()
            .AnyAsync(appointment => appointment.DemoRequestId == command.DemoRequestId, cancellationToken))
        {
            return new DemoAppointmentConfirmationWriteResult(
                DemoAppointmentConfirmationDisposition.AlreadyConfirmed);
        }

        if (await HasHostConflictAsync(command, cancellationToken))
        {
            return new DemoAppointmentConfirmationWriteResult(
                DemoAppointmentConfirmationDisposition.HostConflict);
        }

        throw new InvalidOperationException("The appointment could not be confirmed because concurrent scheduling state changed.");
    }

    private static string Truncate(string value, int maximumLength) =>
        value.Length <= maximumLength ? value : value[..maximumLength];

    private static bool HasPostgresState(Exception exception, params string[] states)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgresException && states.Contains(postgresException.SqlState, StringComparer.Ordinal))
                return true;
        }

        return false;
    }
}
