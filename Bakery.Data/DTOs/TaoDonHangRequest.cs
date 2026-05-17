using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Bakery.Data.DTOs
{
    // Thêm : IValidatableObject vào class
    public class TaoDonHangRequest : IValidatableObject
    {
        public int? Khach_Hang_ID { get; set; }
        public int? Nhan_Vien_ID { get; set; }
        public int? Khuyen_Mai_ID { get; set; }

        [Required(ErrorMessage = "Tên người nhận hàng không được để trống!")]
        [StringLength(100, ErrorMessage = "Tên không quá 100 ký tự!")]
        public string? Ten_Nguoi_Nhan { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống!")]
        [RegularExpression(@"^0[0-9]{9}$", ErrorMessage = "Số điện thoại phải bắt đầu bằng số 0 và đủ 10 chữ số.")]
        public string? SDT_Nguoi_Nhan { get; set; }

        [Required(ErrorMessage = "Địa chỉ giao bánh không được để trống!")]
        public string? Dia_Chi_Giao { get; set; }

        public List<ChiTietGioHang> ChiTietGioHang { get; set; } = new();

        public bool LaDonCustom { get; set; }
        public string? LoaiYeuCau { get; set; }
        public string? KichThuocSoluong { get; set; }
        public string? MauSacChuDao { get; set; }
        public string? GhiChu { get; set; }
        public DateTime? NgayLayHang { get; set; }
        public string? HinhAnh { get; set; }
        public decimal? TongTienCustom { get; set; }
        public string? TenNhanVienCapNhat { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // 1. Ràng buộc nếu là đơn Custom thì KHÔNG ĐƯỢC RỖNG các trường quan trọng
            if (LaDonCustom)
            {
                if (string.IsNullOrWhiteSpace(LoaiYeuCau))
                    yield return new ValidationResult("Vui lòng nhập Loại yêu cầu / Tên bánh Custom!", new[] { nameof(LoaiYeuCau) });

                if (string.IsNullOrWhiteSpace(KichThuocSoluong))
                    yield return new ValidationResult("Kích thước/Quy cách bánh Custom không được để trống!", new[] { nameof(KichThuocSoluong) });

                if (TongTienCustom == null || TongTienCustom <= 0)
                    yield return new ValidationResult("Báo giá bánh Custom phải lớn hơn 0đ. Đừng bán lỗ nhé!", new[] { nameof(TongTienCustom) });

                if (NgayLayHang == null)
                    yield return new ValidationResult("Phải có ngày hẹn khách lấy bánh Custom!", new[] { nameof(NgayLayHang) });
                else if (NgayLayHang.Value.Date < DateTime.Today)
                    yield return new ValidationResult("Ngày lấy bánh không được nằm trong quá khứ!", new[] { nameof(NgayLayHang) });
            }

            // 2. Ràng buộc nếu KHÔNG là đơn Custom thì Giỏ hàng phải có ít nhất 1 món
            if (!LaDonCustom && (ChiTietGioHang == null || ChiTietGioHang.Count == 0))
            {
                yield return new ValidationResult("Giỏ hàng đang trống! Vui lòng chọn bánh tiêu chuẩn hoặc bật chế độ Bánh Custom.", new[] { nameof(ChiTietGioHang) });
            }
        }
    }
}