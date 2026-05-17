using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;


namespace Bakery.Data.DTOs
{
    public class UpdateDonBanhCustom
    {
        public string Loai_Yeu_Cau { get; set; }
        public string Kich_Thuoc_So_Luong { get; set; }
        public string Nhan_Banh { get; set; }
        public string Mau_Sac { get; set; }
        public string Ghi_Chu { get; set; }
    }
}
