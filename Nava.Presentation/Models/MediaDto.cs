using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using Nava.Entities.Media;
using Nava.Presentation.Models.Validations;
using Nava.WebFramework.Api;
using ValidationContext = System.ComponentModel.DataAnnotations.ValidationContext;

namespace Nava.Presentation.Models
{
    #region SQL

    public class MediaDto : BaseDto<MediaDto, Media>, IValidatableObject
    {
        [Display(Name = "نام اثر")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        [MaxLength(100, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string Title { get; set; }

        [Display(Name = "نوع مدیا")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        public MediaType Type { get; set; }

        [Display(Name = "مسیر فایل اثر")]
        public string FilePath { get; set; }

        [Display(Name = "تاریخ انتشار")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        public DateTime ReleaseDate { get; set; }

        [Display(Name = "مسیر عکس اثر")]
        public string ArtworkPath { get; set; }

        // International Standard Recording Code (ISRC) for musics only
        [Display(Name = "کد ISRC")]
        [MaxLength(100, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string Isrc { get; set; }

        [Display(Name = "شماره مدیا")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        public int TrackNumber { get; set; }

        [Display(Name = "متن مدیا")]
        public string Lyric { get; set; }

        [Display(Name = "آیدی آلبوم اثر")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        public int AlbumId { get; set; }

        [Display(Name = "فایل عکس اثر")]
        [MaxFileSize(3 * 1024 * 1024)]
        [AllowedExtensions(new[] { ".jpg", ".png" })]
        public IFormFile ArtworkFile { get; set; }

        [Display(Name = "فایل مدیا")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        [MaxFileSize(500 * 1024 * 1024)]
        [AllowedExtensions(new[] { ".mp3", ".mp4", ".aac", ".wav", ".wma", ".avi", ".mkv" })]
        public IFormFile MediaFile { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Type is MediaType.Music && ArtworkFile is null)
                yield return new ValidationResult(
                    "عکس آهنگ را آپلود کنید.",
                    new[] { nameof(ArtworkFile) });
        }
    }

    public class MediaResultDto : BaseDto<MediaResultDto, Media>
    {
        [Display(Name = "نام اثر")]
        public string Title { get; set; }

        [Display(Name = "نوع مدیا")]
        public MediaType Type { get; set; }

        [Display(Name = "مسیر فایل اثر")]
        public string FilePath { get; set; }

        [Display(Name = "تاریخ انتشار")]
        public DateTime ReleaseDate { get; set; }

        [Display(Name = "مسیر عکس اثر")]
        public string ArtworkPath { get; set; }

        // International Standard Recording Code (ISRC) for musics only
        [Display(Name = "کد ISRC")]
        public string Isrc { get; set; }

        [Display(Name = "شماره مدیا")]
        public int TrackNumber { get; set; }

        [Display(Name = "متن مدیا")]
        public string Lyric { get; set; }

        [Display(Name = "عنوان آلبوم")]
        public string AlbumTitle { get; set; }

        [Display(Name = "خوانندگان")]
        public string Singers { get; set; }

        public override void CustomMappings(IMappingExpression<Media, MediaResultDto> mapping)
        {
            mapping.ForMember(dest => dest.AlbumTitle,
                config => config.MapFrom(src => src.Album.Title));

            mapping.ForMember(dest => dest.Singers, config =>
                config.MapFrom(src => $"{string.Join(',', src.Album.Artists.Select(a => a.ArtisticName))}"));
        }
    }

    public class MediaUpdateDto : BaseDto<MediaUpdateDto, Media>
    {
        [Display(Name = "نام اثر")]
        [MaxLength(100, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string Title { get; set; }

        [Display(Name = "نوع مدیا")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        public MediaType Type { get; set; }

        [Display(Name = "مسیر فایل اثر")]
        public string FilePath { get; set; }

        [Display(Name = "تاریخ انتشار")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        public DateTime ReleaseDate { get; set; }

        [Display(Name = "مسیر عکس اثر")]
        public string ArtworkPath { get; set; }

        // International Standard Recording Code (ISRC) for musics only
        [Display(Name = "کد ISRC")]
        [MaxLength(100, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string Isrc { get; set; }

        [Display(Name = "شماره مدیا")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        public int TrackNumber { get; set; }

        [Display(Name = "متن مدیا")]
        public string Lyric { get; set; }

        [Display(Name = "آیدی آلبوم اثر")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        public int AlbumId { get; set; }

        [Display(Name = "فایل عکس اثر")]
        [MaxFileSize(3 * 1024 * 1024)]
        [AllowedExtensions(new[] { ".jpg", ".png" })]
        public IFormFile ArtworkFile { get; set; }

        [Display(Name = "فایل مدیا")]
        [MaxFileSize(500 * 1024 * 1024)]
        [AllowedExtensions(new[] { ".mp3", ".mp4", ".aac", ".wav", ".wma", ".avi", ".mkv" })]
        public IFormFile MediaFile { get; set; }
    }

    #endregion

    #region MongoDB

    public class MongoMediaDto : BaseDto<MongoMediaDto, Entities.MongoDb.Media, ObjectId>, IValidatableObject
    {
        protected new ObjectId Id { get; set; } = ObjectId.GenerateNewId(DateTime.Now);

        [Display(Name = "نام اثر")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        [MaxLength(100, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string Title { get; set; }

        [Display(Name = "نوع مدیا")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        public MediaType Type { get; set; }

        [Display(Name = "تاریخ انتشار")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        public DateTime ReleaseDate { get; set; }

        // International Standard Recording Code (ISRC) for musics only
        [Display(Name = "کد ISRC")]
        [MaxLength(100, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string Isrc { get; set; }

        [Display(Name = "شماره مدیا")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        public int TrackNumber { get; set; }

        [Display(Name = "متن مدیا")]
        public string Lyric { get; set; }

        [Display(Name = "آیدی آلبوم اثر")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        public string StringAlbumId { get; set; }

        [Display(Name = "فایل عکس اثر")]
        [MaxFileSize(3 * 1024 * 1024)]
        [AllowedExtensions(new[] { ".jpg", ".png" })]
        public IFormFile ArtworkFile { get; set; }

        [Display(Name = "فایل مدیا")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        [MaxFileSize(500 * 1024 * 1024)]
        [AllowedExtensions(new[] { ".mp3", ".mp4", ".aac", ".wav", ".wma", ".avi", ".mkv" })]
        public IFormFile MediaFile { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Type is MediaType.Music && ArtworkFile is null)
                yield return new ValidationResult(
                    "عکس آهنگ را آپلود کنید.",
                    new[] { nameof(ArtworkFile) });
        }
    }

    public class MongoMediaResultDto : BaseDto<MongoMediaResultDto, Entities.MongoDb.Media, string>
    {
        [Display(Name = "نام اثر")]
        public string Title { get; set; }

        [Display(Name = "نوع مدیا")]
        public MediaType Type { get; set; }

        [Display(Name = "مسیر فایل اثر")]
        public string FilePath { get; set; }

        [Display(Name = "تاریخ انتشار")]
        public DateTime ReleaseDate { get; set; }

        [Display(Name = "مسیر عکس اثر")]
        public string ArtworkPath { get; set; }

        // International Standard Recording Code (ISRC) for musics only
        [Display(Name = "کد ISRC")]
        public string Isrc { get; set; }

        [Display(Name = "شماره مدیا")]
        public int TrackNumber { get; set; }

        [Display(Name = "متن مدیا")]
        public string Lyric { get; set; }

        /*
        [Display(Name = "عنوان آلبوم")]
        public string AlbumTitle { get; set; }

        [Display(Name = "خوانندگان")]
        public string Singers { get; set; }

        public override void CustomMappings(IMappingExpression<Media, MediaResultDto> mapping)
        {
            mapping.ForMember(dest => dest.AlbumTitle,
                config => config.MapFrom(src => src.Album.Title));

            mapping.ForMember(dest => dest.Singers, config =>
                config.MapFrom(src => $"{string.Join(',', src.Album.Artists.Select(a => a.ArtisticName))}"));
        }
        */
    }

    public class MongoMediaUpdateDto : BaseDto<MediaUpdateDto, Entities.MongoDb.Media, string>
    {
        [Display(Name = "نام اثر")]
        [MaxLength(100, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string Title { get; set; }

        [Display(Name = "نوع مدیا")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        public MediaType Type { get; set; }

        [Display(Name = "تاریخ انتشار")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        public DateTime ReleaseDate { get; set; }

        // International Standard Recording Code (ISRC) for musics only
        [Display(Name = "کد ISRC")]
        [MaxLength(100, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string Isrc { get; set; }

        [Display(Name = "شماره مدیا")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        public int TrackNumber { get; set; }

        [Display(Name = "متن مدیا")]
        public string Lyric { get; set; }

        [Display(Name = "آیدی آلبوم اثر")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        public string AlbumId { get; set; }

        [Display(Name = "فایل عکس اثر")]
        [MaxFileSize(3 * 1024 * 1024)]
        [AllowedExtensions(new[] { ".jpg", ".png" })]
        public IFormFile ArtworkFile { get; set; }

        [Display(Name = "فایل مدیا")]
        [MaxFileSize(500 * 1024 * 1024)]
        [AllowedExtensions(new[] { ".mp3", ".mp4", ".aac", ".wav", ".wma", ".avi", ".mkv" })]
        public IFormFile MediaFile { get; set; }
    }

    #endregion
}
