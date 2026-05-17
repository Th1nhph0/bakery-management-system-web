using Bakery.Data.DTOs;
using Bakery.Data.Models;
using Bakery.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bakery.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DonHangController : ControllerBase
    {
        private readonly BakeryManagementDbContext _context;
        private readonly DonHangService _donHangService;
        public DonHangController(DonHangService donHangService, BakeryManagementDbContext context)
        {
            _context = context;
            _donHangService = donHangService;
        }

        public class UpdateStatusRequest
        {
            public string TrangThaiMoi { get; set; } = null!;
            public string TenNhanVien { get; set; } = null!;
        }

        [HttpGet("XemToanBoDonHang")]
        public async Task<IActionResult> GetAllDonHang()
        {
            // Kết hợp LINQ để kiểm tra xem đơn hàng có nằm trong bảng Custom hay không ngay lúc query
            var dsDonHang = await _context.DonHangs
                                  .OrderByDescending(d => d.NgayDatHang)
                                  .Select(dh => new
                                  {
                                      // 1. Lấy toàn bộ thông tin gốc của đơn hàng
                                      dh.DonHangId,
                                      dh.MaDhHienThi,
                                      dh.TenNguoiNhan,
                                      dh.SdtNguoiNhan,
                                      dh.DiaChiGiao,
                                      dh.NgayDatHang,
                                      dh.TrangThai,
                                      dh.TongTien,
                                      dh.SoTienGiam,
                                      dh.NguoiCapNhat,
                                      dh.NgayCapNhat,

                                      // 2. VŨ KHÍ TỐI ƯU: Thêm một cột ảo (Cờ) kiểm tra Custom
                                      LaDonCustom = _context.DonBanhCustoms.Any(c => c.DonHangId == dh.DonHangId)
                                  })
                                  .ToListAsync();

            return Ok(dsDonHang);
        }

        [HttpPost("TaoDonHang")]
        public async Task<IActionResult> TaoDonHangMoi([FromBody] TaoDonHangDTO request)
        {
            // 1. Tạo đối tượng Đơn Hàng mới (Entity)
            var donHangMoi = new DonHang
            {
                KhachHangId = request.Khach_Hang_ID,
                NhanVienId = request.Nhan_Vien_ID,
                KhuyenMaiId = request.Khuyen_Mai_ID,
                TenNguoiNhan = request.Ten_Nguoi_Nhan,
                SdtNguoiNhan = request.SDT_Nguoi_Nhan,
                DiaChiGiao = request.Dia_Chi_Giao,
                NgayDatHang = DateTime.Now,
                TrangThai = "Chờ xử lý",
                TongTien = 0 // Tạm thời để bằng 0, sẽ cộng dồn sau
            };

            try
            {
                _context.DonHangs.Add(donHangMoi);
                await _context.SaveChangesAsync(); // SQL sẽ "hét" lên ở đây nếu truyền ID tào lao
            }
            catch (DbUpdateException)
            {
                // Bắt đúng lỗi của SQL và trả về 400 Bad Request cho Frontend
                return BadRequest(new { Message = "Lỗi dữ liệu: Mã khách hàng, nhân viên hoặc khuyến mãi không tồn tại trong hệ thống!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi máy chủ: " + ex.Message });
            }

            decimal tongTienDonHang = 0;

            // 2. Xử lý từng món trong giỏ hàng
            foreach (var item in request.ChiTietGioHang)
            {
                // QUAN TRỌNG: Tự tìm giá gốc trong Database dựa vào ID sản phẩm
                var sanPhamTonTai = await _context.SanPhams.FindAsync(item.SanPham_ID);

                if (sanPhamTonTai != null)
                {
                    var chiTiet = new ChiTietDonHang
                    {
                        DonHangId = donHangMoi.DonHangId,
                        SanPhamId = item.SanPham_ID,
                        SoLuong = item.So_Luong,
                        // Gán Đơn Giá bằng giá thực tế trong Database
                        DonGia = sanPhamTonTai.DonGiaBan,
                    };

                    _context.ChiTietDonHangs.Add(chiTiet);

                    // Cộng dồn tiền để lát nữa cập nhật lại Tổng Tiền Đơn Hàng
                    tongTienDonHang += ((sanPhamTonTai.DonGiaBan ?? 0) * item.So_Luong);
                }
            }

            // 3. TÍNH KHUYẾN MÃI VÀ TRỪ TIỀN AN TOÀN
            decimal soTienGiam = 0;
            if (request.Khuyen_Mai_ID.HasValue)
            {
                var khuyenMai = await _context.KhuyenMais.FindAsync(request.Khuyen_Mai_ID.Value);
                if (khuyenMai != null && khuyenMai.PhanTramGiam.HasValue)
                {
                    soTienGiam = tongTienDonHang * ((decimal)khuyenMai.PhanTramGiam.Value / 100);
                }
            }

            donHangMoi.SoTienGiam = soTienGiam;
            donHangMoi.TongTien = tongTienDonHang - soTienGiam; // Trừ tiền giảm để ra tổng khách trả

            await _context.SaveChangesAsync();

            // 🔥 ĐÂY CHÍNH LÀ DÒNG RETURN ÔNG BỊ THIẾU NÈ:
            return Ok(new { Message = "Tạo đơn hàng thành công!", DonHangId = donHangMoi.DonHangId });
        }

        [HttpPut("CapNhatTrangThaiDonHang/{id}")]
        public async Task<IActionResult> CapNhatTrangThai(int id, [FromBody] UpdateStatusRequest request)
        {
            var donHang = await _context.DonHangs.FindAsync(id);
            if (donHang == null) return NotFound();

            donHang.TrangThai = request.TrangThaiMoi;
            donHang.NguoiCapNhat = request.TenNhanVien;
            donHang.NgayCapNhat = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { Message = $"Nhân viên {request.TenNhanVien} đã cập nhật trạng thái!" });
        }

        [HttpGet("KiemTraDonHang/{donHangId}")]
        public async Task<IActionResult> KiemTraDonCustom(int donHangId)
        {
            try
            {
                // BƯỚC 1: Kiểm tra xem Đơn Hàng này có thực sự tồn tại trong hệ thống không
                // Dùng AnyAsync cho nhẹ server, nó chỉ trả về true/false chứ không lôi cả dòng data lên
                var donHangTonTai = await _context.DonHangs.AnyAsync(d => d.DonHangId == donHangId);

                if (!donHangTonTai)
                {
                    // Nếu không tồn tại, chặn ngay từ cửa và báo lỗi 404
                    return NotFound(new { Message = $"Không tìm thấy đơn hàng nào có mã số {donHangId}." });
                }

                // BƯỚC 2: Nếu đơn hàng CÓ TỒN TẠI, mới tiếp tục kiểm tra xem có phải đơn Custom không
                var thongTinCustom = await _context.DonBanhCustoms
                    .FirstOrDefaultAsync(c => c.DonHangId == donHangId);

                // Nếu tìm không thấy trong bảng Custom -> Chắc chắn đây là đơn mua bánh sẵn
                if (thongTinCustom == null)
                {
                    return Ok(new { LaDonCustom = false, Message = "Đây là đơn bánh có sẵn bình thường." });
                }

                // Nếu tìm thấy -> Báo là đơn Custom và trả về chi tiết (màu sắc, size, lời chúc...)
                return Ok(new
                {
                    LaDonCustom = true,
                    Data = thongTinCustom
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpGet("XuatHoaDonXMLDonHang/{id}")]
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

        // 1. SỬA LẠI HÀM CŨ: Bổ sung thêm trường SanPhamId vào để Edit Mode nhận diện được ID bánh
        [HttpGet("LayChiTietSanPhams/{donHangId}")]
        public async Task<IActionResult> LayChiTietSanPhams(int donHangId)
        {
            try
            {
                var danhSachMon = await _context.ChiTietDonHangs
                    .Where(ct => ct.DonHangId == donHangId)
                    .Select(ct => new
                    {
                        SanPhamId = ct.SanPhamId, // 🔥 BỔ SUNG DÒNG NÀY
                        TenSanPham = ct.SanPham.TenSanPham,
                        HinhAnh = ct.SanPham.HinhAnh,
                        SoLuong = ct.SoLuong,
                        DonGia = ct.DonGia
                    })
                    .ToListAsync();

                return Ok(danhSachMon);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi tải chi tiết món: " + ex.Message });
            }
        }

        // 🔥 BỔ SUNG MỚI: API lấy thông tin chung của 1 đơn hàng theo ID
        [HttpGet("LayDonHangTheoId/{id}")]
        public async Task<IActionResult> GetDonHangById(int id)
        {
            var dh = await _context.DonHangs.FindAsync(id);
            if (dh == null) return NotFound(new { Message = "Không tìm thấy đơn hàng!" });
            return Ok(dh);
        }

        [HttpPut("CapNhatThongTinDonHang/{id}")]
        public async Task<IActionResult> CapNhatDonHang(int id, [FromBody] CapNhatDonHangTongHopDTO request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var donHang = await _context.DonHangs.FindAsync(id);
            if (donHang == null) return NotFound(new { Message = "Không tìm thấy đơn hàng cần sửa!" });

            try
            {
                // A. CẬP NHẬT BẢNG ĐƠN HÀNG CHÍNH (DONHANG)
                donHang.KhachHangId = request.Khach_Hang_ID;
                donHang.KhuyenMaiId = request.Khuyen_Mai_ID;
                donHang.TenNguoiNhan = request.Ten_Nguoi_Nhan;
                donHang.SdtNguoiNhan = request.SDT_Nguoi_Nhan;
                donHang.DiaChiGiao = request.Dia_Chi_Giao;  
                donHang.NgayCapNhat = DateTime.Now;
                donHang.NguoiCapNhat = request.TenNhanVienCapNhat ?? "Nhân viên hệ thống";
             
                if (request.LaDonCustom)
                    {
                        var customDon = await _context.DonBanhCustoms.FirstOrDefaultAsync(c => c.DonHangId == id);
                        if (customDon != null)
                        {
                            customDon.LoaiYeuCau = request.LoaiYeuCau;
                            customDon.KichThuocSoLuong = request.KichThuocSoluong;
                            customDon.MauSacChuDao = request.MauSacChuDao;
                            customDon.GhiChu = request.GhiChu;

                            // 🔥 FIX LỖI 1: Thêm ?? DateTime.Now để tránh lệch kiểu dữ liệu nullable
                            customDon.NgayLayHang = request.NgayLayHang ?? DateTime.Now;

                            // 🔥 FIX LỖI 2: Nếu chưa kịp update Database, ông tạm thời comment dòng dưới lại bằng dấu // nhé
                            // Nếu đã làm Bước 2 dưới đây rồi thì giữ nguyên dòng này chạy bình thường:
                            customDon.HinhAnh = request.HinhAnh;
                            if (request.TongTienCustom.HasValue)
                            {
                                donHang.TongTien = request.TongTienCustom.Value;
                            }

                    }
                    }


                // C. LÀM SẠCH VÀ TÍNH LẠI GIỎ HÀNG CHI TIẾT
                var chiTietsCu = await _context.ChiTietDonHangs.Where(ct => ct.DonHangId == id).ToListAsync();
                if (chiTietsCu.Any()) _context.ChiTietDonHangs.RemoveRange(chiTietsCu);

                // 🔥 VÁ LỖI 1: Lấy tiền báo giá Custom làm gốc (nếu có), ngược lại bắt đầu từ 0đ
                decimal tongTienDonHang = request.LaDonCustom ? (request.TongTienCustom ?? 0) : 0;
                foreach (var item in request.ChiTietGioHang)
                {
                    var sanPhamTonTai = await _context.SanPhams.FindAsync(item.SanPham_ID);
                    if (sanPhamTonTai != null)
                    {
                        var chiTiet = new ChiTietDonHang
                        {
                            DonHangId = id,
                            SanPhamId = item.SanPham_ID,
                            SoLuong = item.So_Luong,
                            DonGia = sanPhamTonTai.DonGiaBan,
                        };
                        _context.ChiTietDonHangs.Add(chiTiet);

                        // Cộng dồn thêm tiền các món bánh có sẵn vào
                        tongTienDonHang += ((sanPhamTonTai.DonGiaBan ?? 0) * item.So_Luong);
                    }
                }
                decimal soTienGiam = 0;
                if (request.Khuyen_Mai_ID.HasValue)
                {
                    var khuyenMai = await _context.KhuyenMais.FindAsync(request.Khuyen_Mai_ID.Value);
                    if (khuyenMai != null && khuyenMai.PhanTramGiam.HasValue)
                    {
                        soTienGiam = tongTienDonHang * ((decimal)khuyenMai.PhanTramGiam.Value / 100);
                    }
                }
                donHang.SoTienGiam = soTienGiam;

                donHang.TongTien = tongTienDonHang - soTienGiam; await _context.SaveChangesAsync();

                return Ok(new { Message = "Cập nhật đơn hàng tổng hợp thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống khi cập nhật: " + ex.Message });
            }
        }

        [HttpDelete("XoaDonHang/{id}")]
        public async Task<IActionResult> XoaDonHang(int id)
        {
            var donHang = await _context.DonHangs.FindAsync(id);
            if (donHang == null) return NotFound(new { Message = "Không tìm thấy đơn hàng cần xóa!" });

            try
            {
                // 1. Tìm và xóa toàn bộ các món bánh trong bảng chi tiết đơn trước
                var chiTiets = await _context.ChiTietDonHangs.Where(ct => ct.DonHangId == id).ToListAsync();
                if (chiTiets.Any())
                {
                    _context.ChiTietDonHangs.RemoveRange(chiTiets);
                }

                // 2. Kiểm tra nếu có liên kết đơn Custom thì xóa nốt (bọc lót nâng cao)
                var customDon = await _context.DonBanhCustoms.FirstOrDefaultAsync(c => c.DonHangId == id);
                if (customDon != null)
                {
                    _context.DonBanhCustoms.Remove(customDon);
                }

                // 3. Xóa đơn hàng chính
                _context.DonHangs.Remove(donHang);

                // Lưu tất cả thay đổi xuống database
                await _context.SaveChangesAsync();
                return Ok(new { Success = true, Message = "Đã xóa đơn hàng thành công khỏi hệ thống!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = "Lỗi khi xóa đơn hàng: " + ex.Message });
            }
        }

        [HttpPost("UploadAnhMauCustom")]
        public async Task<IActionResult> UploadAnh(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Message = "File ảnh không hợp lệ!" });

            try
            {
                // Tạo đường dẫn đến thư mục wwwroot/uploads trong project Backend
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

                // Nếu thư mục uploads chưa tồn tại thì tự động tạo mới
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                // Đổi tên file thành mã ngẫu nhiên để tránh trùng tên file (Ví dụ: a12b3c_elsa.jpg)
                var fileName = Guid.NewGuid().ToString().Substring(0, 8) + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(folderPath, fileName);

                // Tiến hành lưu file vật lý xuống ổ đĩa server
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Trả về đường link local dẫn tới file ảnh vừa up thành công
                var linkAnhNoiBo = $"{Request.Scheme}://{Request.Host}/uploads/{fileName}";
                return Ok(new { Success = true, LinkAnh = linkAnhNoiBo });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi upload file: " + ex.Message });
            }
        }

    }
}