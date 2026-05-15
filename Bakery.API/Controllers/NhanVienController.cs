using Bakery.API.DTOs;
using Bakery.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
// using Bakery.Data.Models; // Mở comment dòng này và sửa theo tên Project của ông
// using Bakery.API.DTOs;

namespace Bakery.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NhanVienController : ControllerBase
    {
        private readonly BakeryManagementDbContext _context;

        public NhanVienController(BakeryManagementDbContext context)
        {
            _context = context;
        }

        [HttpGet("HienThiDanhSachNhanVien")]
        public async Task<IActionResult> LayDanhSachNhanVien()
        {
            var list = await _context.NhanViens.ToListAsync();
            return Ok(list);
        }

        [HttpPost("ThemNhanVienMoi")]
        public async Task<IActionResult> TaoNhanVienMoi([FromBody] NhanVienDTO request)
        {
            try
            {
                // 1. Kiểm tra xem Email có bị trùng không (Rất quan trọng để không bị lỗi SQL)
                var emailTonTai = await _context.NhanViens.AnyAsync(nv => nv.Email == request.Email);
                if (emailTonTai)
                {
                    return BadRequest(new { Message = "Email này đã có người xài rồi ông ơi!" });
                }

                // 2. Chuyển từ DTO sang Model thực tế của Database
                var nvMoi = new NhanVien
                {
                    TenNhanVien = request.TenNhanVien,
                    Sdt = request.Sdt,
                    Email = request.Email,
                    MatKhau = request.MatKhau,
                    ChucVu = request.ChucVu,
                    NgayVaoLam = DateTime.Now
                };

                // 3. Đẩy xuống SQL Server
                _context.NhanViens.Add(nvMoi);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Tạo nhân viên thành công rực rỡ!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống C#: " + ex.Message });
            }
        }

        [HttpPut("CapNhatNhanVien/{id}")]
        public async Task<IActionResult> CapNhatNhanVien(int id, [FromBody] NhanVienDTO request)
        {
            var nv = await _context.NhanViens.FindAsync(id);
            if (nv == null) return NotFound();

            nv.TenNhanVien = request.TenNhanVien;
            nv.Sdt = request.Sdt;
            nv.Email = request.Email;
            nv.ChucVu = request.ChucVu;
            // Nếu pass không đổi thì thôi, hoặc cập nhật luôn tùy ông
            if (!string.IsNullOrEmpty(request.MatKhau)) nv.MatKhau = request.MatKhau;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Cập nhật xong rồi nha!" });
        }

        [HttpDelete("XoaNhanVien/{id}")]
        public async Task<IActionResult> XoaNhanVien(int id)
        {
            var nv = await _context.NhanViens.FindAsync(id);
            if (nv == null) return NotFound();

            _context.NhanViens.Remove(nv);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Đã tiễn nhân viên lên đường!" });
        }
    }
}