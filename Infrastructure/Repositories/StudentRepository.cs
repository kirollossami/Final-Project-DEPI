using Domain.Entities;
using Infrastructure.Context;
using Infrastructure.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories;


public interface IStudentRepository : IBaseRepository<Student>
{
}

public class StudentRepository : BaseRepository<Student>, IStudentRepository
{
    public StudentRepository(StudentHousingDBContext context) : base(context)
    {
    }
}