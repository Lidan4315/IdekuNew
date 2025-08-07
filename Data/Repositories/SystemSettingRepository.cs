// Data/Repositories/SystemSettingRepository.cs (NEW)
using Microsoft.EntityFrameworkCore;
using Ideku.Data.Context;
using Ideku.Models.Entities;

namespace Ideku.Data.Repositories
{
    public class SystemSettingRepository
    {
        private readonly AppDbContext _context;

        public SystemSettingRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<SystemSetting?> GetByKeyAsync(string key)
        {
            return await _context.SystemSettings
                .Include(ss => ss.UpdatedByUser)
                    .ThenInclude(u => u.Employee)
                .FirstOrDefaultAsync(ss => ss.SettingKey == key);
        }

        public async Task<List<SystemSetting>> GetAllAsync()
        {
            return await _context.SystemSettings
                .Include(ss => ss.UpdatedByUser)
                    .ThenInclude(u => u.Employee)
                .OrderBy(ss => ss.SettingKey)
                .ToListAsync();
        }

        public async Task<T?> GetValueAsync<T>(string key)
        {
            var setting = await GetByKeyAsync(key);
            if (setting == null) return default(T);

            try
            {
                return setting.DataType switch
                {
                    "NUMBER" => (T)Convert.ChangeType(setting.SettingValue, typeof(T)),
                    "BOOLEAN" => (T)Convert.ChangeType(bool.Parse(setting.SettingValue), typeof(T)),
                    "STRING" => (T)Convert.ChangeType(setting.SettingValue, typeof(T)),
                    _ => (T)Convert.ChangeType(setting.SettingValue, typeof(T))
                };
            }
            catch
            {
                return default(T);
            }
        }

        public async Task SetValueAsync<T>(string key, T value, long updatedByUserId, string? description = null)
        {
            var setting = await GetByKeyAsync(key);
            var dataType = GetDataType<T>();

            if (setting == null)
            {
                setting = new SystemSetting
                {
                    SettingKey = key,
                    SettingValue = value?.ToString() ?? "",
                    DataType = dataType,
                    Description = description,
                    UpdatedByUserId = updatedByUserId
                };
                _context.SystemSettings.Add(setting);
            }
            else
            {
                setting.SettingValue = value?.ToString() ?? "";
                setting.DataType = dataType;
                setting.UpdatedByUserId = updatedByUserId;
                if (!string.IsNullOrEmpty(description))
                    setting.Description = description;
            }

            await _context.SaveChangesAsync();
        }

        private string GetDataType<T>()
        {
            var type = typeof(T);
            if (type == typeof(bool) || type == typeof(bool?)) return "BOOLEAN";
            if (type.IsNumericType()) return "NUMBER";
            return "STRING";
        }
    }
}

// Extension method for numeric type checking
public static class TypeExtensions
{
    public static bool IsNumericType(this Type type)
    {
        var numericTypes = new[]
        {
            typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
            typeof(int), typeof(uint), typeof(long), typeof(ulong),
            typeof(float), typeof(double), typeof(decimal),
            typeof(byte?), typeof(sbyte?), typeof(short?), typeof(ushort?),
            typeof(int?), typeof(uint?), typeof(long?), typeof(ulong?),
            typeof(float?), typeof(double?), typeof(decimal?)
        };
        
        return numericTypes.Contains(type);
    }
}