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
        // Hàm check hạn sử dụng và tính toán tiền giảm
        public async Task<object> CheckVaApDungKhuyenMaiAsync(string tenCode, decimal tongTienDonHang)
        {
            var khuyenMai = await _context.KhuyenMais.FirstOrDefaultAsync(km => km.MaCode== tenCode);

            // 1. Check mã có tồn tại không
            if (khuyenMai == null)
            {
                return new { HopLe = false, Message = "Mã khuyến mãi không tồn tại!" };
            }

            // 2. Check hạn sử dụng (Giả sử DB có cột NgayBatDau và NgayKetThuc)
            DateTime homNay = DateTime.Now;
            if (homNay < khuyenMai.NgayBatDau || homNay > khuyenMai.NgayKetThuc)
            {
                return new { HopLe = false, Message = "Mã giảm giá đã hết hạn hoặc chưa tới thời gian áp dụng!" };
            }

            // 3. Tính toán tiền giảm
            decimal soTienDuocGiam = tongTienDonHang * ((decimal)khuyenMai.PhanTramGiam / 100);
            decimal tienSauGiam = tongTienDonHang - soTienDuocGiam;

            return new
            {
                HopLe = true,
                Message = "Áp dụng mã thành công!",
                PhanTramGiam = khuyenMai.PhanTramGiam,
                TienGiam = soTienDuocGiam,
                TongTienCuoiCung = tienSauGiam
            };
        }
    }
}