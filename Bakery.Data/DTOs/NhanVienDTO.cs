using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;


namespace Bakery.API.DTOs
{
    public class NhanVienDTO // & KhachHangDTO cũng y chang
    {
        [Required(ErrorMessage = "Tên không được để trống!")]
        public string TenNhanVien { get; set; }

        [Required(ErrorMessage = "Email không được để trống!")]
        [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ (phải có @gmail.com...)")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống!")]
        [RegularExpression(@"^0[0-9]{9}$", ErrorMessage = "Số điện thoại phải bắt đầu bằng số 0 và đủ 10 số!")]
        public string Sdt { get; set; }

        public string? MatKhau { get; set; }
        public string? ChucVu { get; set; }
        public string? RoleNguoiSua { get; set; }

        public string? MatKhauCu { get; set; }
    }
}
