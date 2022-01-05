using AutoMapper;
using CompanyEmployees.ActionFilters;
using Contracts;
using Entities.DTO;
using Entities.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CompanyEmployees.Controllers
{
    [Route("api/companies/{companyId}/employees")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly IRepositoryManager _repository;
        private readonly ILoggerManager _logger;
        private readonly IMapper _mapper;
        public EmployeesController(IRepositoryManager repository, ILoggerManager logger, IMapper mapper)
        {
            _repository = repository;
            _logger = logger;
            _mapper = mapper;
        }
        [HttpGet]
        public async Task<IActionResult> GetEmployeesForCompany(Guid companyId,[FromQuery] EmployeeParameters employeeParameters)
        {
            if (!employeeParameters.ValidAgeRange)
                return BadRequest("Max age can't be less than min age.");
            var company = await _repository.Company.GetCompany(companyId, trackChanges: false);
            if (company == null)
            {
                _logger.LogInfo($"Company with id: {companyId} doesn't exist in the database.");
                return NotFound();
            }
            var employeesFromDb = await _repository.Employee.GetEmployees(companyId,employeeParameters, trackChanges: false);
            
            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(employeesFromDb.MetaData));

            var employeesDto = _mapper.Map<IEnumerable<EmployeeDto>>(employeesFromDb);
            return Ok(employeesDto);
        }
        [HttpGet("{id}", Name = "GetEmployeeForCompany")]
        public async Task<IActionResult> GetEmployeeForCompany(Guid companyId,Guid id)
        {
            var company = await _repository.Company.GetCompany(companyId, false);
            if(company == null)
            {
                _logger.LogInfo($"Company with id: {companyId} doesn't exist in the databse");
                return NotFound();
            }
            var employee = await _repository.Employee.GetEmployee(companyId, id, false);
            if (employee == null)
            {
                _logger.LogInfo($"Employee with id: {id} doesn't exist in the databse");
                return NotFound();
            }
            var employeeDto = _mapper.Map<EmployeeDto>(employee);
            return Ok(employeeDto);
        }
        [HttpPost]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public async Task<IActionResult> CreateEmployeeForCompany(Guid companyId,[FromBody]CreateEmployeeDto employeeDto)
        {
            var company = await _repository.Company.GetCompany(companyId, trackChanges: false);
            if (company == null)
            {
                _logger.LogInfo($"Company with id: {companyId} doesn't exist in the database.");
                return NotFound();
            }
            var employeeEntity = _mapper.Map<Employee>(employeeDto);

            _repository.Employee.CreateEmployeeForCompany(companyId, employeeEntity);
            await _repository.Save();

            var employeeToReturn = _mapper.Map<EmployeeDto>(employeeEntity);
            return CreatedAtRoute("GetEmployeeForCompany", new
            {
                companyId,
                id = employeeToReturn.Id
            }, employeeToReturn);
        }
        [HttpDelete("{id}")]
        [ServiceFilter(typeof(ValidateEmployeeForCompanyExistsAttribute))]
        public async Task<IActionResult> DeleteEmployeeForCompany(Guid companyId, Guid id)
        {
            var employeeForCompany = HttpContext.Items["employee"] as Employee;

            _repository.Employee.DeleteEmployee(employeeForCompany);
            await _repository.Save();
            return NoContent();
        }
        [HttpPut("{id}")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        [ServiceFilter(typeof(ValidateEmployeeForCompanyExistsAttribute))]
        public async Task<IActionResult> UpdateEmployeeForCompany(Guid companyId, Guid id, [FromBody] UpdateEmployeeDto employeeDto)
        {
            var employeeEntity = HttpContext.Items["employee"] as Employee;

            _mapper.Map(employeeDto, employeeEntity);
            await _repository.Save();
            return NoContent();
        }
        [HttpPatch("{id}")]
        [ServiceFilter(typeof(ValidateEmployeeForCompanyExistsAttribute))]
        public async Task<IActionResult> PartiallyUpdateEmployeeForCompany(Guid companyId, Guid id, 
            [FromBody] JsonPatchDocument<UpdateEmployeeDto> patchDoc)
        {
            if(patchDoc == null)
            {
                _logger.LogError("patchDoc object sent from client is null.");
                return BadRequest("patchDoc object is null");
            }
            var employeeEntity = HttpContext.Items["employee"] as Employee;

            var employeeToPatch = _mapper.Map<UpdateEmployeeDto>(employeeEntity);

            patchDoc.ApplyTo(employeeToPatch, ModelState);

            TryValidateModel(employeeToPatch);

            if (!ModelState.IsValid)
            {
                _logger.LogError("Invalid model state for the patch document");
                return UnprocessableEntity(ModelState);
            }
            _mapper.Map(employeeToPatch, employeeEntity);
            await _repository.Save();
            return NoContent();
        }
    }
}
