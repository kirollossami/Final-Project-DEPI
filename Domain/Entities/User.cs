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
<<<<<<< HEAD
        public bool TwoFactorEnabled { get; set; }
        public string? TwoFactorSecret { get; set; }
=======
>>>>>>> d373b1145cc825f184dd583507a557a4aaf9a1f0
        public virtual List<Notification> Notifications { get; set; } = null!;
    }
}
