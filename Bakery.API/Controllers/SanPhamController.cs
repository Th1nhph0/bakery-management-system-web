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
    }
}