using Bakery.Data.DTOs;
using Bakery.Data.Models;
using Bakery.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bakery.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KhachHangController : ControllerBase
    {
        private readonly KhachHangService _service;
        private readonly BakeryManagementDbContext _context;

        public KhachHangController(KhachHangService service, BakeryManagementDbContext context)
        {
            _context = context;
            _service = service;
        }

        [HttpGet("DanhSachKhachHang")]
        public async Task<IActionResult> GetAllKhachHang()
        {
            return Ok(await _context.KhachHangs.ToListAsync());
        }

        // 🔥 BỔ SUNG: Lấy thông tin 1 khách hàng để đổ vào Form khi Sửa
        [HttpGet("LayKhachHang/{id}")]
        public async Task<IActionResult> GetKhachHangById(int id)
        {
            var kh = await _context.KhachHangs.FindAsync(id);
            if (kh == null) return NotFound(new { Message = "Không tìm thấy khách hàng!" });
            return Ok(kh);
        }

        [HttpPost("TaoKhachHangMoi")]
        public async Task<IActionResult> Create([FromBody] KhachHangCreateDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // 1. TỰ ĐỘNG TẠO MẬT KHẨU NGẪU NHIÊN GỒM 6 CHỮ SỐ (Ví dụ: 582491)
                string matKhauNgauNhien = new Random().Next(100000, 999999).ToString();

                // 2. Map dữ liệu từ DTO sang Model thực thể gốc
                var kh = new KhachHang
                {
                    TenKhachHang = dto.TenKhachHang,
                    Sdt = dto.Sdt,
                    Email = dto.Email,
                    DiaChi = dto.DiaChi,

                    // 🔥 LƯU Ý: Ông kiểm tra xem trong class 'KhachHang' của ông 
                    // thuộc tính này viết là 'MatKhau' hay 'Mat_Khau' để gõ cho đúng nha!
                    MatKhau = matKhauNgauNhien
                };

                // 3. Gọi hàm service xử lý lưu xuống Database như bình thường
                var result = await _service.ThemKhachHangMoiAsync(kh);

                // 4. Trả thêm trường 'matKhauTuTao' về để Swagger/Frontend hiển thị nếu cần
                return Ok(new
                {
                    Success = true,
                    Message = "Thêm khách hàng thành công!",
                    MatKhauTuTao = matKhauNgauNhien,
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpGet("TimKiemKhachHang/{sdt}")]
        public async Task<IActionResult> GetBySdt(string sdt)
        {
            var kh = await _service.TimKhachHangTheoSDTAsync(sdt);
            if (kh == null) return NotFound(new { Message = "Khách hàng mới" });
            return Ok(kh);
        }
        // 🔥 ĐỒNG BỘ: Đổi sang nhận KhachHangUpdateDTO để chặt đứt vòng lặp liên kết bảng
        [HttpPut("CapNhatThongTinKhachHang/{id}")]
        public async Task<IActionResult> CapNhatKhachHang(int id, [FromBody] KhachHangUpdateDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 1. Tìm khách hàng cũ trong DB
            var kh = await _context.KhachHangs.FindAsync(id);
            if (kh == null) return NotFound(new { Message = "Không tìm thấy khách hàng!" });

            // 2. Đổ dữ liệu phẳng từ DTO vào thực thể Model gốc để cập nhật
            kh.TenKhachHang = dto.TenKhachHang;
            kh.Sdt = dto.Sdt;
            kh.DiaChi = dto.DiaChi;
            kh.Email = dto.Email;

            try
            {
                // 3. Lưu lại thay đổi vào SQL Server
                await _context.SaveChangesAsync();
                return Ok(new { Success = true, Message = "Cập nhật thông tin khách hàng thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống khi lưu: " + ex.InnerException?.Message ?? ex.Message });
            }
        }

        // 🔥 BỔ SUNG: API Lấy lịch sử mua hàng dựa vào ID khách hàng
        [HttpGet("LichSuMuaHang/{id}")]
        public async Task<IActionResult> GetLichSuMuaHang(int id)
        {
            // Tìm tất cả đơn hàng thuộc về id khách hàng này (ông nhớ chỉnh chữ DonHangs cho khớp với DbSet của ông nha)
            var lichSu = await _context.DonHangs
                .Where(dh => dh.KhachHangId == id)
                .OrderByDescending(dh => dh.DonHangId)
                .ToListAsync();

            return Ok(lichSu);
        }
    }
}