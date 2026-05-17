using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;


namespace Bakery.Data.DTOs
{
    public class KhuyenMaiDTO : IValidatableObject
    {
        [Required(ErrorMessage = "Mã code không được để trống!")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Mã code phải từ 3 - 20 ký tự!")]
        public string MaCode { get; set; }

        [Required]
        [Range(1, 100, ErrorMessage = "Phần trăm giảm giá chỉ được từ 1% đến 100%!")]
        public int PhanTramGiam { get; set; } = 0;

        [Required(ErrorMessage = "Ngày bắt đầu không được để trống!")]
        public DateTime NgayBatDau { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc không được để trống!")]
        public DateTime NgayKetThuc { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (NgayKetThuc <= NgayBatDau)
            {
                yield return new ValidationResult("Ngày kết thúc khuyến mãi phải lớn hơn ngày bắt đầu!", new[] { nameof(NgayKetThuc) });
            }
        }
    }
}
