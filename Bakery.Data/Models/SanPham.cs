using System;
using System.Collections.Generic;

namespace Bakery.Data.Models;

public partial class SanPham
{
    public int SanPhamId { get; set; }

    public string? MaSpHienThi { get; set; }

    public string TenSanPham { get; set; } = null!;

    public string? PhanLoai { get; set; }

    public decimal? DonGiaBan { get; set; }

    public string? MoTa { get; set; }

    public int? SoLuongTon { get; set; }

    public string? HinhAnh { get; set; }

    public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; } = new List<ChiTietDonHang>();
}
