using Bakery.Data.Models;
using Bakery.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bakery.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SanPhamController : ControllerBase
    {
        private readonly SanPhamService _sanPhamService;

        // Tiêm Service vào Controller (Dependency Injection)
        public SanPhamController(SanPhamService sanPhamService)
        {
            _sanPhamService = sanPhamService;
        }

        // ĐÂY LÀ CÁI HÀM MÀ SWAGGER ĐANG TÌM KIẾM NÀY!
        // Endpoint: GET /api/sanpham
        [HttpGet]
        public async Task<IActionResult> GetDanhSachSanPham()
        {
            // Gọi xuống tầng Service để lấy data
            var data = await _sanPhamService.GetAllSanPhamAsync();

            // Trả về dữ liệu dạng JSON với mã trạng thái 200 (Ok)
            return Ok(data);
        }
        // Endpoint: GET /api/SanPham/TimKiem?tuKhoa=Tiramisu
        [HttpGet("TimKiem")]
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
        // Endpoint: PUT /api/SanPham/{id} - ADMIN Cập nhật
        [HttpPut("{id}")]
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

        // Endpoint: DELETE /api/SanPham/{id} - ADMIN Xóa
        [HttpDelete("{id}")]
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