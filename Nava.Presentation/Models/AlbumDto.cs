using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Nava.Entities.Media;
using Nava.Presentation.Models.Validations;
using Nava.WebFramework.Api;
using ValidationContext = System.ComponentModel.DataAnnotations.ValidationContext;

namespace Nava.Presentation.Models
{
    public class AlbumDto : BaseDto<AlbumDto, Album>, IValidatableObject
    {
        [Display(Name = "نام آلبوم")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        [MaxLength(100, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string Title { get; set; }

        [Display(Name = "تاریخ انتشار")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        public DateTime ReleaseDate { get; set; }

        [Display(Name = "ژانر")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        [MaxLength(100, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string Genre { get; set; }

        [Display(Name = "وضعیت اتمام آلبوم")]
        public bool IsComplete { get; set; }

        [Display(Name = "تک آهنگ")]
        public bool IsSingle { get; set; }

        [Display(Name = "کپی رایت")]
        public string Copyright { get; set; }

        [Display(Name = "مسیر عکس آلبوم")]
        public string ArtworkPath { get; set; }

        [Display(Name = "عکس آلبوم")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        [MaxFileSize(3 * 1024 * 1024)]
        [AllowedExtensions(new[] { ".jpg", ".png" })]
        public IFormFile ImageFile { get; set; }

        [Display(Name = "آیدی هنرمندان آلبوم")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        public string ArtistIds { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var artistIdsString = ArtistIds.Split(',');
            foreach (var artistId in artistIdsString)
                if (!int.TryParse(artistId, out _))
                    yield return new ValidationResult(
                        "فرمت آیدی هنرمندان اشتباه است. فرمت درج برای مثال '1,2' می باشد.",
                        new[] { nameof(ArtistIds) });
        }
    }

    public class AlbumResultDto : BaseDto<AlbumResultDto, Album>
    {
        [Display(Name = "نام آلبوم")]
        public string Title { get; set; }

        [Display(Name = "تاریخ انتشار")]
        public DateTime ReleaseDate { get; set; }

        [Display(Name = "ژانر")]
        public string Genre { get; set; }

        [Display(Name = "وضعیت اتمام آلبوم")]
        public bool IsComplete { get; set; }

        [Display(Name = "تک آهنگ")]
        public bool IsSingle { get; set; }

        [Display(Name = "کپی رایت")]
        public string Copyright { get; set; }

        [Display(Name = "مسیر عکس آلبوم")]
        public string ArtworkPath { get; set; }

        public string MediasCount { get; set; }

        public string Singers { get; set; }

        public override void CustomMappings(IMappingExpression<Album, AlbumResultDto> mapping)
        {
            mapping.ForMember(
                dest => dest.MediasCount,
                config =>
                    config.MapFrom(src => $"{src.Medias.Count}"));

            mapping.ForMember(
                dest => dest.Singers,
                config =>
                    config.MapFrom(src => $"{string.Join(",", src.Artists.Select(i => i.ArtisticName))}"));
        }
    }

    public class AlbumUpdateDto : BaseDto<AlbumUpdateDto, Album>, IValidatableObject
    {
        [Display(Name = "نام آلبوم")]
        [MaxLength(100, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string Title { get; set; }

        [Display(Name = "تاریخ انتشار")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        public DateTime ReleaseDate { get; set; }

        [Display(Name = "ژانر")]
        [MaxLength(100, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string Genre { get; set; }

        [Display(Name = "وضعیت اتمام آلبوم")]
        public bool IsComplete { get; set; }

        [Display(Name = "تک آهنگ")]
        public bool IsSingle { get; set; }

        [Display(Name = "کپی رایت")]
        public string Copyright { get; set; }

        [Display(Name = "مسیر عکس آلبوم")]
        public string ArtworkPath { get; set; }

        [Display(Name = "عکس آلبوم")]
        [MaxFileSize(3 * 1024 * 1024)]
        [AllowedExtensions(new[] { ".jpg", ".png" })]
        public IFormFile ImageFile { get; set; }

        [Display(Name = "آیدی هنرمندان آلبوم")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        public string ArtistIds { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var artistIdsString = ArtistIds.Split(',');
            foreach (var artistId in artistIdsString)
                if (!int.TryParse(artistId, out _))
                    yield return new ValidationResult(
                        "فرمت آیدی هنرمندان اشتباه است. فرمت درج برای مثال '1,2' می باشد.",
                        new[] { nameof(ArtistIds) });
        }
    }
}
