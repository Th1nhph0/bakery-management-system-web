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
                    TrangThai = "Chờ duyệt" //Đc duyệt thì mới làm 
                };

                _context.DonHangs.Add(donHang);
                await _context.SaveChangesAsync(); // Lưu để lấy được donHang.DonHangId tự tăng

                // 2. Lưu vào bảng DonBanhCustom với link DonHangId vừa tạo
                var donCustom = new DonBanhCustom
                {
                    DonHangId = donHang.DonHangId, 
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
    }
}