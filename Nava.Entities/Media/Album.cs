using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nava.Entities.Media
{
    public class Album : BaseEntity
    {
        public string Title { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string Genre { get; set; }
        public bool IsComplete { get; set; }
        public bool IsSingle { get; set; }
        public string Copyright { get; set; }
        public string ArtworkPath { get; set; }

        public virtual ICollection<Media> Medias { get; set; }
        public virtual ICollection<Artist> Artists { get; set; }
    }

    public class AlbumConfiguration : IEntityTypeConfiguration<Album>
    {
        public void Configure(EntityTypeBuilder<Album> builder)
        {
            builder.Property(album => album.Title).IsRequired().HasMaxLength(100);
            builder.Property(album => album.Genre).IsRequired().HasMaxLength(100);
            builder.Property(album => album.ReleaseDate).IsRequired();
        }
    }
}
