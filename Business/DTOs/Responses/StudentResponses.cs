using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOs.Responses;

public class StudentResponse
{
    public Guid StudentId { get; set; }
    public string? UserId { get; set; }
    public DateTime DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PreferredArea { get; set; }
    public string? NationalId { get; set; }
}

public class StudentIndexedResponse : GenericIndexedResponse<StudentResponse>
{
}
