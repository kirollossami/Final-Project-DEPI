using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.DTOs.Responses;

public class GenericIndexedResponse<T> where T : class
{
    public int PageSize { get; set; } = 15;
    public int PageIndex { get; set; } = 0;
    public int TotalRecords{  get; set; }
    public List<T>? Records { get; set; } 
}
