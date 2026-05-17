using Bakery.Data.Models;
using Bakery.Data.DTOs; // Nạp thư mục DTO vào
using Bakery.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bakery.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KhuyenMaiController : ControllerBase
    {
        private readonly BakeryManagementDbContext _context;
        private readonly KhuyenMaiService _service;

        public KhuyenMaiController(KhuyenMaiService service, BakeryManagementDbContext context)
        {
            _context = context;
            _service = service;
        }

        // 1. LẤY DANH SÁCH (Giữ đúng tên hàm của ông)
        [HttpGet("HienThiCacMaKhuyenMai")]
        public async Task<IActionResult> GetAllKhuyenMai()
        {
            return Ok(await _context.KhuyenMais.ToListAsync());
        }

        // 🔥 BỔ SUNG: API lấy chi tiết 1 mã theo ID để phục vụ nút Sửa trên giao diện
        [HttpGet("LayKhuyenMai/{id}")]
        public async Task<IActionResult> GetKhuyenMaiById(int id)
        {
            var km = await _context.KhuyenMais.FindAsync(id);
            if (km == null) return NotFound(new { Message = "Không tìm thấy mã!" });
            return Ok(km);
        }

        // 2. THÊM MỚI (Đổi sang dùng DTO để chặn lỗi 400/500 vòng lặp)
        [HttpPost("TaoKhuyenMaiMoi")]
        public async Task<IActionResult> TaoKhuyenMai([FromBody] KhuyenMaiDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var km = new KhuyenMai
            {
                MaCode = dto.MaCode,
                PhanTramGiam = dto.PhanTramGiam,
                NgayBatDau = dto.NgayBatDau,
                NgayKetThuc = dto.NgayKetThuc
            };

            _context.KhuyenMais.Add(km);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Tạo mã khuyến mãi thành công!", Data = km });
        }

        // 3. CẬP NHẬT (Vá lỗi thiếu dấu / ở route và đổi sang dùng DTO)
        [HttpPut("CapNhatKhuyenMai/{id}")]
        public async Task<IActionResult> CapNhatKhuyenMai(int id, [FromBody] KhuyenMaiDTO kmCapNhat)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var km = await _context.KhuyenMais.FindAsync(id);
            if (km == null) return NotFound(new { Message = "Không tìm thấy mã khuyến mãi!" });

            km.MaCode = kmCapNhat.MaCode;
            km.PhanTramGiam = kmCapNhat.PhanTramGiam;
            km.NgayBatDau = kmCapNhat.NgayBatDau;
            km.NgayKetThuc = kmCapNhat.NgayKetThuc;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Đã cập nhật mã khuyến mãi!" });
        }

        // 4. XÓA (Vá lỗi thiếu dấu / ở route)
        [HttpDelete("XoaKhuyenMai/{id}")]
        public async Task<IActionResult> XoaKhuyenMai(int id)
        {
            var km = await _context.KhuyenMais.FindAsync(id);
            if (km == null) return NotFound();

            try
            {
                _context.KhuyenMais.Remove(km);
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Đã xóa mã khuyến mãi!" });
            }
            catch (Exception)
            {
                return BadRequest(new { Message = "Mã này đã được khách sử dụng trong đơn hàng, không thể xóa!" });
            }
        }

        // 5. CHECK MÃ CODE (Vá lỗi thiếu dấu / ở route)
        [HttpGet("KiemTraMaCodeKhuyenMai/{tenCode}")]
        public async Task<IActionResult> KiemTraMaGiamGia(string tenCode)
        {
            try
            {
                var result = await _service.LayThongTinKhuyenMaiAsync(tenCode);
                if (result == null)
                {
                    return NotFound(new { Success = false, Message = "Mã khuyến mãi không tồn tại!" });
                }
                return Ok(new { Success = true, Message = "Áp dụng mã thành công!", Data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        // 6. TÍNH TOÁN TIỀN GIẢM (Giữ nguyên gốc của ông)
        [HttpGet("KiemTraTinhToanKhuyenMai")]
        public async Task<IActionResult> KiemTraVaTinhTienGiam([FromQuery] string tenCode, [FromQuery] decimal tongTien)
        {
            try
            {
                var result = await _service.CheckVaApDungKhuyenMaiAsync(tenCode, tongTien);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { HopLe = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}