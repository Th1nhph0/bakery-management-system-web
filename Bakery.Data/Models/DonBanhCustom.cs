using System;
using System.Collections.Generic;

namespace Bakery.Data.Models;

public partial class DonBanhCustom
{
    public int CustomId { get; set; }

    public string? MaCustomHienThi { get; set; }

    public int DonHangId { get; set; }

    public string? LoaiYeuCau { get; set; }

    public string? KichThuocSoLuong { get; set; }

    public string? NhanBanh { get; set; }

    public string? MauSacChuDao { get; set; }

    public string? GhiChu { get; set; }

    public DateTime NgayLayHang { get; set; }

    public virtual DonHang DonHang { get; set; } = null!;
}
