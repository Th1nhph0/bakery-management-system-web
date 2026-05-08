using Bakery.Data.DTOs;
using Bakery.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bakery.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DonBanhCustomController : ControllerBase
    {
        private readonly DonBanhCustomService _service;

        // Tiêm Service vào Controller
        public DonBanhCustomController(DonBanhCustomService service)
        {
            _service = service;
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
    }
}