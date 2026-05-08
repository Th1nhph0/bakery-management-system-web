using Bakery.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Bakery.Services
{
    public class KhuyenMaiService
    {
        private readonly BakeryManagementDbContext _context;

        public KhuyenMaiService(BakeryManagementDbContext context)
        {
            _context = context;
        }

        // Hàm truy vấn mã giảm giá theo tên code bằng LINQ
        public async Task<KhuyenMai?> LayThongTinKhuyenMaiAsync(string tenCode)
        {
            // TÌM KIẾM BẰNG LINQ:
            // Lưu ý: Bạn kiểm tra lại trong file KhuyenMai.cs xem cột mã code 
            // được đặt tên là gì nhé (có thể là MaCode, TenCode, MaKhuyenMai...)
            // Ở đây tui đang ví dụ là 'TenCode'
            var khuyenMai = await _context.KhuyenMais
                .FirstOrDefaultAsync(km => km.MaCode == tenCode);

            return khuyenMai;
        }
    }
}