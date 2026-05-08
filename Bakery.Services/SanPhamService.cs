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
    }
}
