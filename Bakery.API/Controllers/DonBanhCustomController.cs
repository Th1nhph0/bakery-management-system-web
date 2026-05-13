using Bakery.Data.DTOs;
using Bakery.Data.Models;
using Bakery.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bakery.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DonBanhCustomController : ControllerBase
    {
        private readonly BakeryManagementDbContext _context;
        private readonly DonBanhCustomService _service;

        // Tiêm Service vào Controller
        public DonBanhCustomController(DonBanhCustomService service, BakeryManagementDbContext context)
        {
            _service = service;
            _context = context;
        }

        // Endpoint: POST api/DonBanhCustom/DatBanh
        [HttpPost("DatBanh")]
        public async Task<IActionResult> DatBanhCustom([FromBody] DonBanhCustomRequest request)
        {
            try
            {
                // Gọi sang Service để lưu dữ liệu và lấy mã Đơn hàng (ID) về
                var id = await _service.LuuDonBanhCustomAsync(request);

                // Trả về JSON chuẩn format cho Frontend dễ đọc
                return Ok(new
                {
                    Success = true,
                    Message = "Đã nhận yêu cầu đặt bánh custom thành công!",
                    DonHangID = id
                });
            }
            catch (Exception ex)
            {
                // Trả về mã 500 kèm chi tiết lỗi nếu có sự cố
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Lỗi hệ thống: " + ex.Message
                });
            }
        }
        [HttpGet("TheoDonHang/{donHangId}")]
        public async Task<IActionResult> GetChiTietCustom(int donHangId)
        {
            try
            {
                var thongTinCustom = await _context.DonBanhCustoms
                                           .FirstOrDefaultAsync(c => c.DonHangId == donHangId);

                // Nếu không tìm thấy (lỡ truyền nhầm ID của đơn bánh ngọt bình thường)
                if (thongTinCustom == null)
                {
                    return NotFound(new { Message = "Đơn hàng này không có thông tin yêu cầu Custom!" });
                }

                // Trả về toàn bộ chi tiết: Nhan_Banh, Mau_Sac_Chu_Dao, Ghi_Chu...
                return Ok(thongTinCustom);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
        // PUT: api/DonBanhCustom/{id} (Cập nhật yêu cầu custom)
        [HttpPut("{id}")]
        public async Task<IActionResult> CapNhatCustom(int id, [FromBody] DonBanhCustom customCapNhat)
        {
            var custom = await _context.DonBanhCustoms.FindAsync(id);
            if (custom == null) return NotFound();

            custom.LoaiYeuCau = customCapNhat.LoaiYeuCau;
            custom.KichThuocSoLuong = customCapNhat.KichThuocSoLuong;
            custom.NhanBanh = customCapNhat.NhanBanh;
            custom.MauSacChuDao= customCapNhat.MauSacChuDao;
            custom.GhiChu = customCapNhat.GhiChu;
            custom.NgayLayHang = customCapNhat.NgayLayHang;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Đã cập nhật thông tin bánh Custom!" });
        }
    }
}