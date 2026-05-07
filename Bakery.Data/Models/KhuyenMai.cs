using System;
using System.Collections.Generic;

namespace Bakery.Data.Models;

public partial class KhuyenMai
{
    public int KhuyenMaiId { get; set; }

    public string MaCode { get; set; } = null!;

    public int? PhanTramGiam { get; set; }

    public DateTime NgayBatDau { get; set; }

    public DateTime NgayKetThuc { get; set; }

    public virtual ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();
}
