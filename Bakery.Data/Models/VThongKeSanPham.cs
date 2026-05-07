using System;
using System.Collections.Generic;

namespace Bakery.Data.Models;

public partial class VThongKeSanPham
{
    public int SanPhamId { get; set; }

    public string? MaSpHienThi { get; set; }

    public string TenSanPham { get; set; } = null!;

    public string? PhanLoai { get; set; }

    public decimal? DonGiaBan { get; set; }

    public int? SoLuongTon { get; set; }

    public int TongSoLuongDaBan { get; set; }
}
