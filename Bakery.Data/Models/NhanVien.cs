using System;
using System.Collections.Generic;

namespace Bakery.Data.Models;

public partial class NhanVien
{
    public int NhanVienId { get; set; }

    public string? MaNvHienThi { get; set; }

    public string TenNhanVien { get; set; } = null!;

    public string Sdt { get; set; } = null!;

    public string? Email { get; set; }

    public string MatKhau { get; set; } = null!;

    public string? ChucVu { get; set; }

    public DateTime? NgayVaoLam { get; set; }

    public virtual ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();
}
