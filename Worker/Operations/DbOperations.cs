using DetectorVulnerabilitatsDatabase.Context;
using Microsoft.EntityFrameworkCore;

namespace Worker.Operations
{
    public class DbOperations
    {
        private readonly DetectorVulnerabilitatsDatabaseContext _context;
        public DbOperations(DetectorVulnerabilitatsDatabaseContext context)
        {
            _context = context;
        }

        public async Task AddObjectToDbAsync<T>(T obj) where T : class
        {
            await _context.Set<T>().AddAsync(obj);
            await _context.SaveChangesAsync();
        }

        public async Task<List<T>> GetAllAsync<T>() where T : class
        {
            return await _context.Set<T>()
                                 .AsNoTracking()
                                 .ToListAsync();
        }
    }
}
