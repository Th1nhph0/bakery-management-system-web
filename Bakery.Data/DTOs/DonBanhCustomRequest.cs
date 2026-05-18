using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Bakery.Data.DTOs
{
     public class DonBanhCustomRequest
    {
        // Thông tin đơn hàng cơ bản
        public int? Khach_Hang_ID { get; set; }
        public string? Ten_Nguoi_Nhan { get; set; }
        public string? SDT_Nguoi_Nhan { get; set; }
        public string? Dia_Chi_Giao { get; set; }

        // Thông tin đặc thù của bánh Custom
        public string? Mo_Ta_Yeu_Cau { get; set; } // Ví dụ: Bánh 2 tầng, ghi chữ "Chúc mừng sinh nhật Thịnh"
        public string? Kich_Thuoc { get; set; }
        public string? Mau_Sac { get; set; }
        public DateTime Ngay_Lay_Banh { get; set; }
        public decimal Gia_Du_Kien { get; set; }
    }
}