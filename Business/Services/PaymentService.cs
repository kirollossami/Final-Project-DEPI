using Business.DTOs.Requests;
using Business.DTOs.Responses;
using Business.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Business.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;

    public PaymentService(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<PaymentResponse?> GetPaymentByIdAsync(Guid paymentId)
    {
        var payment = await _paymentRepository.GetAsync(paymentId);
        if (payment == null) return null;

        return new PaymentResponse
        {
            PaymentId = payment.PaymentId,
            BookingId = payment.BookingId,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            PaymentStatus = payment.PaymentStatus,
            PaymentDate = payment.PaymentDate,
            TransactionId = payment.TransactionId
        };
    }

    public async Task<PaymentIndexedResponse> GetPaymentsAsync(PaymentFilterRequest filter)
    {
        var query = _paymentRepository.GetAll().AsQueryable();

        if (filter.BookingId.HasValue)
        {
            query = query.Where(p => p.BookingId == filter.BookingId.Value);
        }

        if (filter.PaymentStatus.HasValue)
        {
            query = query.Where(p => p.PaymentStatus == filter.PaymentStatus.Value);
        }

        if (filter.PaymentMethod.HasValue)
        {
            query = query.Where(p => p.PaymentMethod == filter.PaymentMethod.Value);
        }

        var totalCount = await query.CountAsync();
        var payments = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PaymentIndexedResponse
        {
            Records = payments.Select(p => new PaymentResponse
            {
                PaymentId = p.PaymentId,
                BookingId = p.BookingId,
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod,
                PaymentStatus = p.PaymentStatus,
                PaymentDate = p.PaymentDate,
                TransactionId = p.TransactionId
            }).ToList(),
            TotalRecords = totalCount,
            PageIndex = filter.PageNumber - 1,
            PageSize = filter.PageSize
        };
    }

    public async Task<PaymentResponse?> CreatePaymentAsync(PaymentCreateRequest request)
    {
        var payment = new Domain.Entities.Payment
        {
            PaymentId = Guid.NewGuid(),
            BookingId = request.BookingId,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = PaymentStatus.Pending,
            PaymentDate = DateTime.UtcNow,
            TransactionId = request.TransactionId
        };

        await _paymentRepository.Insert(payment);
        await _paymentRepository.CommitAsync();

        return new PaymentResponse
        {
            PaymentId = payment.PaymentId,
            BookingId = payment.BookingId,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            PaymentStatus = payment.PaymentStatus,
            PaymentDate = payment.PaymentDate,
            TransactionId = payment.TransactionId
        };
    }

    public async Task<PaymentResponse?> UpdatePaymentAsync(PaymentUpdateRequest request)
    {
        var payment = await _paymentRepository.GetAsync(request.PaymentId);
        if (payment == null) return null;

        if (request.PaymentStatus.HasValue)
        {
            payment.PaymentStatus = request.PaymentStatus.Value;
        }

        if (request.TransactionId != null)
        {
            payment.TransactionId = request.TransactionId;
        }

        await _paymentRepository.Delete(payment);
        await _paymentRepository.CommitAsync();

        return new PaymentResponse
        {
            PaymentId = payment.PaymentId,
            BookingId = payment.BookingId,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            PaymentStatus = payment.PaymentStatus,
            PaymentDate = payment.PaymentDate,
            TransactionId = payment.TransactionId
        };
    }
}
