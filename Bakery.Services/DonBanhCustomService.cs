using Bakery.Data.DTOs;
using Bakery.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Bakery.Services
{
    public class DonBanhCustomService
    {
        private readonly BakeryManagementDbContext _context;

        public DonBanhCustomService(BakeryManagementDbContext context)
        {
            _context = context;
        }

        public async Task<int> LuuDonBanhCustomAsync(DonBanhCustomRequest request)
        {
            // Sử dụng Transaction để đảm bảo nếu lưu bảng 2 lỗi thì bảng 1 phải hủy
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Tạo Đơn hàng tổng quát trước
                var donHang = new DonHang
                {
                    KhachHangId = request.Khach_Hang_ID,
                    NgayDatHang = DateTime.Now,
                    TenNguoiNhan = request.Ten_Nguoi_Nhan,
                    SdtNguoiNhan = request.SDT_Nguoi_Nhan,
                    DiaChiGiao = request.Dia_Chi_Giao,
                    TrangThai = "Chờ duyệt",
                    TongTien = request.Gia_Du_Kien,
                    SoTienGiam = 0
                };

                _context.DonHangs.Add(donHang);
                await _context.SaveChangesAsync(); // Lưu để lấy được donHang.DonHangId tự tăng

                // 2. Lưu vào bảng DonBanhCustom với link DonHangId vừa tạo
                var donCustom = new DonBanhCustom
                {
                    DonHangId = donHang.DonHangId, 
                    LoaiYeuCau = "Đặt bánh custom",
                    GhiChu = request.Mo_Ta_Yeu_Cau,
                    KichThuocSoLuong = request.Kich_Thuoc,
                    MauSacChuDao = request.Mau_Sac,
                    NgayLayHang = request.Ngay_Lay_Banh,
                    
                };

                _context.DonBanhCustoms.Add(donCustom);
                await _context.SaveChangesAsync();

                // Hoàn tất giao dịch
                await transaction.CommitAsync();

                return donHang.DonHangId;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        // Hàm tính toán giá bánh custom (STT 4)
        public decimal TinhTienBanhCustom(string kichThuoc, string yeuCauThietKe)
        {
            decimal giaGoc = 250000; // Giá cốt bánh cơ bản mặc định
            decimal phuPhiSize = 0;
            decimal phuPhiThietKe = 0;

            // 1. Logic tính phụ phí theo Size
            if (!string.IsNullOrEmpty(kichThuoc))
            {
                string size = kichThuoc.ToLower();
                if (size.Contains("l") || size.Contains("20cm") || size.Contains("lớn"))
                    phuPhiSize = 100000;
                else if (size.Contains("m") || size.Contains("16cm") || size.Contains("vừa"))
                    phuPhiSize = 50000;
            }

            // 2. Logic tính phụ phí Thiết kế (Bắt từ khóa)
            if (!string.IsNullOrEmpty(yeuCauThietKe))
            {
                string yeuCau = yeuCauThietKe.ToLower();
                if (yeuCau.Contains("2 tầng") || yeuCau.Contains("vẽ hình") || yeuCau.Contains("phức tạp"))
                    phuPhiThietKe = 200000;
                else if (yeuCau.Contains("ghi chữ") || yeuCau.Contains("đơn giản") || yeuCau.Contains("hoa"))
                    phuPhiThietKe = 50000;
            }

            // Trả về tổng tiền
            return giaGoc + phuPhiSize + phuPhiThietKe;
        }
    }
}
