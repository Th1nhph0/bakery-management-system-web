using System;
using System.Collections.Generic;

namespace Bakery.Data.Models;

public partial class KhachHang
{
    public int KhachHangId { get; set; }

    public string? MaKhHienThi { get; set; }

    public string TenKhachHang { get; set; } = null!;

    public string Sdt { get; set; } = null!;

    public string? DiaChi { get; set; }

    public string? Email { get; set; }

    public string MatKhau { get; set; } = null!;

    public virtual ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();
}
