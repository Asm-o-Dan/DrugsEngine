using Application.Interfaces.Repositories.DrugStoreRepositories;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Commands.DrugStoreCommands;

/// <summary>
/// Находит аптеку по устойчивому ключу источника (телефону) и обновляет её данные,
/// либо создаёт новую с постоянным номером. Сохранение — на вызывающей стороне (Unit of Work).
/// </summary>
public class CreateOrGetDrugStoreCommandHandler(
    IDrugStoreReadRepository readRepository,
    IDrugStoreWriteRepository writeRepository,
    ILogger<CreateOrGetDrugStoreCommandHandler> logger) : IRequestHandler<CreateOrGetDrugStoreCommand, Guid>
{
    public async Task<Guid> Handle(CreateOrGetDrugStoreCommand request, CancellationToken cancellationToken)
    {
        var incoming = request.DrugStore ?? throw new ArgumentNullException(nameof(request.DrugStore));

        var existing = incoming.ExternalKey is { } key
            ? await readRepository.GetByExternalKeyAsync(incoming.DrugNetwork, key, cancellationToken)
            : await readRepository.GetByNumberAndNetworkAsync(incoming.Number, incoming.DrugNetwork, cancellationToken);

        if (existing != null)
        {
            if (existing.UpdateDetails(incoming.Address, incoming.PhoneNumber, incoming.Hours, incoming.Latitude, incoming.Longitude))
            {
                logger.LogInformation("Обновлены данные аптеки {Network} №{Number}", existing.DrugNetwork, existing.Number);
                await writeRepository.UpdateAsync(existing, cancellationToken);
            }
            return existing.Id;
        }

        // Номера у источника нет — назначаем следующий свободный в сети, дальше он не меняется.
        if (incoming.ExternalKey is not null || incoming.Number <= 0)
            incoming.AssignNumber(await readRepository.GetMaxNumberAsync(incoming.DrugNetwork, cancellationToken) + 1);

        logger.LogInformation("Новая аптека {Network} №{Number}: {City}, {Street} {House}",
            incoming.DrugNetwork, incoming.Number, incoming.Address.City, incoming.Address.Street, incoming.Address.House);
        await writeRepository.AddAsync(incoming, cancellationToken);
        return incoming.Id;
    }
}
