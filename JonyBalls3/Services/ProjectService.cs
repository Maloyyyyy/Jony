using Microsoft.EntityFrameworkCore;
 using JonyBalls3.Data;
using JonyBalls3.Models;

namespace JonyBalls3.Services
{
    public class ProjectService
    {
        private readonly ApplicationDbContext _context;
        
        public ProjectService(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<List<Project>> GetUserProjectsAsync(string userId)
        {
            return await _context.Projects
                .Include(p => p.Stages)
                .Include(p => p.Contractor)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
        
        public async Task<Project> GetProjectByIdAsync(int id)
        {
            return await _context.Projects
                .Include(p => p.Stages)
                .Include(p => p.Contractor)
                .Include(p => p.Expenses)
                .FirstOrDefaultAsync(p => p.Id == id);
        }
        
        public async Task<Project> CreateProjectAsync(Project project)
        {
            project.CreatedAt = DateTime.Now;
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
            return project;
        }
        
        public async Task<Project> UpdateProjectAsync(Project project)
        {
            project.UpdatedAt = DateTime.Now;
            _context.Entry(project).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return project;
        }
        
        public async Task<bool> DeleteProjectAsync(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null) return false;
            
            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
            return true;
        }
        
        public async Task<ProjectStage> AddStageAsync(ProjectStage stage)
        {
            _context.ProjectStages.Add(stage);
            await _context.SaveChangesAsync();
            return stage;
        }
        
        public async Task<ProjectStage> GetStageByIdAsync(int id)
        {
            return await _context.ProjectStages
                .Include(s => s.Project)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task UpdateStageAsync(ProjectStage stage)
        {
            _context.Entry(stage).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task UpdateProjectProgressAsync(int projectId)
        {
            var project = await _context.Projects
                .Include(p => p.Stages)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project != null && project.Stages.Any())
            {
                project.Progress = (int)Math.Round(project.Stages.Average(s => s.Progress));
                project.Spent = project.Stages.Sum(s => s.Spent);
                project.UpdatedAt = DateTime.Now;
                
                await _context.SaveChangesAsync();
            }
        }
    }
}