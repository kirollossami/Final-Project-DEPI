using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Business.Services;

public class ComplaintService : IComplaintService
{
    private readonly IComplaintRepository _complaintRepository;

    public ComplaintService(IComplaintRepository complaintRepository)
    {
        _complaintRepository = complaintRepository;
    }

    public async Task<ComplaintResponse?> GetComplaintByIdAsync(Guid complaintId)
    {
        var complaint = await _complaintRepository.GetAsync(complaintId);
        if (complaint == null) return null;

        return new ComplaintResponse
        {
            ComplaintId = complaint.ComplaintId,
            StudentId = complaint.StudentId,
            LandLordId = complaint.LandLordId,
            Description = complaint.Description,
            Status = complaint.Status,
            CreatedDate = complaint.CreatedDate
        };
    }

    public async Task<ComplaintIndexedResponse> GetComplaintsAsync(ComplaintFilterRequest filter)
    {
        var query = _complaintRepository.GetAll().AsQueryable();

        if (filter.StudentId.HasValue)
        {
            query = query.Where(c => c.StudentId == filter.StudentId.Value);
        }

        if (filter.LandLordId.HasValue)
        {
            query = query.Where(c => c.LandLordId == filter.LandLordId.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(c => c.Status == filter.Status.Value);
        }

        var totalCount = await query.CountAsync();
        var complaints = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new ComplaintIndexedResponse
        {
            Records = complaints.Select(c => new ComplaintResponse
            {
                ComplaintId = c.ComplaintId,
                StudentId = c.StudentId,
                LandLordId = c.LandLordId,
                Description = c.Description,
                Status = c.Status,
                CreatedDate = c.CreatedDate
            }).ToList(),
            TotalRecords = totalCount,
            PageIndex = filter.PageNumber - 1,
            PageSize = filter.PageSize
        };
    }

    public async Task<ComplaintResponse?> CreateComplaintAsync(ComplaintCreateRequest request)
    {
        var complaint = new Domain.Entities.Complaint
        {
            ComplaintId = Guid.NewGuid(),
            StudentId = request.StudentId,
            LandLordId = request.LandLordId,
            Description = request.Description,
            Status = ComplaintStatus.Pending,
            CreatedDate = DateTime.UtcNow
        };

        await _complaintRepository.Insert(complaint);
        await _complaintRepository.CommitAsync();

        return new ComplaintResponse
        {
            ComplaintId = complaint.ComplaintId,
            StudentId = complaint.StudentId,
            LandLordId = complaint.LandLordId,
            Description = complaint.Description,
            Status = complaint.Status,
            CreatedDate = complaint.CreatedDate
        };
    }

    public async Task<ComplaintResponse?> UpdateComplaintAsync(ComplaintUpdateRequest request)
    {
        var complaint = await _complaintRepository.GetAsync(request.ComplaintId);
        if (complaint == null) return null;

        if (request.Status.HasValue)
        {
            complaint.Status = request.Status.Value;
        }

        if (request.Description != null)
        {
            complaint.Description = request.Description;
        }

        _complaintRepository.Update(complaint);
        await _complaintRepository.CommitAsync();

        return new ComplaintResponse
        {
            ComplaintId = complaint.ComplaintId,
            StudentId = complaint.StudentId,
            LandLordId = complaint.LandLordId,
            Description = complaint.Description,
            Status = complaint.Status,
            CreatedDate = complaint.CreatedDate
        };
    }

    public async Task<bool> DeleteComplaintAsync(Guid complaintId)
    {
        var complaint = await _complaintRepository.GetAsync(complaintId);
        if (complaint == null) return false;

        await _complaintRepository.Delete(complaint);
        await _complaintRepository.CommitAsync();

        return true;
    }
}
