using System.ComponentModel.DataAnnotations;

namespace Nava.Common
{
    public enum ApiResultStatusCode
    {
        [Display(Name = "عملیات با موفقیت انجام شد")]
        Success = 0,

        [Display(Name = "خطایی در سرور رخ داده است")]
        ServerError = 1,

        [Display(Name = "پارامترهای ارسالی معتبر نمی باشند")]
        BadRequest = 2,

        [Display(Name = "یافت نشد")]
        NotFound = 3,

        [Display(Name = "لیست خالی است")]
        ListEmpty = 4,

        [Display(Name = "احراز هویت انجام نشده است")]
        UnAuthorized = 5,

        [Display(Name = "خطایی در پردازش رخ داده است")]
        LogicError = 6
    }
}