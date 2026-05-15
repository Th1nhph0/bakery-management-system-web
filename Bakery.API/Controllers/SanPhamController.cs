using Bakery.Data.Models;
using Bakery.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bakery.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SanPhamController : ControllerBase
    {
        private readonly BakeryManagementDbContext _context;
        private readonly SanPhamService _sanPhamService;

        public SanPhamController(SanPhamService sanPhamService, BakeryManagementDbContext context)
        {
            _context = context;
            _sanPhamService = sanPhamService; 
        }
        [HttpGet("HienThiDanhSachSanPham")]
        public async Task<IActionResult> GetDanhSachSanPham()
        {
            // Gọi xuống tầng Service để lấy data
            var data = await _sanPhamService.GetAllSanPhamAsync();

            // Trả về dữ liệu dạng JSON với mã trạng thái 200 (Ok)
            return Ok(data);
        }

        [HttpGet("XemPhanLoaiSanPham")]
        public async Task<IActionResult> GetDanhSachDanhMuc()
        {
            try
            {
                // Gọi thông qua Service, Controller không cần quan tâm _context là gì nữa
                var danhMuc = await _sanPhamService.GetDanhSachDanhMucAsync();
                return Ok(danhMuc);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Lỗi: " + ex.Message);
            }
        }


        [HttpPost("ThemSanPhamMoi")]
        public async Task<IActionResult> ThemSanPhamMoi([FromBody] SanPham sp)
        {
            try
            {
                _context.SanPhams.Add(sp);
                await _context.SaveChangesAsync();
                return Ok(new { Success = true, Message = "Thêm bánh mới thành công!", Data = sp });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi: " + ex.Message });
            }
        }

        [HttpGet("TimKiemSanPham")]
        public async Task<IActionResult> TimKiemSanPham([FromQuery] string tuKhoa)
        {
            try
            {
                var data = await _sanPhamService.TimKiemSanPhamAsync(tuKhoa);

                if (data == null || data.Count == 0)
                {
                    return NotFound(new { Success = false, Message = "Không tìm thấy đồ ngọt nào khớp với từ khóa!" });
                }

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }
   
        [HttpPut("CapNhatSanPham{id}")]
        public async Task<IActionResult> CapNhatSanPham(int id, [FromBody] SanPham sanPhamCapNhat)
        {
            try
            {
                var result = await _sanPhamService.CapNhatSanPhamAsync(id, sanPhamCapNhat);
                if (!result)
                {
                    return NotFound(new { Success = false, Message = "Không tìm thấy sản phẩm để cập nhật!" });
                }

                return Ok(new { Success = true, Message = "Cập nhật sản phẩm thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpDelete("XoaSanPham{id}")]
        public async Task<IActionResult> XoaSanPham(int id)
        {
            try
            {
                var result = await _sanPhamService.XoaSanPhamAsync(id);
                if (!result)
                {
                    return NotFound(new { Success = false, Message = "Không tìm thấy sản phẩm để xóa!" });
                }

                return Ok(new { Success = true, Message = "Đã xóa sản phẩm thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "Lỗi xóa dữ liệu: " + ex.Message });
            }
        }
    }
}