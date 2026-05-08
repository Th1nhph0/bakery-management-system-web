using Bakery.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bakery.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KhuyenMaiController : ControllerBase
    {
        private readonly KhuyenMaiService _service;

        public KhuyenMaiController(KhuyenMaiService service)
        {
            _service = service;
        }

        // Endpoint: GET api/KhuyenMai/{tenCode}
        [HttpGet("{tenCode}")]
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
        // Endpoint: GET /api/KhuyenMai/KiemTra?tenCode=KHAITRUONG&tongTien=200000
        [HttpGet("KiemTra")]
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
    }
}