using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bakery.Data.Models;

public partial class DonHang
{
    public int DonHangId { get; set; }

    public string? MaDhHienThi { get; set; }

    public int? KhachHangId { get; set; }

    public int? NhanVienId { get; set; }

    public int? KhuyenMaiId { get; set; }

    public string TenNguoiNhan { get; set; } = null!;

    public string SdtNguoiNhan { get; set; } = null!;

    public string DiaChiGiao { get; set; } = null!;

    public DateTime? NgayDatHang { get; set; }

    public string? TrangThai { get; set; }

    public decimal? TongTien { get; set; }

    public decimal? SoTienGiam { get; set; }

    public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; } = new List<ChiTietDonHang>();

    public virtual DonBanhCustom? DonBanhCustom { get; set; }

    public virtual KhachHang? KhachHang { get; set; }

    public virtual KhuyenMai? KhuyenMai { get; set; }

    public virtual NhanVien? NhanVien { get; set; }

    [Column("Nguoi_Cap_Nhat")]
    public string? NguoiCapNhat { get; set; }

    [Column("Ngay_Cap_Nhat")]
    public DateTime? NgayCapNhat { get; set; }
}
