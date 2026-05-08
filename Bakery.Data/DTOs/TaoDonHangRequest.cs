using System;
using System.Collections.Generic;
using System.Text;

namespace Bakery.Data.DTOs
{
    // Đại diện cho 1 món hàng trong giỏ
    public class GioHangItemDTO
    {
        public int SanPham_ID { get; set; }
        public int So_Luong { get; set; }
        public decimal Don_Gia { get; set; }
    }

    // Đại diện cho toàn bộ thông tin Đơn hàng khách gửi lên
    public class TaoDonHangRequest
    {
        public int? Khach_Hang_ID { get; set; }
        public int? Nhan_Vien_ID { get; set; }
        public int? Khuyen_Mai_ID { get; set; }

        // Thêm dấu ? vào kiểu string để cho phép null
        public string? Ten_Nguoi_Nhan { get; set; }
        public string? SDT_Nguoi_Nhan { get; set; }
        public string? Dia_Chi_Giao { get; set; }

        public List<GioHangItemDTO> ChiTietGioHang { get; set; } = new List<GioHangItemDTO>();
    }
}
