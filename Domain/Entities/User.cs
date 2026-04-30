using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class User : IdentityUser
    {
        public bool IsDeleted { get; set; }
        public bool IsActive { get; set; }
        public string? ProfileImage { get; set; }
        public string? GoogleId { get; set; }
        public bool IsGoogleUser { get; set; }
        public virtual List<Notification> Notifications { get; set; } = null!;
    }
}
