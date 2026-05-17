using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;


namespace Bakery.Data.DTOs
{
    public class UpdateDonBanhCustom
    {
        [Required(ErrorMessage = "Loại yêu cầu không được xóa trống!")]
        public string Loai_Yeu_Cau { get; set; }

        [Required(ErrorMessage = "Kích thước không được xóa trống!")]
        public string Kich_Thuoc_So_Luong { get; set; }

        [Required(ErrorMessage = "Nhân bánh (cốt bánh) phải có!")]
        public string Nhan_Banh { get; set; }

        public string Mau_Sac { get; set; } // Màu sắc có thể để trống (tự do)
        public string Ghi_Chu { get; set; }
    }
}
