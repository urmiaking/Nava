using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nava.Entities.Media
{
    public class Following : IEntity
    {
        public int UserId { get; set; }
        public int ArtistId { get; set; }
        public DateTime TimeStamp { get; set; }

        public virtual User.User User { get; set; }
        public virtual Artist Artist { get; set; }
    }

    public class FollowingConfiguration : IEntityTypeConfiguration<Following>
    {
        public void Configure(EntityTypeBuilder<Following> builder)
        {
            builder.HasKey(u => new { u.UserId, u.ArtistId });

            builder.HasOne(pt => pt.User)
                .WithMany(p => p.FollowingArtists)
                .HasForeignKey(pt => pt.UserId);

            builder.HasOne(pt => pt.Artist)
                .WithMany(t => t.Followers)
                .HasForeignKey(pt => pt.ArtistId);

            builder.Property(a => a.TimeStamp)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}
