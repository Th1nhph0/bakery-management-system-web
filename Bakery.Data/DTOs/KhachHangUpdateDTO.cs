using System;
using System.Collections.Generic;
using System.Text;

using System.ComponentModel.DataAnnotations;

namespace Bakery.Data.DTOs
{
    public class KhachHangUpdateDTO
    {
        [Required(ErrorMessage = "Tên khách hàng không được để trống")]
        public string TenKhachHang { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        public string Sdt { get; set; } // Khớp thuộc tính Sdt viết hoa chữ S của ông

        public string? Email { get; set; }

        public string? DiaChi { get; set; }
    }
}
