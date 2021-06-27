using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nava.Entities.Media;

namespace Nava.Entities.User
{
    public class User : IdentityUser<int>, IEntity
    {
        public User()
        {
            IsActive = true;
            FollowingArtists = new List<Following>();
            LikedMedias = new List<LikedMedia>();
            VisitedMedias = new List<VisitedMedia>();
        }

        public string FullName { get; set; }
        public string AvatarPath { get; set; }
        public string Bio { get; set; }
        public bool IsActive { get; set; }

        public virtual ICollection<Following> FollowingArtists { get; set; }

        public virtual ICollection<LikedMedia> LikedMedias { get; set; }
        public virtual ICollection<VisitedMedia> VisitedMedias { get; set; }
    }

    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.Property(user => user.UserName).IsRequired().HasMaxLength(20);
            builder.Property(user => user.FullName).IsRequired().HasMaxLength(100);
        }
    }
}
