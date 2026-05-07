using System;
using System.Collections.Generic;

namespace Bakery.Data.Models;

public partial class VTraCuuDonHang
{
    public int DonHangId { get; set; }

    public string? MaDhHienThi { get; set; }

    public string TenKhachMua { get; set; } = null!;

    public string SdtNguoiNhan { get; set; } = null!;

    public DateTime? NgayDatHang { get; set; }

    public string? TrangThai { get; set; }

    public decimal? TongTien { get; set; }

    public decimal? SoTienGiam { get; set; }

    public decimal? ThanhTienThucTe { get; set; }

    public string? NhanVienTiepNhan { get; set; }
}
