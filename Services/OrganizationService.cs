// Services/OrganizationService.cs (Minor updates)
using Ideku.Data.Repositories;
using Ideku.Models.Entities;

namespace Ideku.Services
{
    public class OrganizationService
    {
        private readonly DepartmentRepository _departmentRepository;
        private readonly DivisionRepository _divisionRepository;
        private readonly CategoryRepository _categoryRepository;
        private readonly EventRepository _eventRepository;

        public OrganizationService(
            DepartmentRepository departmentRepository,
            DivisionRepository divisionRepository,
            CategoryRepository categoryRepository,
            EventRepository eventRepository)
        {
            _departmentRepository = departmentRepository;
            _divisionRepository = divisionRepository;
            _categoryRepository = categoryRepository;
            _eventRepository = eventRepository;
        }

        public async Task<List<Departement>> GetDepartmentsByDivisionIdAsync(string divisionId)
        {
            return await _departmentRepository.GetByDivisionIdAsync(divisionId);
        }

        public async Task<List<Divisi>> GetAllDivisionsAsync()
        {
            return await _divisionRepository.GetAllAsync();
        }

        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            return await _categoryRepository.GetAllAsync();
        }

        public async Task<List<Event>> GetAllEventsAsync()
        {
            return await _eventRepository.GetAllAsync();
        }

        public async Task<Divisi?> GetDivisionByIdAsync(string id)
        {
            return await _divisionRepository.GetByIdAsync(id);
        }

        public async Task<Departement?> GetDepartmentByIdAsync(string id)
        {
            return await _departmentRepository.GetByIdAsync(id);
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            return await _categoryRepository.GetByIdAsync(id);
        }

        public async Task<Event?> GetEventByIdAsync(int id)
        {
            return await _eventRepository.GetByIdAsync(id);
        }

        public async Task<List<Departement>> GetAllDepartmentsAsync()
        {
            return await _departmentRepository.GetAllAsync();
        }
    }
}