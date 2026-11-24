using Application.Interfaces;
using Application.Interfaces.Repositories.DrugRepositories;
using Application.UseCases.Commands.CountryCommands;
using Application.UseCases.Queries.CountryQueries;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Commands.DrugCommands;

public class CreateOrGetDrugCommandHandler : IRequestHandler<CreateOrGetDrugCommand, Guid>
{
    private readonly IDrugReadRepository _drugReadRepository;
    private readonly IDrugWriteRepository _drugWriteRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<CreateOrGetDrugCommandHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaProducer _kafkaProducer;

    public CreateOrGetDrugCommandHandler(
        IDrugReadRepository drugReadRepository,
        IDrugWriteRepository drugWriteRepository,
        IMediator mediator,
        IKafkaProducer kafkaProducer,
        ILogger<CreateOrGetDrugCommandHandler> logger, IUnitOfWork unitOfWork)
    {
        _drugReadRepository = drugReadRepository;
        _drugWriteRepository = drugWriteRepository;
        _mediator = mediator;
        _kafkaProducer = kafkaProducer;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateOrGetDrugCommand request, CancellationToken cancellationToken)
    {
        var drug = request.Drug;

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Повторная проверка перед вставкой (защита от гонки потоков)
            var existingDrug = await _drugReadRepository.GetByNameAndManufacturerAsync(drug.Name, drug.Manufacturer, cancellationToken);
            if (existingDrug != null)
            {
                // Источник мог начать указывать форму или действующее вещество — дополняем.
                if (existingDrug.SetDetails(drug.Form, drug.ActiveIngredient))
                {
                    await _drugWriteRepository.UpdateAsync(existingDrug, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                }
                _logger.LogDebug("Используем существующее лекарство: {DrugName}", existingDrug.Name);
                return existingDrug.Id;
            }

            _logger.LogDebug("Лекарство {DrugName} не найдено. Добавляем новое.", drug.Name);

            // Проверяем страну
            
            drug.CountryCodeId = drug.Country is null
                ? null
                : await _mediator.Send(new CreateOrGetCountryCommand(drug.Country), cancellationToken);
            drug.Country = null;
            
            await _drugWriteRepository.AddAsync(drug, cancellationToken);
            
            await _unitOfWork.SaveChangesAsync(cancellationToken); // Гарантируем установку ID
            await transaction.CommitAsync(cancellationToken);
            try
            {
                _kafkaProducer.ProduceDrug(drug);
                _logger.LogDebug("Сообщение для DrugItem {DrugName} отправлено в Kafka", drug.Name);
            }
            catch (Exception kafkaEx)
            {
                _logger.LogError(kafkaEx, "Ошибка при отправке сообщения в Kafka для DrugItem {DrugName}", drug.Name);
            }
            return drug.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при добавлении лекарства {DrugName}", drug.Name);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}