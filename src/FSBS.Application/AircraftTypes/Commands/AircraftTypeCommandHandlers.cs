using FSBS.Application.Common.Exceptions;
using FSBS.Domain.Entities;
using FSBS.Domain.Interfaces;
using FSBS.Shared.Simulators;
using MediatR;

namespace FSBS.Application.AircraftTypes.Commands;

public sealed class CreateAircraftTypeHandler(IAircraftTypeRepository repo)
    : IRequestHandler<CreateAircraftTypeCommand, AircraftTypeDto>
{
    public async Task<AircraftTypeDto> Handle(CreateAircraftTypeCommand request, CancellationToken ct)
    {
        var normalizedIcaoCode = request.IcaoCode.Trim().ToUpperInvariant();
        var normalizedName = request.Name.Trim();

        var existing = await repo.ListAllAsync(ct);
        if (existing.Any(a => string.Equals(a.IcaoCode, normalizedIcaoCode, StringComparison.OrdinalIgnoreCase)))
            throw new AircraftTypeAlreadyExistsException(normalizedIcaoCode);

        var entity = new AircraftType
        {
            Id = Guid.NewGuid(),
            IcaoCode = normalizedIcaoCode,
            Name = normalizedName,
            IsActive = true,
        };

        await repo.AddAsync(entity, ct);
        return new AircraftTypeDto(entity.Id, entity.IcaoCode, entity.Name, entity.IsActive);
    }
}

public sealed class UpdateAircraftTypeHandler(IAircraftTypeRepository repo)
    : IRequestHandler<UpdateAircraftTypeCommand, AircraftTypeDto>
{
    public async Task<AircraftTypeDto> Handle(UpdateAircraftTypeCommand request, CancellationToken ct)
    {
        var entity = await repo.FindByIdAsync(request.AircraftTypeId, ct)
            ?? throw new AircraftTypeNotFoundException(request.AircraftTypeId);

        var normalizedIcaoCode = request.IcaoCode.Trim().ToUpperInvariant();
        var normalizedName = request.Name.Trim();

        var existing = await repo.ListAllAsync(ct);
        if (existing.Any(a => a.Id != request.AircraftTypeId
                              && string.Equals(a.IcaoCode, normalizedIcaoCode, StringComparison.OrdinalIgnoreCase)))
            throw new AircraftTypeAlreadyExistsException(normalizedIcaoCode);

        entity.IcaoCode = normalizedIcaoCode;
        entity.Name = normalizedName;
        entity.IsActive = request.IsActive;

        return new AircraftTypeDto(entity.Id, entity.IcaoCode, entity.Name, entity.IsActive);
    }
}

public sealed class DeleteAircraftTypeHandler(IAircraftTypeRepository repo)
    : IRequestHandler<DeleteAircraftTypeCommand, Unit>
{
    public async Task<Unit> Handle(DeleteAircraftTypeCommand request, CancellationToken ct)
    {
        var entity = await repo.FindByIdAsync(request.AircraftTypeId, ct)
            ?? throw new AircraftTypeNotFoundException(request.AircraftTypeId);

        entity.IsDeleted = true;
        return Unit.Value;
    }
}

