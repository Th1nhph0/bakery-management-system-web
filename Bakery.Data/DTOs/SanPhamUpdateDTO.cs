using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;


namespace Bakery.Data.DTOs
{
    public class SanPhamUpdateDTO
    {
        [Required(ErrorMessage = "Tên sản phẩm không được để trống!")]
        [StringLength(200, ErrorMessage = "Tên bánh quá dài!")]
        public string TenSanPham { get; set; }

        [Required]
        [Range(1000, double.MaxValue, ErrorMessage = "Giá bán bánh tối thiểu phải từ 1,000 đ!")]
        public decimal GiaBan { get; set; }

        [Required]
        [Range(0, 10000, ErrorMessage = "Số lượng tồn kho không được âm!")]
        public int SoLuong { get; set; }

        [Required(ErrorMessage = "Phải chọn phân loại bánh!")]
        public string PhanLoai { get; set; }

        public string? MoTa { get; set; }

        public string? HinhAnh { get; set; }

        public string? RoleNguoiSua { get; set; }

    }
}
