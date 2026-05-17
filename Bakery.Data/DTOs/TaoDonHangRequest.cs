using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations; // 🔥 BẮT BUỘC CÓ THƯ VIỆN NÀY ĐỂ KÍCH HOẠT BỘ LỌC

namespace Bakery.Data.DTOs
{
   
    public class GioHangItemDTO
    {
        [Required(ErrorMessage = "Mã sản phẩm không được để trống!")]
        public int SanPham_ID { get; set; }

        // 🔢 Số lượng bắt buộc phải từ 1 cái trở lên, không được mua 0 cái hoặc số âm
        [Required(ErrorMessage = "Số lượng không được để trống!")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng bánh đặt mua phải từ 1 cái trở lên!")]
        public int So_Luong { get; set; }

        // 💰 Đơn giá gốc của bánh không được là số âm
        [Required(ErrorMessage = "Đơn giá không được để trống!")]
        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá sản phẩm không được là số âm!")]
        public decimal Don_Gia { get; set; }
    }

    // ==========================================
    // 📋 RÀNG BUỘC CHO TOÀN BỘ ĐƠN HÀNG TẠO MỚI
    // ==========================================
    public class TaoDonHangRequest
    {
        public int? Khach_Hang_ID { get; set; }
        public int? Nhan_Vien_ID { get; set; }
        public int? Khuyen_Mai_ID { get; set; }

        [Required(ErrorMessage = "Tên người nhận hàng không được để trống!")]
        [StringLength(100, ErrorMessage = "Tên người nhận không được dài quá 100 ký tự!")]
        public string? Ten_Nguoi_Nhan { get; set; }

        // 📞 KIỂM TRA SỐ ĐIỆN THOẠI: Bắt buộc đầu số 0, đủ 10 chữ số chuẩn Việt Nam
        [Required(ErrorMessage = "Số điện thoại người nhận không được để trống!")]
        [RegularExpression(@"^0[0-9]{9}$", ErrorMessage = "Số điện thoại không hợp lệ! Phải bắt đầu bằng số 0 và có đúng 10 chữ số.")]
        public string? SDT_Nguoi_Nhan { get; set; }

        [Required(ErrorMessage = "Địa chỉ giao bánh không được để trống!")]
        [StringLength(500, ErrorMessage = "Địa chỉ giao hàng quá dài (Tối đa 500 ký tự)!")]
        public string? Dia_Chi_Giao { get; set; }

        public List<GioHangItemDTO> ChiTietGioHang { get; set; } = new List<GioHangItemDTO>();

        // Các trường bổ sung nếu gạt chọn đơn Custom
        public bool LaDonCustom { get; set; }
        public string? LoaiYeuCau { get; set; }
        public string? KichThuocSoluong { get; set; }
        public string? MauSacChuDao { get; set; }
        public string? GhiChu { get; set; }
        public DateTime? NgayLayHang { get; set; }
        public string? HinhAnh { get; set; }

        // 💰 KIỂM TRA BÁO GIÁ CUSTOM: Nếu gạt đơn Custom và điền tiền thì cấm điền số âm
        [Range(0, double.MaxValue, ErrorMessage = "Số tiền báo giá bánh Custom không được là số âm!")]
        public decimal? TongTienCustom { get; set; }

        public string? TenNhanVienCapNhat { get; set; }
    }
}