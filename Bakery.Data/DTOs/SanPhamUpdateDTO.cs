using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;


namespace Bakery.Data.DTOs
{
    public class SanPhamUpdateDTO
    {
        // include both property names to match different usages in controllers
        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        [StringLength(100, ErrorMessage = "Tên sản phẩm không được quá 100 ký tự")]
        public string TenSanPham { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá bán phải lớn hơn hoặc bằng 0")]
        public decimal GiaBan { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn phải lớn hơn hoặc bằng 0")]
        public int SoLuong { get; set; }

        [Required(ErrorMessage = "Phân loại không được để trống")]
        public string PhanLoai { get; set; }

        public string? MoTa { get; set; }

        public string? HinhAnh { get; set; } // Cho phép null nếu họ không muốn sửa ảnh cũ
    }
}
