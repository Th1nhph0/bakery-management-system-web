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
        public async Task<KhachHang> ThemKhachHangMoiAsync(KhachHang kh)
        {
            _context.KhachHangs.Add(kh);
            await _context.SaveChangesAsync();
            return kh;
        }
    }
}