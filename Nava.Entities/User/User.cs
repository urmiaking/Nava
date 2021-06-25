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
        }

        public string FullName { get; set; }
        public string AvatarPath { get; set; }
        public string Bio { get; set; }
        public bool IsActive { get; set; }

        public virtual ICollection<Artist> FollowingArtists { get; set; }
        public virtual ICollection<Media.Media> LikedMedias { get; set; }
        public virtual ICollection<Media.Media> VisitedMedias { get; set; }
    }

    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.Property(user => user.UserName).IsRequired().HasMaxLength(20);
            builder.Property(user => user.FullName).IsRequired().HasMaxLength(100);

            builder
                .HasMany(user => user.FollowingArtists)
                .WithMany(artist => artist.Followers)
                .UsingEntity(a => a.ToTable("Followings"));
            builder
                .HasMany(user => user.LikedMedias)
                .WithMany(media => media.LikedUsers)
                .UsingEntity(a => a.ToTable("Likes"));
            builder
                .HasMany(user => user.VisitedMedias)
                .WithMany(media => media.VisitedUsers)
                .UsingEntity(a => a.ToTable("Visits"));
        }
    }
}
