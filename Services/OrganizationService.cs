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
    }
}
