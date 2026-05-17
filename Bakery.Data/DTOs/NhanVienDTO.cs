using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;


namespace Bakery.API.DTOs
{
    public class NhanVienDTO
    {
        public string TenNhanVien { get; set; }
        public string Sdt { get; set; }
        public string Email { get; set; }
        public string MatKhau { get; set; }
        public string ChucVu { get; set; }
        public string? MatKhauCu { get; set; }
        public string? RoleNguoiSua { get; set; } 
    }
}
