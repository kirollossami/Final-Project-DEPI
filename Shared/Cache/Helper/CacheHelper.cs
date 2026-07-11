using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Cache.Helper;

public static class CacheHelper
{
    public static int ExpiresTime { get; set; } = 2;
    public static string StudentHousingCacheKey { get; set; } = "StudentHousingCacheKey";
    public static string HousingUnitsMapPinsCacheKey { get; set; } = "HousingUnitsMapPinsCacheKey";
}
