using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nava.Entities.Media
{
    public class Media : BaseEntity
    {
        public Media()
        {
            LikedUsers = new List<LikedMedia>();
            VisitedUsers = new List<VisitedMedia>();
        }
        public string Title { get; set; }
        public MediaType Type { get; set; }
        public string FilePath { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string ArtworkPath { get; set; }

        // International Standard Recording Code (ISRC) for musics only
        public string Isrc { get; set; }
        public int TrackNumber { get; set; }
        public string Lyric { get; set; }

        public int AlbumId { get; set; }
        public virtual Album Album { get; set; }

        public virtual ICollection<LikedMedia> LikedUsers { get; set; }
        public virtual ICollection<VisitedMedia> VisitedUsers { get; set; }
    }

    public enum MediaType
    {
        [Display(Name = "آهنگ")]
        Music = 1,

        [Display(Name = "نماهنگ")]
        MusicVideo = 2
    }

    public class MediaConfiguration : IEntityTypeConfiguration<Media>
    {
        public void Configure(EntityTypeBuilder<Media> builder)
        {
            builder.Property(media => media.Title).IsRequired().HasMaxLength(100);
            builder.Property(media => media.TrackNumber).IsRequired();
            builder.Property(media => media.FilePath).IsRequired();
        }
    }
}
