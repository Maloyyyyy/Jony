using JonyBalls3.Data;
using JonyBalls3.Models;
using Microsoft.EntityFrameworkCore;

namespace JonyBalls3.Services
{
    public class ContractorService
    {
        private readonly ApplicationDbContext _context;
        
        public ContractorService(ApplicationDbContext context)
        {
            _context = context;
        }
public async Task<List<PortfolioItem>> GetPortfolioAsync(int contractorId)
{
    return await _context.PortfolioItems
        .Where(p => p.ContractorId == contractorId)
        .OrderByDescending(p => p.UploadedAt)
        .ToListAsync();
}

public async Task AddPortfolioItemAsync(PortfolioItem item)
{
    _context.PortfolioItems.Add(item);
    await _context.SaveChangesAsync();
}
public async Task<List<ContractorProfile>> SearchContractorsAsync(string? specialization, decimal? maxRate, string? status = null)
{
    var query = _context.ContractorProfiles
        .Include(c => c.User)
        .Include(c => c.Reviews)
        .AsQueryable();
    
    if (!string.IsNullOrEmpty(specialization))
    {
        query = query.Where(c => c.Specialization.Contains(specialization));
    }
    
    if (maxRate.HasValue)
    {
        query = query.Where(c => c.HourlyRate <= maxRate.Value);
    }
    
    if (!string.IsNullOrEmpty(status))
    {
        if (status == "available")
            query = query.Where(c => c.Status == ContractorStatus.Available);
        else if (status == "busy")
            query = query.Where(c => c.Status == ContractorStatus.Busy);
    }
    
    return await query.OrderByDescending(c => c.Rating).ToListAsync();
}
public async Task DeletePortfolioItemAsync(int id, int contractorId)
{
    var item = await _context.PortfolioItems
        .FirstOrDefaultAsync(p => p.Id == id && p.ContractorId == contractorId);
    
    if (item != null)
    {
        // Удаляем файл
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", item.ImageUrl.TrimStart('/'));
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }
        
        _context.PortfolioItems.Remove(item);
        await _context.SaveChangesAsync();
    }
}
        
        public async Task<List<ContractorProfile>> GetAllContractorsAsync()
        {
            return await _context.ContractorProfiles
                .Include(c => c.User)
                .Include(c => c.Portfolio)
                .Include(c => c.Reviews)
                .OrderByDescending(c => c.Rating)
                .ToListAsync();
        }
        
        public async Task<ContractorProfile> GetContractorByIdAsync(int id)
        {
            return await _context.ContractorProfiles
                .Include(c => c.User)
                .Include(c => c.Portfolio)
                .Include(c => c.Reviews)
                .FirstOrDefaultAsync(c => c.Id == id);
        }
        
      public async Task<ContractorProfile?> GetContractorByUserIdAsync(string userId)
{
    return await _context.ContractorProfiles
        .Include(c => c.User)
        .Include(c => c.Portfolio)
        .Include(c => c.Projects)
            .ThenInclude(p => p.User)
        .FirstOrDefaultAsync(c => c.UserId == userId);
}
        
        public async Task<ContractorProfile> CreateContractorProfileAsync(ContractorProfile profile)
        {
            profile.CreatedAt = DateTime.Now;
            _context.ContractorProfiles.Add(profile);
            await _context.SaveChangesAsync();
            return profile;
        }
        
        public async Task<ContractorProfile> UpdateContractorProfileAsync(ContractorProfile profile)
        {
            _context.Entry(profile).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return profile;
        }
        
        public async Task<List<ContractorProfile>> SearchContractorsAsync(string specialization, decimal? maxRate)
        {
            var query = _context.ContractorProfiles
                .Include(c => c.User)
                .Include(c => c.Reviews)
                .AsQueryable();
            
            if (!string.IsNullOrEmpty(specialization))
            {
                query = query.Where(c => c.Specialization.Contains(specialization));
            }
            
            if (maxRate.HasValue)
            {
                query = query.Where(c => c.HourlyRate <= maxRate.Value);
            }
            
            return await query.OrderByDescending(c => c.Rating).ToListAsync();
        }
    }
}
