using Microsoft.EntityFrameworkCore;
using JonyBalls3.Data;
using JonyBalls3.Models;

namespace JonyBalls3.Services
{
    public class ChatService
    {
        private readonly ApplicationDbContext _context;
        
        public ChatService(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<List<ChatMessage>> GetMessagesAsync(int projectId, int lastMessageId = 0)
        {
            var query = _context.ChatMessages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.ProjectId == projectId);
            
            if (lastMessageId > 0)
            {
                query = query.Where(m => m.Id > lastMessageId);
            }
            
            return await query
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }
        
        public async Task<ChatMessage> SendMessageAsync(string senderId, string receiverId, int projectId, string message, string attachmentUrl = "")
        {
            var chatMessage = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                ProjectId = projectId,
                Message = message,
                SentAt = DateTime.Now,
                AttachmentUrl = attachmentUrl ?? ""
            };
            
            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();
            
            return chatMessage;
        }
        
        public async Task MarkAsReadAsync(int messageId, string userId)
        {
            var message = await _context.ChatMessages
                .FirstOrDefaultAsync(m => m.Id == messageId && m.ReceiverId == userId && m.ReadAt == null);
                
            if (message != null)
            {
                message.ReadAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }
        
        public async Task MarkAllAsReadAsync(int projectId, string userId)
        {
            var messages = await _context.ChatMessages
                .Where(m => m.ProjectId == projectId && m.ReceiverId == userId && m.ReadAt == null)
                .ToListAsync();
                
            foreach (var message in messages)
            {
                message.ReadAt = DateTime.Now;
            }
            
            await _context.SaveChangesAsync();
        }
        
        public async Task MarkMessagesAsReadAsync(List<int> messageIds, string userId)
        {
            var messages = await _context.ChatMessages
                .Where(m => messageIds.Contains(m.Id) && m.ReceiverId == userId && m.ReadAt == null)
                .ToListAsync();
                
            foreach (var message in messages)
            {
                message.ReadAt = DateTime.Now;
            }
            
            await _context.SaveChangesAsync();
        }
        
        public async Task<List<Project>> GetUserChatsAsync(string userId)
        {
            return await _context.Projects
                .Include(p => p.User)
                .Include(p => p.Contractor)
                    .ThenInclude(c => c.User)
                .Include(p => p.ChatMessages)
                .Where(p => p.UserId == userId || (p.Contractor != null && p.Contractor.UserId == userId))
                .Where(p => p.ContractorId != null)
                .OrderByDescending(p => p.ChatMessages.Any() ? p.ChatMessages.Max(m => m.SentAt) : p.CreatedAt)
                .ToListAsync();
        }
        
        public async Task<int> GetUnreadCountForProjectAsync(string userId, int projectId)
        {
            return await _context.ChatMessages
                .Where(m => m.ProjectId == projectId && m.ReceiverId == userId && m.ReadAt == null)
                .CountAsync();
        }
        
        public async Task<int> GetTotalUnreadCountAsync(string userId)
        {
            return await _context.ChatMessages
                .Where(m => m.ReceiverId == userId && m.ReadAt == null)
                .CountAsync();
        }
    }
}