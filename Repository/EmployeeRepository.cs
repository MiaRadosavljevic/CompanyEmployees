﻿using Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository
{
    public class EmployeeRepository : RepositoryBase<Employee>, IEmployeeRepository
    {
        public EmployeeRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }

        public void CreateEmployeeForCompany(Guid companyId, Employee employee)
        {
            employee.CompanyId = companyId;
            Create(employee);
        }
        public void DeleteEmployee(Employee employee) =>
            Delete(employee);
        public Employee GetEmployee(Guid companyId, Guid id, bool trackChanges) =>
            FindByCondition(c => c.CompanyId.Equals(companyId) && c.Id.Equals(id), trackChanges).SingleOrDefault();
        public IEnumerable<Employee> GetEmployees(Guid companyId, bool trackChanges) =>
            FindByCondition(c => c.CompanyId.Equals(companyId), trackChanges).OrderBy(e => e.Name);
    }
}
