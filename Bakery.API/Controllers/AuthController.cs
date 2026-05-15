using Bakery.Data.DTOs;
using Bakery.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
// Nhớ using thư mục chứa DbContext và LoginDTO của ông vào đây nha

namespace Bakery.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly BakeryManagementDbContext _context;

        public AuthController(BakeryManagementDbContext context)
        {
            _context = context;
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO request)
        {
            // Vào bảng NhanVien tìm xem có ai khớp Email và Mật khẩu không
            // Tạm thời so sánh chuỗi chay cho lẹ, không xài Hashing để kịp deadline
            var nhanVien = await _context.NhanViens
                .FirstOrDefaultAsync(nv => nv.Email == request.Email && nv.MatKhau == request.MatKhau);

            if (nhanVien == null)
            {
                // Nếu tìm không thấy -> Đuổi về (Status 401 Unauthorized)
                return Unauthorized(new { Message = "Sai email hoặc mật khẩu rồi ông ơi!" });
            }

            // Nếu đúng -> Cho vào nhà và cấp thông tin để JS lưu vào LocalStorage
            return Ok(new
            {
                id = nhanVien.NhanVienId,
                ten = nhanVien.TenNhanVien,
                // Chỗ này gán cứng là Admin cho dễ test, hoặc lấy từ nhanVien.Chuc_Vu của ông
                role = "Admin"
            });
        }
    }
}