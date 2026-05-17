using Azure.Core;
using Bakery.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Bakery.Services
{
    public class KhachHangService
    {
        private readonly BakeryManagementDbContext _context;

        public KhachHangService(BakeryManagementDbContext context)
        {
            _context = context;
        }

        // Tìm khách hàng theo SĐT (Để check xem khách đã từng mua chưa)
        public async Task<KhachHang?> TimKhachHangTheoSDTAsync(string sdt)
        {
            return await _context.KhachHangs
                .FirstOrDefaultAsync(kh => kh.Sdt == sdt);
        }

        // Thêm khách hàng mới
        // Nằm trong file KhachHangService.cs
        public async Task<KhachHang> ThemKhachHangMoiAsync(KhachHang kh)
        {
            // 1. Kiểm tra trùng Số điện thoại (Code cũ của ông)
            var khachCungSdt = await _context.KhachHangs.FirstOrDefaultAsync(k => k.Sdt == kh.Sdt);
            if (khachCungSdt != null)
            {
                throw new Exception("Số điện thoại này đã tồn tại trong hệ thống!");
            }

            // 2. 🔥 KIỂM TRA TRÙNG EMAIL (CODE MỚI THÊM VÀO)
            var emailTonTai = await _context.KhachHangs.AnyAsync(k => k.Email == kh.Email);
            if (emailTonTai)
            {
                throw new Exception("Email này đã được sử dụng cho một khách hàng khác! Vui lòng nhập Email khác.");
            }

            // 3. Map dữ liệu từ Request sang Entity
            var khMoi = new KhachHang
            {
                TenKhachHang = kh.TenKhachHang,
                Sdt = kh.Sdt,
                Email = kh.Email,
                DiaChi = kh.DiaChi,

                // Logic tự động gán Mật khẩu bằng chính Số điện thoại
                MatKhau = kh.Sdt
            };

            _context.KhachHangs.Add(khMoi);
            await _context.SaveChangesAsync();

            return khMoi;
        }
        public async Task<string> XoaKhachHangAsync(int khachHangId)
        {
            // 1. Tìm xem khách hàng có tồn tại không
            var kh = await _context.KhachHangs.FindAsync(khachHangId);
            if (kh == null)
            {
                return "Không tìm thấy thông tin khách hàng trong hệ thống!";
            }

            // 2. 🔥 KIỂM TRA RÀNG BUỘC SỐNG CÒN: Khách này đã mua hàng bao giờ chưa?
            // Giả sử bảng hóa đơn của ông tên là DonHangs và có cột Khach_Hang_ID (hoặc KhachHangId)
            bool daCoDonHang = await _context.DonHangs.AnyAsync(dh => dh.KhachHangId == khachHangId);

            if (daCoDonHang)
            {
                return "⚠️ LỖI RÀNG BUỘC: Không thể xóa khách hàng này vì họ đã có lịch sử đặt hàng. Hãy dùng tính năng cập nhật/khóa tài khoản thay vì xóa cứng để bảo toàn sổ sách kế toán!";
            }

            // 3. Nếu chưa mua lần nào -> Qua ải, tiến hành trảm!
            _context.KhachHangs.Remove(kh);
            await _context.SaveChangesAsync();

            return "OK";
        }
    }
}
