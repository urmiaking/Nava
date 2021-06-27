﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nava.Entities.Media
{
    public class VisitedMedia : IEntity
    {
        public int UserId { get; set; }
        public int MediaId { get; set; }
        public DateTime TimeStamp { get; set; }

        public virtual User.User User { get; set; }
        public virtual Media Media { get; set; }
    }

    public class VisitedMediaConfiguration : IEntityTypeConfiguration<VisitedMedia>
    {
        public void Configure(EntityTypeBuilder<VisitedMedia> builder)
        {
            builder.HasKey(u => new { u.UserId, u.MediaId });

            builder.HasOne(pt => pt.User)
                .WithMany(p => p.VisitedMedias)
                .HasForeignKey(pt => pt.UserId);

            builder.HasOne(pt => pt.Media)
                .WithMany(t => t.VisitedUsers)
                .HasForeignKey(pt => pt.MediaId);

            builder.Property(a => a.TimeStamp)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}
