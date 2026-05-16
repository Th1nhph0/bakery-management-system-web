using System;
using System.Collections.Generic;
using System.Text;

public class CapNhatDonHangTongHopDTO
{
    public int Khach_Hang_ID { get; set; }
    public int? Khuyen_Mai_ID { get; set; }
    public string Ten_Nguoi_Nhan { get; set; } = null!;
    public string SDT_Nguoi_Nhan { get; set; } = null!;
    public string Dia_Chi_Giao { get; set; } = null!;
    public List<ChiTietGioHangGiaoDien> ChiTietGioHang { get; set; } = new();

    // Các trường bổ sung nếu là đơn Custom
    public bool LaDonCustom { get; set; }
    public string? LoaiYeuCau { get; set; }
    public string? KichThuocSoluong { get; set; }
    public string? MauSacChuDao { get; set; }
    public string? GhiChu { get; set; }
    public DateTime? NgayLayHang { get; set; }
    public string? HinhAnh { get; set; }
}

public class ChiTietGioHangGiaoDien
{
    public int SanPham_ID { get; set; }
    public int So_Luong { get; set; }
}
