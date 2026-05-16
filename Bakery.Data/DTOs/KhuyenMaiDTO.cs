using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace Bakery.Data.DTOs
{
    public class KhuyenMaiDTO
    {
        [Required(ErrorMessage = "Mã code không được để trống")]
        public string MaCode { get; set; }

        [Range(1, 100, ErrorMessage = "Phần trăm giảm phải từ 1 đến 100%")]
        public int PhanTramGiam { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu không được để trống")]
        public DateTime NgayBatDau { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc không được để trống")]
        public DateTime NgayKetThuc { get; set; }
    }
}
