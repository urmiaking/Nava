using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using Nava.Entities.Media;
using Nava.Presentation.Models.Validations;
using Nava.WebFramework.Api;
using ValidationContext = System.ComponentModel.DataAnnotations.ValidationContext;

namespace Nava.Presentation.Models
{
    public class ArtistDto : BaseDto<ArtistDto, Artist>, IValidatableObject
    {
        [Display(Name = "نام کامل خواننده")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        [MaxLength(100, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string FullName { get; set; }

        [Display(Name = "نام هنری خواننده")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        [MaxLength(100, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string ArtisticName { get; set; }

        [Display(Name = "تاریخ تولد")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        public DateTime BirthDate { get; set; }

        [Display(Name = "مسیر آواتار")]
        public string AvatarPath { get; set; }

        [Display(Name = "بیو")]
        [MaxLength(1000, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string Bio { get; set; }

        [Display(Name = "آواتار")]
        [Required(ErrorMessage = "لطفا {0} را آپلود کنید")]
        [MaxFileSize(1 * 1024 * 1024)]
        [AllowedExtensions(new[] { ".jpg", ".png" })]
        public IFormFile ImageFile { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (BirthDate.Equals(DateTime.MinValue))
                yield return new ValidationResult(
                    "تاریخ تولد نمی تواند null باشد",
                    new[] { nameof(BirthDate) });

            if (BirthDate > DateTime.Now.AddYears(100))
                yield return new ValidationResult(
                    "تاریخ تولد نمی تواند بزرگتر از 100 سال باشد",
                    new[] { nameof(BirthDate) });
        }
    }

    public class ArtistResultDto : BaseDto<ArtistResultDto, Artist>
    {
        public string FullName { get; set; }

        public string ArtisticName { get; set; }

        public DateTime BirthDate { get; set; }

        public string AvatarPath { get; set; }

        public string Bio { get; set; }

        public string FollowersCount { get; set; }

        public override void CustomMappings(IMappingExpression<Artist, ArtistResultDto> mapping)
        {
            mapping.ForMember(
                dest => dest.FollowersCount,
                config => config.MapFrom(src => $"{src.Followers.Count}"));
        }
    }

    public class ArtistUpdateDto : BaseDto<ArtistUpdateDto, Artist>, IValidatableObject
    {
        [Display(Name = "نام کامل خواننده")]
        [MaxLength(100, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string FullName { get; set; }

        [Display(Name = "نام هنری خواننده")]
        [MaxLength(100, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string ArtisticName { get; set; }

        [Display(Name = "تاریخ تولد")]
        public DateTime BirthDate { get; set; }

        [Display(Name = "مسیر آواتار")]
        public string AvatarPath { get; set; }

        [Display(Name = "بیو")]
        [MaxLength(1000, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string Bio { get; set; }

        [Display(Name = "آواتار")]
        [MaxFileSize(1 * 1024 * 1024)]
        [AllowedExtensions(new[] { ".jpg", ".png" })]
        public IFormFile ImageFile { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (BirthDate > DateTime.Now.AddYears(100))
                yield return new ValidationResult(
                    "تاریخ تولد نمی تواند بزرگتر از 100 سال باشد",
                    new[] { nameof(BirthDate) });

            if (BirthDate.Equals(DateTime.MinValue))
                yield return new ValidationResult(
                    "تاریخ تولد نمی تواند null باشد",
                    new[] { nameof(BirthDate) });
        }
    }

    public class MongoArtistDto : BaseDto<MongoArtistDto, Entities.MongoDb.Artist, ObjectId>, IValidatableObject
    {
        protected new ObjectId Id { get; set; } = ObjectId.GenerateNewId(DateTime.Now);

        [Display(Name = "نام کامل خواننده")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        [MaxLength(100, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string FullName { get; set; }

        [Display(Name = "نام هنری خواننده")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        [MaxLength(100, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string ArtisticName { get; set; }

        [Display(Name = "تاریخ تولد")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        public DateTime BirthDate { get; set; }

        //[Display(Name = "مسیر آواتار")]
        //public string AvatarPath { get; set; }

        [Display(Name = "بیو")]
        [MaxLength(1000, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string Bio { get; set; }

        [Display(Name = "آواتار")]
        [Required(ErrorMessage = "لطفا {0} را آپلود کنید")]
        [MaxFileSize(1 * 1024 * 1024)]
        [AllowedExtensions(new[] { ".jpg", ".png" })]
        public IFormFile ImageFile { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (BirthDate.Equals(DateTime.MinValue))
                yield return new ValidationResult(
                    "تاریخ تولد نمی تواند null باشد",
                    new[] { nameof(BirthDate) });

            if (BirthDate > DateTime.Now.AddYears(100))
                yield return new ValidationResult(
                    "تاریخ تولد نمی تواند بزرگتر از 100 سال باشد",
                    new[] { nameof(BirthDate) });
        }
    }

    public class MongoArtistResultDto : BaseDto<MongoArtistResultDto, Entities.MongoDb.Artist, string>
    {
        public string FullName { get; set; }
        public string ArtisticName { get; set; }
        public DateTime BirthDate { get; set; }
        public string AvatarPath { get; set; }
        public string Bio { get; set; }
        public string FollowersCount { get; set; }

        public override void CustomMappings(IMappingExpression<Entities.MongoDb.Artist, MongoArtistResultDto> mapping)
        {
            mapping.ForMember(
                dest => dest.FollowersCount,
                config => config.MapFrom(src => $"{src.Followers.Count}"));
        }
    }

    public class MongoArtistUpdateDto : BaseDto<MongoArtistUpdateDto, Entities.MongoDb.Artist, string>, IValidatableObject
    {
        [Display(Name = "نام کامل خواننده")]
        [MaxLength(100, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string FullName { get; set; }

        [Display(Name = "نام هنری خواننده")]
        [MaxLength(100, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string ArtisticName { get; set; }

        [Display(Name = "تاریخ تولد")]
        public DateTime BirthDate { get; set; }

        [Display(Name = "بیو")]
        [MaxLength(1000, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string Bio { get; set; }

        [Display(Name = "آواتار")]
        [MaxFileSize(1 * 1024 * 1024)]
        [AllowedExtensions(new[] { ".jpg", ".png" })]
        public IFormFile ImageFile { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (BirthDate > DateTime.Now.AddYears(100))
                yield return new ValidationResult(
                    "تاریخ تولد نمی تواند بزرگتر از 100 سال باشد",
                    new[] { nameof(BirthDate) });

            if (BirthDate.Equals(DateTime.MinValue))
                yield return new ValidationResult(
                    "تاریخ تولد نمی تواند null باشد",
                    new[] { nameof(BirthDate) });
        }
    }
}
