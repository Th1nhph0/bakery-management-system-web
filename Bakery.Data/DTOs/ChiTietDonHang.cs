using System;
using System.Collections.Generic;
using System.Text;

namespace Bakery.Data.DTOs
{
    public class ChiTietGioHangDTO
    {
        public int SanPham_ID { get; set; }
        public int So_Luong { get; set; }
    }

    public class TaoDonHangDTO
    {
        public int Khach_Hang_ID { get; set; }
        public int Nhan_Vien_ID { get; set; }
        public int? Khuyen_Mai_ID { get; set; } // Có thể null nếu không nhập mã
        public string Ten_Nguoi_Nhan { get; set; }
        public string SDT_Nguoi_Nhan { get; set; }
        public string Dia_Chi_Giao { get; set; }

        // Gắn danh sách món ăn vào đây
        public List<ChiTietGioHangDTO> ChiTietGioHang { get; set; }
    }
}
