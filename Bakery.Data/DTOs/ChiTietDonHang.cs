using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // Kích hoạt bộ lọc dữ liệu

namespace Bakery.Data.DTOs
{
    public class ChiTietGioHang
    {
        [Required(ErrorMessage = "Mã sản phẩm không được để trống!")]
        public int SanPham_ID { get; set; }

        [Required(ErrorMessage = "Số lượng không được để trống!")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng bánh đặt mua phải từ 1 cái trở lên!")]
        public int So_Luong { get; set; }
    }

}