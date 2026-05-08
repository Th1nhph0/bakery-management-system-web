using Bakery.Data.DTOs;
using Bakery.Data.Models;
using Bakery.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bakery.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DonHangController : ControllerBase
    {
        private readonly DonHangService _donHangService;

        public DonHangController(DonHangService donHangService)
        {
            _donHangService = donHangService;
        }

        [HttpPost("TaoDonHang")]
        public async Task<IActionResult> TaoDonHangMoi([FromBody] TaoDonHangRequest request)
        {
            try
            {
                if (request.ChiTietGioHang == null || request.ChiTietGioHang.Count == 0)
                {
                    return BadRequest("Giỏ hàng không được để trống!");
                }

                int newOrderId = await _donHangService.TaoDonHangMoiAsync(request);
                return Ok(new { Message = "Đặt hàng thành công!", DonHangID = newOrderId });
            }
            catch (Exception ex)
            {
                // Nắm bắt cái lỗi THROW từ SQL Server (như hết hàng, lỗi transaction)
                return StatusCode(500, new { Message = "Lỗi khi tạo đơn hàng: " + ex.Message });
            }
        }
        [HttpGet("XuatHoaDonXML/{id}")]
        public async Task<IActionResult> XuatHoaDonXML(int id)
        {
            try
            {
                var xmlString = await _donHangService.XuatHoaDonXmlAsync(id);

                if (xmlString == null)
                {
                    return NotFound(new { Message = "Không tìm thấy đơn hàng số " + id });
                }

                // Chuyển chuỗi XML thành mảng Byte để ép trình duyệt tải file về
                byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(xmlString);
                string fileName = $"HoaDon_So_{id}.xml";

                // Trả về định dạng File (application/xml)
                return File(fileBytes, "application/xml", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi tạo XML: " + ex.Message });
            }
        }
    }
}