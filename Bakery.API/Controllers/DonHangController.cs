using Bakery.Data.DTOs;
using Bakery.Data.Models;
using Bakery.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq; // 🔥 BẮT BUỘC: Đảm bảo có dòng này để xử lý XElement và XDocument

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

        // ==========================================
        // 1. API: XEM TOÀN BỘ ĐƠN HÀNG (TRANG CHỦ + GRID)
        // ==========================================
        [HttpGet("XemToanBoDonHang")]
        public async Task<IActionResult> GetAllDonHang()
        {
            var dsDonHang = await _context.DonHangs
                                          .OrderByDescending(d => d.NgayDatHang)
                                          .Select(dh => new
                                          {
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
                                              LaDonCustom = _context.DonBanhCustoms.Any(c => c.DonHangId == dh.DonHangId),
                                              TenMaCode = dh.KhuyenMai != null ? dh.KhuyenMai.MaCode : "",
                                              PhanTramGiamGoc = dh.KhuyenMai != null ? dh.KhuyenMai.PhanTramGiam : 0
                                          })
                                          .ToListAsync();

            return Ok(dsDonHang);
        }

        // ==========================================
        // 2. API: TẠO MỚI ĐƠN HÀNG ĐÔNG BỘ (CHECK KHO + TẠO CUSTOM)
        // ==========================================
        [HttpPost("TaoDonHang")]
        public async Task<IActionResult> TaoDonHangMoi([FromBody] TaoDonHangRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var donHangMoi = new DonHang
                {
                    KhachHangId = request.Khach_Hang_ID == 0 ? null : request.Khach_Hang_ID,
                    NhanVienId = request.Nhan_Vien_ID,
                    KhuyenMaiId = request.Khuyen_Mai_ID,
                    TenNguoiNhan = request.Ten_Nguoi_Nhan,
                    SdtNguoiNhan = request.SDT_Nguoi_Nhan,
                    DiaChiGiao = request.Dia_Chi_Giao,
                    NgayDatHang = DateTime.Now,
                    TrangThai = "Chờ xử lý",
                    TongTien = 0
                };

                _context.DonHangs.Add(donHangMoi);
                await _context.SaveChangesAsync();

                decimal tongTienDonHang = request.LaDonCustom ? (request.TongTienCustom ?? 0) : 0;

                if (request.ChiTietGioHang != null && request.ChiTietGioHang.Count > 0)
                {
                    foreach (var item in request.ChiTietGioHang)
                    {
                        var sanPhamTonTai = await _context.SanPhams.FindAsync(item.SanPham_ID);
                        if (sanPhamTonTai != null)
                        {
                            if (sanPhamTonTai.SoLuongTon < item.So_Luong)
                            {
                                return BadRequest(new { Message = $"Bánh [{sanPhamTonTai.TenSanPham}] trong kho chỉ còn {sanPhamTonTai.SoLuongTon} cái, không đủ để đặt!" });
                            }

                            sanPhamTonTai.SoLuongTon -= item.So_Luong;

                            var chiTiet = new ChiTietDonHang
                            {
                                DonHangId = donHangMoi.DonHangId,
                                SanPhamId = item.SanPham_ID,
                                SoLuong = item.So_Luong,
                                DonGia = sanPhamTonTai.DonGiaBan,
                            };
                            _context.ChiTietDonHangs.Add(chiTiet);

                            tongTienDonHang += ((sanPhamTonTai.DonGiaBan ?? 0) * item.So_Luong);
                        }
                    }
                }

                if (request.LaDonCustom)
                {
                    var totalCustomRecords = await _context.DonBanhCustoms.CountAsync() + 1;
                    var maCustomHienThi = $"DC{totalCustomRecords:D3}";

                    var donBanhCustom = new DonBanhCustom
                    {
                        MaCustomHienThi = maCustomHienThi,
                        DonHangId = donHangMoi.DonHangId,
                        LoaiYeuCau = request.LoaiYeuCau,
                        KichThuocSoLuong = request.KichThuocSoluong,
                        MauSacChuDao = request.MauSacChuDao,
                        GhiChu = request.GhiChu,
                        NhanBanh = "Cốt kem tươi vani tiêu chuẩn",
                        NgayLayHang = request.NgayLayHang ?? DateTime.Now.AddDays(3),
                        HinhAnh = request.HinhAnh ?? ""
                    };
                    _context.DonBanhCustoms.Add(donBanhCustom);
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

                donHangMoi.SoTienGiam = soTienGiam;
                donHangMoi.TongTien = tongTienDonHang - soTienGiam;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { Message = "Tạo đơn hàng tổng hợp thành công!", DonHangId = donHangMoi.DonHangId });
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { Message = "Lỗi dữ liệu liên kết hệ thống mã Id không khớp!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { Message = "Lỗi hệ thống máy chủ: " + ex.Message });
            }
        }

        // ==========================================
        // 3. API: CẬP NHẬT TRẠNG THÁI TIẾN ĐỘ SIÊU TỐC
        // ==========================================
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

        // ==========================================
        // 4. API: KIỂM TRA ĐƠN HÀNG CUSTOM
        // ==========================================
        [HttpGet("KiemTraDonHang/{donHangId}")]
        public async Task<IActionResult> KiemTraDonCustom(int donHangId)
        {
            try
            {
                var donHangTonTai = await _context.DonHangs.AnyAsync(d => d.DonHangId == donHangId);
                if (!donHangTonTai)
                {
                    return NotFound(new { Message = $"Không tìm thấy đơn hàng mã số {donHangId}." });
                }

                var thongTinCustom = await _context.DonBanhCustoms.FirstOrDefaultAsync(c => c.DonHangId == donHangId);
                if (thongTinCustom == null)
                {
                    return Ok(new { LaDonCustom = false, Message = "Đây là đơn bánh có sẵn bình thường." });
                }

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

        // =========================================================================
        // 5. 🔥 API NÂNG CẤP: TỰ ĐỘNG DỰNG FILE XML ĐẦY ĐỦ THÔNG TIN ĐỂ XUẤT/TẢI VỀ
        // =========================================================================
        [HttpGet("XuatHoaDonXMLDonHang/{id}")]
        public async Task<IActionResult> XuatHoaDonXML(int id)
        {
            try
            {
                // Tải dữ liệu đầy đủ từ DB lên bao gồm Khách hàng, Nhân viên, Khuyến mãi
                var dh = await _context.DonHangs
                                       .Include(d => d.KhachHang)
                                       .Include(d => d.NhanVien)
                                       .Include(d => d.KhuyenMai)
                                       .FirstOrDefaultAsync(d => d.DonHangId == id);

                if (dh == null) return NotFound(new { Message = "Không tìm thấy đơn hàng số " + id });

                var chiTiets = await _context.ChiTietDonHangs
                                             .Include(ct => ct.SanPham)
                                             .Where(ct => ct.DonHangId == id)
                                             .ToListAsync();

                var customBanh = await _context.DonBanhCustoms.FirstOrDefaultAsync(c => c.DonHangId == id);

                // Dùng LINQ to XML thiết lập cấu trúc cây XML phức tạp chuẩn điểm A+
                var xmlTree = new XElement("HoaDon",
                    new XElement("DonHangId", dh.DonHangId),
                    new XElement("MaDhHienThi", dh.MaDhHienThi ?? "DH" + id),
                    new XElement("TenNguoiNhan", dh.TenNguoiNhan ?? "Khách vãng lai"),
                    new XElement("SdtNguoiNhan", dh.SdtNguoiNhan ?? "-"),
                    new XElement("DiaChiGiao", dh.DiaChiGiao ?? "Nhận tại tiệm"),
                    new XElement("NgayDatHang", dh.NgayDatHang?.ToString("dd/MM/yyyy HH:mm") ?? DateTime.Now.ToString()),
                    new XElement("TongTien", dh.TongTien ?? 0),
                    new XElement("SoTienGiam", dh.SoTienGiam ?? 0),
                    new XElement("TenNhanVien", dh.NhanVien?.TenNhanVien ?? dh.NguoiCapNhat ?? "Nhân viên hệ thống"),
                    new XElement("MaCodeKhuyenMai", dh.KhuyenMai?.MaCode ?? ""),
                    new XElement("ChiTietDonHangs",
                        chiTiets.Select(ct => new XElement("ChiTietDonHang",
                            new XElement("TenSanPham", ct.SanPham?.TenSanPham ?? "Bánh ngọt có sẵn"),
                            new XElement("SoLuong", ct.SoLuong ?? 0),
                            new XElement("DonGia", ct.DonGia ?? 0)
                        ))
                    ),
                    customBanh != null ? new XElement("DonBanhCustom",
                        new XElement("LoaiYeuCau", customBanh.LoaiYeuCau ?? "Bánh đặt thiết kế"),
                        new XElement("KichThuocSoLuong", customBanh.KichThuocSoLuong ?? "Chưa rõ size"),
                        new XElement("MauSacChuDao", customBanh.MauSacChuDao ?? "Tự do"),
                        new XElement("GhiChu", customBanh.GhiChu ?? ""),
                        new XElement("NgayLayHang", customBanh.NgayLayHang.ToString("dd/MM/yyyy") ?? "-")
                    ) : null
                );

                string xmlString = xmlTree.ToString();
                byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(xmlString);
                string fileName = $"HoaDon_Chuan_So_{id}.xml";

                return File(fileBytes, "application/xml", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khởi tạo cấu trúc dữ liệu XML: " + ex.Message });
            }
        }

        // =========================================================================
        // 6. 🔥 API ĐỒNG BỘ: ĐỌC TỪ XML TRUNG GIAN ĐỂ BIẾN ĐỔI GIAO DIỆN IN ẤN / PDF
        // =========================================================================
        [HttpGet("XuatHoaDonPDFDonHang/{id}")]
        public async Task<IActionResult> XuatHoaDonPDF(int id)
        {
            try
            {
                // Bước 1: Gọi nội bộ hàm xuất XML ở trên để lấy chuỗi XML gốc làm dữ liệu trung gian
                var xmlResult = await XuatHoaDonXML(id);
                if (xmlResult is NotFoundObjectResult || xmlResult is NotFoundResult)
                {
                    return NotFound(new { Message = "Không thể đọc dữ liệu XML của hóa đơn số " + id });
                }

                // Ép kiểu lấy mảng Byte dữ liệu XML ra chuỗi ký tự string
                var fileContentResult = xmlResult as FileContentResult;
                if (fileContentResult == null) return StatusCode(500, new { Message = "Lỗi luồng chuyển đổi dữ liệu XML!" });
                string xmlString = System.Text.Encoding.UTF8.GetString(fileContentResult.FileContents);

                // Bước 2: Parse chuỗi XML bằng XDocument
                XDocument xmlDoc = XDocument.Parse(xmlString);
                XElement root = xmlDoc.Root;

                // Bước 3: Đọc dữ liệu từ các thẻ XML ra biến C#
                string maDhHienThi = root.Element("MaDhHienThi")?.Value ?? id.ToString();
                string tenNguoiNhan = root.Element("TenNguoiNhan")?.Value ?? "Khách hàng";
                string sdtNguoiNhan = root.Element("SdtNguoiNhan")?.Value ?? "-";
                string diaChiGiao = root.Element("DiaChiGiao")?.Value ?? "-";
                string ngayDat = root.Element("NgayDatHang")?.Value ?? DateTime.Now.ToString();
                string tongTien = root.Element("TongTien")?.Value ?? "0";
                string soTienGiam = root.Element("SoTienGiam")?.Value ?? "0";
                string tenNhanVien = root.Element("TenNhanVien")?.Value ?? "Nhân viên hệ thống";
                string maKhuyenMai = root.Element("MaCodeKhuyenMai")?.Value ?? "";

                // 🔥 THAY ĐỔI QUAN TRỌNG: Ép kiểu InvariantCulture để tránh lỗi nhảy số từ 15k lên 1 triệu 5
                decimal decimalTongTien = decimal.Parse(tongTien, System.Globalization.CultureInfo.InvariantCulture);
                decimal decimalSoTienGiam = decimal.Parse(soTienGiam, System.Globalization.CultureInfo.InvariantCulture);
                decimal decimalTongTienTruocGiam = decimalTongTien + decimalSoTienGiam;

                // Bước 4: Chạy vòng lặp các thẻ ChiTietDonHang của XML để dựng bảng bánh tiêu chuẩn
                string danhSachBanhHtml = "";
                int stt = 1;
                decimal tongTienBanhThuong = 0;

                var chiTietElements = root.Element("ChiTietDonHangs")?.Elements("ChiTietDonHang") ?? Enumerable.Empty<XElement>();
                foreach (var item in chiTietElements)
                {
                    string tenSp = item.Element("TenSanPham")?.Value ?? "Bánh ngọt";
                    string soLuong = item.Element("SoLuong")?.Value ?? "0";
                    string donGia = item.Element("DonGia")?.Value ?? "0";

                    // 🔥 THAY ĐỔI QUAN TRỌNG: Thêm InvariantCulture vào đây để đơn giá nhân lên chính xác
                    decimal subTotal = decimal.Parse(donGia, System.Globalization.CultureInfo.InvariantCulture) * int.Parse(soLuong);
                    tongTienBanhThuong += subTotal;

                    danhSachBanhHtml += $@"
            <tr>
                <td style='text-align:center;'>{stt++}</td>
                <td><strong>{tenSp}</strong></td>
                <td style='text-align:center;'>{soLuong}</td>
                <td>{string.Format("{0:N0}", decimal.Parse(donGia, System.Globalization.CultureInfo.InvariantCulture))} đ</td>
                <td><strong>{string.Format("{0:N0}", subTotal)} đ</strong></td>
            </tr>";
                }

                // Bước 5: Kiểm tra thẻ DonBanhCustom của XML để chèn thông số bánh Custom
                var customElement = root.Element("DonBanhCustom");
                string khungBanhCustomHtml = "";

                if (customElement != null)
                {
                    string loaiYeuCau = customElement.Element("LoaiYeuCau")?.Value ?? "Bánh thiết kế";
                    string kichThuoc = customElement.Element("KichThuocSoLuong")?.Value ?? "Chưa rõ";
                    string mauSac = customElement.Element("MauSacChuDao")?.Value ?? "Tự do";
                    string ghiChu = customElement.Element("GhiChu")?.Value ?? "Không có";
                    string ngayLay = customElement.Element("NgayLayHang")?.Value ?? "-";

                    decimal tienBanhCustom = decimalTongTienTruocGiam - tongTienBanhThuong;
                    if (tienBanhCustom < 0) tienBanhCustom = 0;

                    danhSachBanhHtml += $@"
            <tr style='background-color: #fffbf0;'>
                <td style='text-align:center;'>{stt++}</td>
                <td><span style='color: #b47e00; font-weight: bold;'>⭐ [BÁNH THIẾT KẾ CUSTOM] - {loaiYeuCau}</span></td>
                <td style='text-align:center;'>1</td>
                <td>{string.Format("{0:N0}", tienBanhCustom)} đ</td>
                <td><strong style='color: #b47e00;'>{string.Format("{0:N0}", tienBanhCustom)} đ</strong></td>
            </tr>";

                    khungBanhCustomHtml = $@"
            <div style='background-color: #fffdf6; border: 1px dashed #ffc107; border-radius: 6px; padding: 12px; margin-top: 15px;'>
                <div style='color: #b47e00; font-weight: bold; margin-bottom: 5px;'>📝 THÔNG SỐ CẤU HÌNH BÁNH THIẾT KẾ RIÊNG (BÓC TỪ THẺ XML)</div>
                <table style='width: 100%; font-size: 13px;' cellpadding='4'>
                    <tr>
                        <td style='width: 20%; font-weight:bold;'>Quy cách/Size:</td>
                        <td style='width: 30%;'>{kichThuoc}</td>
                        <td style='width: 20%; font-weight:bold;'>Màu sắc gốc:</td>
                        <td style='width: 30%;'>{mauSac}</td>
                    </tr>
                    <tr>
                        <td style='font-weight:bold;'>Yêu cầu chữ:</td>
                        <td colspan='3'>{ghiChu}</td>
                    </tr>
                    <tr>
                        <td style='font-weight:bold;'>Ngày hẹn lấy:</td>
                        <td colspan='3' style='color:red; font-weight:bold;'>{ngayLay}</td>
                    </tr>
                </table>
            </div>";
                }

                // Bước 6: Ghép dữ liệu dựng Template HTML hóa đơn in ấn hoàn chỉnh (ĐÃ SỬA GIAO DIỆN THEO Ý ÔNG)
                string dongKhuyenMaiHtml = "";
                if (decimalSoTienGiam > 0)
                {
                    dongKhuyenMaiHtml = $@"
            <tr>
                <td style='color: #ff3e1d;'>Khuyến mãi ({(!string.IsNullOrEmpty(maKhuyenMai) ? maKhuyenMai : "Đã áp dụng")}):</td>
                <td style='color: #ff3e1d; font-weight:bold;'>-{string.Format("{0:N0}", decimalSoTienGiam)} đ</td>
            </tr>";
                }

                var finalHtml = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8' />
            <title>HoaDon_XML_#{maDhHienThi}</title>
            <style>
                body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; color: #333; padding: 20px; }}
                .invoice-box {{ max-width: 800px; margin: auto; padding: 30px; border: 1px solid #eee; box-shadow: 0 0 10px rgba(0, 0, 0, .15); border-radius: 8px; background: #fff; }}
                
                /* Tên tiệm nằm riêng biệt ở góc trái */
                .store-name {{ font-size: 24px; font-weight: bold; color: #5f5af6; text-transform: uppercase; text-align: left; margin-bottom: 5px; }}
                
                /* Đẩy tiêu đề hóa đơn xuống dưới và căn lề giữa */
                .invoice-header-center {{ text-align: center; margin-top: 15px; margin-bottom: 30px; }}
                .invoice-title {{ font-size: 22px; font-weight: bold; color: #333; letter-spacing: 0.5px; }}
                
                .details-table {{ width: 100%; border-collapse: collapse; margin-top: 15px; }}
                .details-table th {{ background-color: #f5f5f9; color: #566a7f; padding: 10px; border: 1px solid #d9dee3; font-weight: 600; }}
                .details-table td {{ padding: 10px; border: 1px solid #d9dee3; }}
                .summary-table {{ width: 45%; margin-left: auto; margin-top: 20px; border-collapse: collapse; }}
                .summary-table td {{ padding: 6px 10px; text-align: right; }}
                @media print {{ .no-print {{ display: none; }} .invoice-box {{ box-shadow: none; border: none; padding: 0; }} }}
            </style>
        </head>
        <body>
            <div class='invoice-box'>
                <div class='no-print' style='text-align: right; margin-bottom: 15px;'>
                    <button onclick='window.print()' style='background: #696cff; color: #fff; border: none; padding: 8px 16px; border-radius: 4px; font-weight: bold; cursor: pointer;'>🖨️ Bấm In Hóa Đơn / Xuất PDF</button>
                </div>
                
                <div class='store-name'>🍰 BAKERY</div>
                
                <div class='invoice-header-center'>
                    <div class='invoice-title'>HÓA ĐƠN BÁN HÀNG</div>
                    <span style='color:#8592a3; font-size: 13px;'>Dữ liệu trung gian: XML Raw</span>
                </div>

                <table style='width:100%; margin-bottom:25px; line-height: 24px; font-size: 14px;'>
                    <tr>
                        <td style='width:50%; vertical-align:top;'>
                            <strong>Mã hóa đơn:</strong> #{maDhHienThi}<br/>
                            <strong>Nhân viên lập đơn:</strong> {tenNhanVien}<br/>
                            <strong>Ngày tạo dữ liệu:</strong> {ngayDat}<br/>
                        </td>
                        <td style='width:50%; vertical-align:top;'>
                            <strong>Tên người nhận:</strong> {tenNguoiNhan}<br/>
                            <strong>Số điện thoại:</strong> {sdtNguoiNhan}<br/>
                            <strong>Địa chỉ giao:</strong> {diaChiGiao}<br/>
                        </td>
                    </tr>
                </table>

                <table class='details-table'>
                    <thead>
                        <tr>
                            <th style='width:8%; text-align:center;'>STT</th>
                            <th>Tên Loại Bánh / Sản Phẩm</th>
                            <th style='width:12%; text-align:center;'>SL</th>
                            <th style='width:18%;'>Đơn Giá</th>
                            <th style='width:20%;'>Thành Tiền</th>
                        </tr>
                    </thead>
                    <tbody>
                        {danhSachBanhHtml}
                    </tbody>
                </table>

                {khungBanhCustomHtml}

                <table class='summary-table'>
                    <tr>
                        <td>Tổng tiền hàng gộp:</td>
                        <td>{string.Format("{0:N0}", decimalTongTienTruocGiam)} đ</td>
                    </tr>
                    {dongKhuyenMaiHtml}
                    <tr style='font-size: 16px; font-weight: bold; color: #71dd37; border-top: 2px solid #71dd37;'>
                        <td>TỔNG THANH TOÁN:</td>
                        <td style='font-size:18px;'>{string.Format("{0:N0}", decimalTongTien)} đ</td>
                    </tr>
                </table>

                <div style='text-align: center; margin-top: 40px; font-style: italic; color: #a1acb8; border-top: 1px dashed #d9dee3; padding-top: 15px;'>
                    <p>Cảm ơn Quý khách đã lựa chọn mua bánh tại cửa hàng của chúng tôi!</p>
                    <small>Hệ thống quản lý tiệm bánh</small>
                </div>
            </div>
            <script>window.onload = function() {{ window.print(); }}</script>
        </body>
        </html>";

                return Content(finalHtml, "text/html", System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi xử lý và bóc tách dữ liệu XML hóa đơn: " + ex.Message });
            }
        }

        // ==========================================
        // 7. API: TẢI CHI TIẾT GIỎ MÓN ĂN ĐI KÈM
        // ==========================================
        [HttpGet("LayChiTietSanPhams/{donHangId}")]
        public async Task<IActionResult> LayChiTietSanPhams(int donHangId)
        {
            try
            {
                var danhSachMon = await _context.ChiTietDonHangs
                    .Where(ct => ct.DonHangId == donHangId)
                    .Select(ct => new
                    {
                        SanPhamId = ct.SanPhamId,
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

        // ==========================================
        // 8. API: LẤY THÔNG TIN HÀNH CHÍNH ĐƠN THEO ID
        // ==========================================
        [HttpGet("LayDonHangTheoId/{id}")]
        public async Task<IActionResult> GetDonHangById(int id)
        {
            var dh = await _context.DonHangs.FindAsync(id);
            if (dh == null) return NotFound(new { Message = "Không tìm thấy đơn hàng!" });
            return Ok(dh);
        }

        // ==========================================
        // 9. API: CẬP NHẬT TỔNG HỢP HỒ SƠ ĐƠN HÀNG
        // ==========================================
        [HttpPut("CapNhatThongTinDonHang/{id}")]
        public async Task<IActionResult> CapNhatDonHang(int id, [FromBody] CapNhatDonHangTongHopDTO request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var donHang = await _context.DonHangs.FindAsync(id);
            if (donHang == null) return NotFound(new { Message = "Không tìm thấy đơn hàng cần sửa!" });

            try
            {
                donHang.KhachHangId = request.Khach_Hang_ID == 0 ? null : request.Khach_Hang_ID;
                donHang.KhuyenMaiId = request.Khuyen_Mai_ID;
                donHang.TenNguoiNhan = request.Ten_Nguoi_Nhan;
                donHang.SdtNguoiNhan = request.SDT_Nguoi_Nhan;
                donHang.DiaChiGiao = request.Dia_Chi_Giao;
                donHang.NgayCapNhat = DateTime.Now;
                donHang.NguoiCapNhat = request.TenNhanVienCapNhat ?? "Nhân viên hệ thống";

                var customDon = await _context.DonBanhCustoms.FirstOrDefaultAsync(c => c.DonHangId == id);
                if (request.LaDonCustom)
                {
                    if (customDon != null)
                    {
                        customDon.LoaiYeuCau = request.LoaiYeuCau;
                        customDon.KichThuocSoLuong = request.KichThuocSoluong;
                        customDon.MauSacChuDao = request.MauSacChuDao;
                        customDon.GhiChu = request.GhiChu;
                        customDon.NgayLayHang = request.NgayLayHang ?? DateTime.Now;
                        customDon.HinhAnh = request.HinhAnh;
                    }
                    else
                    {
                        var totalCustomRecords = await _context.DonBanhCustoms.CountAsync() + 1;
                        var newCustom = new DonBanhCustom
                        {
                            MaCustomHienThi = $"DC{totalCustomRecords:D3}",
                            DonHangId = id,
                            LoaiYeuCau = request.LoaiYeuCau,
                            KichThuocSoLuong = request.KichThuocSoluong,
                            MauSacChuDao = request.MauSacChuDao,
                            GhiChu = request.GhiChu,
                            NgayLayHang = request.NgayLayHang ?? DateTime.Now,
                            HinhAnh = request.HinhAnh,
                            NhanBanh = "Cốt kem tươi vani tiêu chuẩn"
                        };
                        _context.DonBanhCustoms.Add(newCustom);
                    }
                }
                else
                {
                    if (customDon != null) _context.DonBanhCustoms.Remove(customDon);
                }

                var chiTietsCu = await _context.ChiTietDonHangs.Where(ct => ct.DonHangId == id).ToListAsync();
                if (chiTietsCu.Any()) _context.ChiTietDonHangs.RemoveRange(chiTietsCu);

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
                donHang.TongTien = tongTienDonHang - soTienGiam;

                await _context.SaveChangesAsync();
                return Ok(new { Message = "Cập nhật hồ sơ đơn hàng tổng hợp thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống khi cập nhật: " + ex.Message });
            }
        }

        // ==========================================
        // 10. API: XÓA ĐƠN HÀNG KHỎI DB
        // ==========================================
        [HttpDelete("XoaDonHang/{id}")]
        public async Task<IActionResult> XoaDonHang(int id)
        {
            var donHang = await _context.DonHangs.FindAsync(id);
            if (donHang == null) return NotFound(new { Message = "Không tìm thấy đơn hàng cần xóa!" });

            try
            {
                var chiTiets = await _context.ChiTietDonHangs.Where(ct => ct.DonHangId == id).ToListAsync();
                if (chiTiets.Any()) _context.ChiTietDonHangs.RemoveRange(chiTiets);

                var customDon = await _context.DonBanhCustoms.FirstOrDefaultAsync(c => c.DonHangId == id);
                if (customDon != null) _context.DonBanhCustoms.Remove(customDon);

                _context.DonHangs.Remove(donHang);
                await _context.SaveChangesAsync();
                return Ok(new { Success = true, Message = "Đã xóa đơn hàng thành công khỏi hệ thống!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = "Lỗi khi xóa đơn hàng: " + ex.Message });
            }
        }

        // ==========================================
        // 11. API: TẢI FILE ẢNH MẪU LÊN SERVER VẬT LÝ
        // ==========================================
        [HttpPost("UploadAnhMauCustom")]
        public async Task<IActionResult> UploadAnh([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest(new { Message = "File ảnh không hợp lệ!" });

            try
            {
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                var fileName = Guid.NewGuid().ToString().Substring(0, 8) + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(folderPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

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