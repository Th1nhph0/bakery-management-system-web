using Bakery.Data.Models;
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

        [HttpGet("HienThiCacMaKhuyenMai")]
        public async Task<IActionResult> GetAllKhuyenMai()
        {
            return Ok(await _context.KhuyenMais.ToListAsync());
        }


        [HttpPost("TaoKhuyenMaiMoi")]
        public async Task<IActionResult> TaoKhuyenMai([FromBody] KhuyenMai km)
        {
            _context.KhuyenMais.Add(km);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Tạo mã khuyến mãi thành công!", Data = km });
        }


        [HttpPut("CapNhatKhuyenMai{id}")]
        public async Task<IActionResult> CapNhatKhuyenMai(int id, [FromBody] KhuyenMai kmCapNhat)
        {
            var km = await _context.KhuyenMais.FindAsync(id);
            if (km == null) return NotFound();

            km.MaCode = kmCapNhat.MaCode;
            km.PhanTramGiam = kmCapNhat.PhanTramGiam;
            km.NgayBatDau = kmCapNhat.NgayBatDau;
            km.NgayKetThuc = kmCapNhat.NgayKetThuc;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Đã cập nhật mã khuyến mãi!" });
        }

        [HttpGet("KiemTraMaCodeKhuyenMai{tenCode}")]
        public async Task<IActionResult> KiemTraMaGiamGia(string tenCode)
        {
            try
            {
                var result = await _service.LayThongTinKhuyenMaiAsync(tenCode);

                if (result == null)
                {
                    return NotFound(new { Success = false, Message = "Mã khuyến mãi không tồn tại!" });
                }

                // Nếu có check thêm ngày hết hạn thì có thể xử lý ở đây
                return Ok(new { Success = true, Message = "Áp dụng mã thành công!", Data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpGet("KiemTraTinhToanKhuyenMai")]
        public async Task<IActionResult> KiemTraMaGiamGia([FromQuery] string tenCode, [FromQuery] decimal tongTien)
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

        [HttpDelete("XoaKhuyenMai{id}")]
        public async Task<IActionResult> XoaKhuyenMai(int id)
        {
            var km = await _context.KhuyenMais.FindAsync(id);
            if (km == null) return NotFound();

            _context.KhuyenMais.Remove(km);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Đã xóa mã khuyến mãi!" });
        }
    }
}