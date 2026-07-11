using Business.DTOs.Requests;
using Business.Interfaces;
using Domain.Enums;
using Infrastructure.Repositories.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudentHousingAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ChatController : BaseController
{
    private readonly IChatService _chatService;
    private readonly INotificationService _notificationService;
    private readonly IBaseRepository<Domain.Entities.Conversation> _conversationRepo;
    private readonly IUnitOfWork _unitOfWork;

    public ChatController(IChatService chatService, INotificationService notificationService, IBaseRepository<Domain.Entities.Conversation> conversationRepo, IUnitOfWork unitOfWork)
    {
        _chatService = chatService;
        _notificationService = notificationService;
        _conversationRepo = conversationRepo;
        _unitOfWork = unitOfWork;
    }

    [HttpPost("initiate")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> InitiateConversation([FromBody] InitiateConversationRequest request)
    {
        var userId = GetLoggedId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var result = await _chatService.InitiatePreBookingConversationAsync(request.HousingUnitId, userId);
            try
            {
                var unit = await _unitOfWork.HousingUnits.GetAsync(request.HousingUnitId);
                if (unit != null)
                {
                    var landlord = await _unitOfWork.LandLords.GetAsync(unit.LandLordId);
                    if (landlord?.UserId != null)
                        await _notificationService.SendRealTimeNotificationAsync(landlord.UserId, "A student has initiated a conversation about your property.", NotificationTypes.NewConversation);
                }
            }
            catch { }
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetUserConversations()
    {
        var userId = GetLoggedId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _chatService.GetUserConversationsAsync(userId);
        return Ok(result);
    }

    [HttpGet("conversations/{bookingId}")]
    public async Task<IActionResult> GetConversation(Guid bookingId)
    {
        var userId = GetLoggedId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var result = await _chatService.GetOrCreateConversationAsync(bookingId, userId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("by-id/{conversationId}")]
    public async Task<IActionResult> GetConversationById(Guid conversationId)
    {
        var userId = GetLoggedId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var result = await _chatService.GetConversationByIdAsync(conversationId, userId);
            if (result == null)
                return NotFound(new { Message = "Conversation not found." });
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("conversations/{conversationId}/messages")]
    public async Task<IActionResult> GetMessages(Guid conversationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetLoggedId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var result = await _chatService.GetMessagesAsync(conversationId, userId, page, pageSize);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("conversations/{conversationId}/messages")]
    public async Task<IActionResult> SendMessage(Guid conversationId, [FromBody] SendMessageRequest request)
    {
        var userId = GetLoggedId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var result = await _chatService.SaveMessageAsync(conversationId, userId, request.Content);
            try
            {
                var conversation = await _conversationRepo.GetAsync(conversationId);
                if (conversation != null)
                {
                    var otherUserId = conversation.StudentUserId == userId ? conversation.LandLordUserId : conversation.StudentUserId;
                    if (!string.IsNullOrEmpty(otherUserId))
                        await _notificationService.SendRealTimeNotificationAsync(otherUserId, "You have a new message.", NotificationTypes.NewMessage);
                }
            }
            catch { }
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPut("conversations/{conversationId}/read")]
    public async Task<IActionResult> MarkAsRead(Guid conversationId)
    {
        var userId = GetLoggedId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            await _chatService.MarkAsReadAsync(conversationId, userId);
            return Ok();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
