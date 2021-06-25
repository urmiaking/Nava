using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nava.Entities.Media
{
    public class Artist : BaseEntity
    {
        public string FullName { get; set; }
        public string ArtisticName { get; set; }
        public DateTime BirthDate { get; set; }
        public string AvatarPath { get; set; }
        public string Bio { get; set; }

        public virtual ICollection<Album> Albums { get; set; }
        public virtual ICollection<User.User> Followers { get; set; }
    }

    public class ArtistConfiguration : IEntityTypeConfiguration<Artist>
    {
        public void Configure(EntityTypeBuilder<Artist> builder)
        {
            builder.Property(artist => artist.ArtisticName).IsRequired().HasMaxLength(100);
            builder
                .HasMany(artist => artist.Albums)
                .WithMany(album => album.Artists)
                .UsingEntity(a => a.ToTable("AlbumArtists"));
        }
    }
}
