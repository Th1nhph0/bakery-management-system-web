using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Bakery.Data.DTOs
{
    public class KhachHangCreateDTO
    {
        [Required(ErrorMessage = "Tên khách hàng không được để trống")]
        public string TenKhachHang { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        public string Sdt { get; set; } // Đặt chữ Sdt viết hoa chữ S khớp chuẩn DB của ông nhé

        public string? Email { get; set; }

        public string? DiaChi { get; set; }
    }
}
