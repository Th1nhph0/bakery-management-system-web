using Bakery.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;


namespace Bakery.Services
{
    public class SanPhamService
    {
        private readonly BakeryManagementDbContext _context;

        // Tiêm DbContext vào Service
        public SanPhamService(BakeryManagementDbContext context)
        {
            _context = context;
        }

        // Hàm lấy danh sách tất cả sản phẩm
        public async Task<List<SanPham>> GetAllSanPhamAsync()
        {
            return await _context.SanPhams.ToListAsync();
        }
        public async Task<List<SanPham>> TimKiemSanPhamAsync(string tuKhoa)
        {
            // Nếu người dùng không nhập gì, trả về toàn bộ bánh
            if (string.IsNullOrWhiteSpace(tuKhoa))
            {
                return await GetAllSanPhamAsync();
            }

            // Tìm bằng LINQ: Lọc những bánh có tên chứa từ khóa (không phân biệt hoa thường)
            return await _context.SanPhams
                .Where(sp => (sp.TenSanPham != null && sp.TenSanPham.Contains(tuKhoa)) ||
                             (sp.PhanLoai != null && sp.PhanLoai.Contains(tuKhoa)))
                .ToListAsync();
        }
        // ADMIN - Cập nhật thông tin sản phẩm (PUT)
        public async Task<bool> CapNhatSanPhamAsync(int id, SanPham sanPhamCapNhat)
        {
            var sp = await _context.SanPhams.FindAsync(id);
            if (sp == null) return false;

            // Cập nhật các trường thông tin
            sp.TenSanPham = sanPhamCapNhat.TenSanPham;
            sp.PhanLoai = sanPhamCapNhat.PhanLoai;
            // Lưu ý: Nhấn Ctrl + Space để chọn đúng tên cột Giá bán và Mô tả trong DB của bạn nhé
            sp.DonGiaBan = sanPhamCapNhat.DonGiaBan;
            sp.MoTa = sanPhamCapNhat.MoTa;
            sp.SoLuongTon = sanPhamCapNhat.SoLuongTon;

            await _context.SaveChangesAsync();
            return true;
        }

        // ADMIN - Xóa sản phẩm (DELETE)
        public async Task<bool> XoaSanPhamAsync(int id)
        {
            var sp = await _context.SanPhams.FindAsync(id);
            if (sp == null) return false;

            _context.SanPhams.Remove(sp);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<SanPham>> GetFilteredSanPhamsAsync(string? search, string? category, decimal? minPrice)
        {
            var query = _context.SanPhams.AsQueryable();

            // 1. Tìm kiếm theo tên
            if (!string.IsNullOrEmpty(search))
                query = query.Where(sp => sp.TenSanPham.Contains(search));

            // 2. Lọc theo loại (Bánh ngọt, Bánh kem...)
            if (!string.IsNullOrEmpty(category))
                query = query.Where(sp => sp.PhanLoai == category);

            // 3. Lọc theo giá tối thiểu
            if (minPrice.HasValue)
                query = query.Where(sp => sp.DonGiaBan >= minPrice.Value);

            return await query.ToListAsync();
        }
        public async Task DoiTenDanhMucAsync(string tenCu, string tenMoi)
        {
            var dsSanPham = await _context.SanPhams
                .Where(sp => sp.PhanLoai == tenCu)
                .ToListAsync();

            foreach (var sp in dsSanPham)
            {
                sp.PhanLoai = tenMoi;
            }

            await _context.SaveChangesAsync();
        }
        public async Task<List<string>> GetDanhSachDanhMucAsync()
        {
            return await _context.SanPhams
                .Where(sp => sp.PhanLoai != null)
                .Select(sp => sp.PhanLoai!)       
                .Distinct()
                .ToListAsync();
        }
    }
}
