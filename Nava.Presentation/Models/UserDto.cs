using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nava.Entities.User;
using Nava.Presentation.Models.Validations;
using Nava.WebFramework.Api;

namespace Nava.Presentation.Models
{
    public class UserDto : BaseDto<UserDto, User>, IValidatableObject
    {
        [Display(Name = "نام کاربری")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        [MaxLength(100, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string UserName { get; set; }

        [Display(Name = "رمز عبور")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        [MaxLength(100, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string Password { get; set; }

        [Display(Name = "نام کامل")]
        [Required(ErrorMessage = "لطفا {0} را وارد کنید")]
        [MaxLength(100, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string FullName { get; set; }

        [Display(Name = "تصویر آواتار")]
        [MaxFileSize(1 * 1024 * 1024)]
        [AllowedExtensions(new[] { ".jpg", ".png" })]
        public IFormFile AvatarFile { get; set; }

        [Display(Name = "بیو")]
        [MaxLength(150, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string Bio { get; set; }

        // Business Validation Errors
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (UserName.Equals("test", StringComparison.OrdinalIgnoreCase))
                yield return new ValidationResult(
                    "نام کاربری  نمی تواند test باشد",
                    new[] { nameof(UserName) });

            if (Password.Equals("123456"))
                yield return new ValidationResult(
                    "رمز عبور  نمی تواند 123456 باشد",
                    new[] { nameof(Password) });
        }
    }

    public class UserResultDto : BaseDto<UserResultDto, User>
    {
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string AvatarPath { get; set; }
        public string Bio { get; set; }
    }

    public class UserUpdateDto : BaseDto<UserUpdateDto, User>
    {
        [Display(Name = "نام کاربری")]
        [MaxLength(100, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string UserName { get; set; }

        [Display(Name = "رمز عبور جدید")]
        [MaxLength(100, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string NewPassword { get; set; }

        [Display(Name = "تکرار رمز عبور جدید")]
        [MaxLength(100, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        [Compare(nameof(NewPassword), ErrorMessage = "{0} با {1} مطابقت ندارد.")]
        public string RepeatNewPassword { get; set; }

        [Display(Name = "رمز عبور قبلی")]
        [MaxLength(100, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string CurrentPassword { get; set; }

        [Display(Name = "نام کامل")]
        [MaxLength(100, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string FullName { get; set; }

        [Display(Name = "تصویر آواتار")]
        [MaxFileSize(1 * 1024 * 1024)]
        [AllowedExtensions(new[] { ".jpg", ".png" })]
        public IFormFile AvatarFile { get; set; }

        [Display(Name = "بیو")]
        [MaxLength(150, ErrorMessage = "{0} نمی تواند بیشتر از {1} کاراکتر باشد")]
        public string Bio { get; set; }
    }
}
