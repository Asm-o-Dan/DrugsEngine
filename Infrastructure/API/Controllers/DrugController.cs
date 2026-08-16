using Application.DTOs.DrugDTOs;
using Application.UseCases.Commands.DrugCommands;
using Application.UseCases.Queries.DrugQueries;
using Domain.Entities;
using Infrastructure.Dal.Repositories.CountryRepositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace Infrastructure.API.Controllers
{
    [Route("api/Drug")]
    [ApiController]
    public class DrugController : ODataController
    {
        private readonly IMediator _mediator;

        public DrugController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: api/Drugs
        // Поддержка OData-запросов (фильтрация, сортировка, пагинация)
        [EnableQuery]
        public async Task<IActionResult> Get([FromQuery]ODataQueryOptions<Drug> queryOptions)
        {
            var query = new GetDrugQueryableQuery(queryOptions); // запрос из Application слоя
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        // GET: api/Drugs/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetDrugByIdQuery(id);
            var result = await _mediator.Send(query);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        // POST: api/Drugs
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CreateDrugRequest value)
        {
            if (value == null)
                return BadRequest("Тело запроса не может быть пустым.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            Drug drug = new Drug(value.Name, value.Manufacturer, value.CountryCode,
                new Country(value.CountryName, value.CountryCode));
            CreateDrugCommand command = new CreateDrugCommand(drug);

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        // PUT: api/Drugs/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(Guid id, [FromBody] UpdateDrugRequest value)
        {
            if (value == null)
                return BadRequest("Тело запроса не может быть пустым.");

            var command = new UpdateDrugCommand(
                id,
                value.Name,
                value.Manufacturer,
                value.CountryCode,
                value.CountryName
            );

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        // DELETE: api/Drugs/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var command = new DeleteDrugCommand(id);
            await _mediator.Send(command);
            return NoContent();
        }
    }
}
