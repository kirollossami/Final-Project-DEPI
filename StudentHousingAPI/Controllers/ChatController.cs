using Business.DTOs.Requests;
using Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudentHousingAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ChatController : BaseController
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
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
