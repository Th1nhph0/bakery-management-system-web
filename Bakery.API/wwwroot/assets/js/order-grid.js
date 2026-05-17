let globalOrders = [];
let currentTypeFilter = "all";
let currentStatusFilter = "All";

// 1. TẢI TOÀN BỘ ĐƠN HÀNG TỪ CONTROLLER
async function fetchAllOrders() {
    const grid = document.getElementById('gridAllOrders');
    if (!grid) return;

    try {
        const response = await fetch(`${API_URL}/api/DonHang/XemToanBoDonHang`);
        globalOrders = await response.json();
        executeCombinedFilter();
    } catch (e) {
        console.error("Lỗi gọi API đơn hàng:", e);
        grid.innerHTML = `<div class="col-12 text-center text-danger py-5">Không kết nối được Backend API!</div>`;
    }
}

function getStatusConfig(currentStatus) {
    switch (currentStatus) {
        case "Chờ xử lý": return { next: "Đã duyệt", class: "bg-label-secondary", btnClass: "btn-primary", icon: "bx-check-shield" };
        case "Đã duyệt": return { next: "Đang làm", class: "bg-label-primary", btnClass: "btn-warning", icon: "bx-wrench" };
        case "Đang làm": return { next: "Hoàn thành", class: "bg-label-warning", btnClass: "btn-success", icon: "bx-package" };
        case "Hoàn thành": return { next: "Đã giao/Đã thanh toán", class: "bg-label-success", btnClass: "btn-info", icon: "bx-money" };
        case "Giao không thành công": return { next: null, class: "bg-label-danger", btnClass: "", icon: "" };
        case "Đã hủy": return { next: null, class: "bg-label-dark", btnClass: "", icon: "" };
        default: return { next: null, class: "bg-label-info", btnClass: "", icon: "" };
    }
}

// 3. HIỂN THỊ DỮ LIỆU RA LƯỚI CARD + 🔥 TÍCH HỢP NÚT HỦY ĐƠN NGOÀI GIAO DIỆN
function renderOrderGrid(orders) {
    const grid = document.getElementById('gridAllOrders');
    grid.innerHTML = '';

    if (orders.length === 0) {
        grid.innerHTML = `<div class="col-12 text-center text-muted py-5">Không có đơn hàng nào khớp với điều kiện lọc!</div>`;
        return;
    }

    orders.forEach(dh => {
        const id = dh.donHangId || dh.DonHangId;
        const maHienThi = dh.maDhHienThi || `DH${id}`;
        const tenKhach = dh.tenNguoiNhan || dh.TenNguoiNhan || '👤 Khách hàng vãng lai';
        const tongTien = new Intl.NumberFormat('vi-VN').format(dh.tongTien || dh.TongTien || 0) + ' đ';
        const trangThaiHienTai = dh.trangThai || dh.TrangThai || 'Chờ xử lý';
        const laCustom = dh.laDonCustom || dh.LaDonCustom;

        const cfg = getStatusConfig(trangThaiHienTai);

        let xmlBtnHtml = '';
        if (trangThaiHienTai !== "Chờ xử lý") {
            xmlBtnHtml = `
        <a href="${API_URL}/api/DonHang/XuatHoaDonPDFDonHang/${id}" target="_blank" class="btn btn-xs btn-outline-primary w-100 mt-2 fw-bold">
            <i class="bx bx-printer me-1"></i> 🖨️ Xuất Hóa Đơn / PDF
        </a>`;
        }

        let statusHtml = '';
        if (cfg.next) {
            statusHtml = `
                <div class="status-badge-wrapper">
                    <span class="badge ${cfg.class} current-status-badge">${trangThaiHienTai}</span>
                    <button class="btn btn-sm ${cfg.btnClass} next-status-btn" onclick="nextStepStatus(${id}, '${cfg.next}')">
                        <i class="bx ${cfg.icon} me-1"></i> Sang: ${cfg.next}
                    </button>
                </div>`;
        } else {
            statusHtml = `<div class="status-badge-wrapper"><span class="badge ${cfg.class} current-status-badge">${trangThaiHienTai}</span></div>`;
        }

        // Kiểm tra xem đơn hàng đã kết thúc chưa (Để ẩn nút Hủy)
        const checkStatusLower = trangThaiHienTai.toLowerCase().trim();
        const laDonDaKetThuc = ["đã giao/đã thanh toán", "đã giao", "đã thanh toán", "giao không thành công", "đã hủy"].includes(checkStatusLower);

        // Khởi tạo nút bấm Hủy đơn nếu đơn hàng còn đang hoạt động
        let huyDonBtnHtml = '';
        if (!laDonDaKetThuc) {
            huyDonBtnHtml = `
                <button class="btn btn-xs btn-outline-danger ms-1" onclick="clickHuyDonHangSieuToc(${id}, '${maHienThi}')" title="Hủy lập tức đơn hàng này">
                    <i class="bx bx-x-circle me-1"></i>Hủy đơn
                </button>
            `;
        }

        // 🔥 CẬP NHẬT: Khối quản lý nút Sửa, Hủy, Xóa theo quy định trạng thái nghiêm ngặt
        let actionButtonsHtml = '';

        if (trangThaiHienTai === "Chờ xử lý") {
            // TẦNG 1: Đơn hàng mới tạo (Chờ xử lý) -> Cho phép Sửa, Hủy đơn nhanh, hoặc Xóa cứng khỏi DB
            actionButtonsHtml = `
                <button class="btn btn-xs btn-outline-warning" onclick="editOrder(${id})"><i class="bx bx-edit-alt me-1"></i>Sửa</button>
                ${huyDonBtnHtml}
                <button class="btn btn-xs btn-outline-secondary ms-1" onclick="deleteOrder(${id})"><i class="bx bx-trash me-1"></i>Xóa</button>
            `;
        }
        else if (trangThaiHienTai === "Đã hủy" || trangThaiHienTai === "Đã giao/Đã thanh toán" || trangThaiHienTai === "Hoàn thành") {
            // TẦNG 2: 🔥 ĐƠN HÀNG ĐÃ HỦY -> KHÓA CHẾT TOÀN DIỆN, đổi nút Sửa thành "Khóa sửa" disabled, cấm xóa!
            actionButtonsHtml = `
                <button class="btn btn-xs btn-light text-muted" disabled title="Đơn hàng này đã bị hủy, không thể chỉnh sửa thêm bất kỳ thông tin nào!"><i class="bx bx-lock-alt me-1"></i>Khóa sửa</button>
                <button class="btn btn-xs btn-light text-muted ms-1" disabled title="Đơn hàng đã hủy"><i class="bx bx-block me-1"></i>Cấm xóa</button>
            `;
        }
        else {
            // TẦNG 3: Các trạng thái trung gian (Đã duyệt, Đang làm, Hoàn thành, Đã giao...)
            // Vẫn cho bấm nút SỬA để cập nhật thông tin hành chính khách hàng, cho hiện nút Hủy, nhưng CẤM XÓA cứng.
            actionButtonsHtml = `
                <button class="btn btn-xs btn-outline-warning" onclick="editOrder(${id})" title="Chỉnh sửa thông tin hành chính & trạng thái"><i class="bx bx-edit-alt me-1"></i>Sửa</button>
                ${huyDonBtnHtml}
                <button class="btn btn-xs btn-light text-muted ms-1" disabled title="Chỉ được phép thực hiện xóa đơn Chờ xử lý"><i class="bx bx-block me-1"></i>Cấm xóa</button>
            `;
        }

        const iconAnh = laCustom ? "../assets/img/icons/unicons/cc-primary.png" : "../assets/img/icons/unicons/wallet-info.png";
        const nhatKyLoai = laCustom ? `<span class="badge bg-label-warning mb-2 d-inline-block">ĐƠN CUSTOM</span>` : `<span class="badge bg-label-secondary mb-2 d-inline-block">ĐƠN TIÊU CHUẨN</span>`;

        grid.innerHTML += `
            <div class="col-12 col-sm-6 col-md-4 col-xl-3">
                <div class="card bakery-card shadow-sm">
                    <div class="card-img-container" title="Bấm vào xem chi tiết" onclick="showOrderDetails(${id}, ${laCustom})">
                        <img src="${iconAnh}" alt="Đơn hàng">
                    </div>
                    
                    ${statusHtml}
                    <div>${nhatKyLoai}</div>
                    <div class="bakery-title">#${maHienThi}</div>
                    <div class="text-muted small mb-2">Khách: <strong>${tenKhach}</strong></div>
                    <div class="bakery-price mb-2">${tongTien}</div>
                    
                    <div class="d-flex gap-1 mt-2 justify-content-center flex-wrap">
                        ${actionButtonsHtml}
                    </div>
                    ${xmlBtnHtml}
                </div>
            </div>`;
    });
}

// 🔥 NÚT HỦY ĐƠN HÀNG NGAY BÊN NGOÀI GIAO DIỆN QUẦY CARD
async function clickHuyDonHangSieuToc(donHangId, maHienThi) {
    if (confirm(`⚠️ XÁC NHẬN: Bạn có chắc chắn muốn HỦY đơn hàng [ ${maHienThi} ] này ngay ngoài quầy không? Thao tác này sẽ cập nhật trực tiếp vào SQL Server.`)) {
        try {
            const response = await fetch(`${API_URL}/api/DonHang/CapNhatTrangThaiDonHang/${donHangId}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    TrangThaiMoi: "Đã hủy",
                    TenNhanVien: localStorage.getItem('userName') || "Võ Hoàng Thịnh"
                })
            });

            if (response.ok) {
                alert(`🚫 Hệ thống đã hủy thành công đơn hàng ${maHienThi}!`);
                fetchAllOrders(); // Reload làm tươi lại lưới card đơn hàng tức thì
            } else {
                alert("Hủy đơn hàng thất bại! Vui lòng kiểm tra lại API.");
            }
        } catch (error) { console.error("Lỗi gọi lệnh hủy đơn:", error); }
    }
}

async function deleteOrder(id) {
    const dh = globalOrders.find(o => (o.donHangId === id || o.DonHangId === id));
    if (!dh) return;
    const trangThai = dh.trangThai || dh.TrangThai || 'Chờ xử lý';
    if (trangThai !== "Chờ xử lý") {
        alert(`❌ TỪ CHỐI XÓA: Đơn hàng đã chuyển sang giai đoạn [${trangThai}]. Quy trình không cho phép xóa!`);
        return;
    }
    if (confirm(`Bạn có chắc chắn muốn xóa hoàn toàn đơn hàng #${id} khỏi hệ thống không?`)) {
        try {
            const response = await fetch(`${API_URL}/api/DonHang/XoaDonHang/${id}`, { method: 'DELETE' });
            if (response.ok) { alert("Đã xóa đơn hàng thành công!"); fetchAllOrders(); }
            else { alert("Xóa đơn hàng thất bại!"); }
        } catch (error) { console.error(error); }
    }
}

function editOrder(id) { window.location.href = `add-order.html?id=${id}`; }
function filterByType(type) { currentTypeFilter = type; document.getElementById('btnTabAll').classList.remove('active'); document.getElementById('btnTabThuong').classList.remove('active'); document.getElementById('btnTabCustom').classList.remove('active'); if (type === 'all') document.getElementById('btnTabAll').classList.add('active'); if (type === 'standard') document.getElementById('btnTabThuong').classList.add('active'); if (type === 'custom') document.getElementById('btnTabCustom').classList.add('active'); executeCombinedFilter(); }
function filterByStatus() { currentStatusFilter = document.getElementById('filterStatus').value; executeCombinedFilter(); }
function sortOrders() { const sort = document.getElementById('sortOrder').value; if (sort === "newest") { globalOrders.sort((a, b) => (b.donHangId || b.DonHangId) - (a.donHangId || a.DonHangId)); } else { globalOrders.sort((a, b) => (a.donHangId || a.DonHangId) - (b.donHangId || b.DonHangId)); } executeCombinedFilter(); }
document.addEventListener('DOMContentLoaded', fetchAllOrders);

async function nextStepStatus(donHangId, trangThaiMoi) {
    try {
        const response = await fetch(`${API_URL}/api/DonHang/CapNhatTrangThaiDonHang/${donHangId}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ TrangThaiMoi: trangThaiMoi, TenNhanVien: localStorage.getItem('userName') || "Võ Hoàng Thịnh" })
        });
        if (response.ok) { alert(`Đã chuyển đơn sang trạng thái: [${trangThaiMoi}]`); fetchAllOrders(); }
        else { alert("Cập nhật trạng thái thất bại!"); }
    } catch (error) { console.error(error); }
}

// Thay thế toàn bộ hàm showOrderDetails cũ trong file order-grid.js bằng đoạn này:
async function showOrderDetails(donHangId, laCustom) {
    const dh = globalOrders.find(o => (o.donHangId === donHangId || o.DonHangId === donHangId));
    if (!dh) return;

    document.getElementById('modalOrderTitle').innerText = `Chi Tiết Đơn Hàng: #${dh.maDhHienThi || 'DH' + donHangId}`;
    document.getElementById('modalTenNguoiNhan').value = dh.tenNguoiNhan || dh.TenNguoiNhan || '👤 Khách hàng vãng lai';
    document.getElementById('modalSdtNguoiNhan').value = dh.sdtNguoiNhan || dh.SdtNguoiNhan || 'Không có';
    document.getElementById('modalDiaChiGiao').value = dh.diaChiGiao || dh.DiaChiGiao || 'Nhận tại tiệm bánh';
    document.getElementById('modalTongTien').value = new Intl.NumberFormat('vi-VN').format(dh.tongTien || dh.TongTien || 0) + ' đ';
    document.getElementById('modalNgayDat').value = new Date(dh.ngayDatHang || dh.NgayDatHang || Date.now()).toLocaleString('vi-VN');

    const customSection = document.getElementById('modalCustomSection');
    const pTableBody = document.getElementById('modalProductsTableBody');

    if (pTableBody) pTableBody.innerHTML = '';

    let tongTienGioHang = 0;
    let itemsHtml = '';
    let customRowHtml = ''; // Tạo biến hứng hàng Custom tách biệt

    // 1. TẢI CÁC MÓN BÁNH TIÊU CHUẨN ĐI KÈM (NẾU CÓ)
    try {
        const resItems = await fetch(`${API_URL}/api/DonHang/LayChiTietSanPhams/${donHangId}`);
        if (resItems.ok) {
            const items = await resItems.json();
            items.forEach(item => {
                let imgUrl = item.hinhAnh || item.HinhAnh || '';
                if (imgUrl && !imgUrl.startsWith('http') && !imgUrl.startsWith('../')) { imgUrl = '../' + imgUrl; }
                else if (!imgUrl) { imgUrl = '../assets/img/icons/unicons/wallet-info.png'; }

                const name = item.tenSanPham || item.TenSanPham || 'Sản phẩm bánh';
                const qty = item.soLuong || item.SoLuong || 0;
                const priceGoc = item.donGia || item.DonGia || 0;

                tongTienGioHang += (qty * priceGoc);
                const priceFmt = new Intl.NumberFormat('vi-VN').format(priceGoc) + ' đ';

                itemsHtml += `
                    <tr>
                        <td><img src="${imgUrl}" width="35" height="35" class="rounded" style="object-fit:cover;"></td>
                        <td><span class="fw-semibold">${name}</span></td>
                        <td class="text-center"><span class="badge bg-label-secondary">${qty}</span></td>
                        <td><strong class="text-dark">${priceFmt}</strong></td>
                    </tr>`;
            });
        }
    } catch (error) { console.error(error); }

    // 2. TẢI THÔNG TIN ĐƠN HÀNG CUSTOM (NẾU LÀ ĐƠN CUSTOM)
    if (laCustom) {
        if (customSection) customSection.style.display = "block";
        try {
            const response = await fetch(`${API_URL}/api/DonHang/KiemTraDonHang/${donHangId}`);
            if (response.ok) {
                const resData = await response.json();
                const checkCustom = resData.laDonCustom !== undefined ? resData.laDonCustom : resData.LaDonCustom;
                const customData = resData.data !== undefined ? resData.data : resData.Data;

                if (checkCustom && customData) {
                    const loaiYeuCau = customData.loaiYeuCau || customData.LoaiYeuCau || customData.loai_yeu_cau || 'Bánh đặt theo yêu cầu';

                    // 🔥 ĐÃ VÁ LỖI CHÍNH TẢ: Chuyển hết thành kichThuocSoLuong (Chữ L viết HOA) để map chuẩn với API C#
                    const kichThuoc = customData.kichThuocSoLuong || customData.KichThuocSoLuong || customData.kichThuocSoluong || customData.KichThuocSoluong || 'Chưa rõ quy cách';

                    const mauSac = customData.mauSacChuDao || customData.MauSacChuDao || 'Theo mẫu gửi';
                    const ghiChu = customData.ghiChu || customData.GhiChu || 'Không có';
                    const ngayLay = customData.ngayLayHang || customData.NgayLayHang;
                    const customImgUrl = customData.hinhAnh || customData.HinhAnh || '';

                    let totalDonGoc = dh.tongTien || dh.TongTien || 0;
                    let giamGiaGoc = dh.soTienGiam || dh.SoTienGiam || 0;
                    let tienCustom = totalDonGoc + giamGiaGoc - tongTienGioHang;
                    if (tienCustom < 0) tienCustom = 0;

                    document.getElementById('modalLoaiYeuCau').value = loaiYeuCau;
                    document.getElementById('modalKichThuoc').value = kichThuoc;
                    document.getElementById('modalMauSac').value = mauSac;
                    document.getElementById('modalGhiChuCustom').value = ghiChu;
                    document.getElementById('modalNgayLay').value = ngayLay ? new Date(ngayLay).toLocaleDateString('vi-VN') : 'Lấy trong ngày';

                    const modalBaoGia = document.getElementById('modalBaoGiaCustom');
                    if (modalBaoGia) {
                        modalBaoGia.value = new Intl.NumberFormat('vi-VN').format(tienCustom) + ' đ';
                    }

                    const imgContainer = document.getElementById('modalCustomImgContainer');
                    let imgDisplayUrl = '';

                    if (customImgUrl && customImgUrl !== 'string' && customImgUrl.trim() !== '') {
                        imgDisplayUrl = customImgUrl;
                        if (imgContainer) {
                            imgContainer.innerHTML = `<img src="${customImgUrl}" alt="Ảnh mẫu" class="img-fluid rounded" style="max-height: 180px; object-fit: contain; box-shadow: 0 4px 8px rgba(0,0,0,0.1);">`;
                        }
                    } else {
                        imgDisplayUrl = '../assets/img/icons/unicons/cc-primary.png';
                        if (imgContainer) { imgContainer.innerHTML = `<span class="text-muted small">Khách không gửi kèm hình ảnh mẫu</span>`; }
                    }

                    customRowHtml = `
                        <tr class="table-warning" style="background-color: #fff3cd !important;">
                            <td><img src="${imgDisplayUrl}" width="35" height="35" class="rounded border border-warning" style="object-fit:cover;"></td>
                            <td><span class="fw-bold text-warning"><i class="bx bx-star bx-tada me-1"></i> [BÁNH THIẾT KẾ] - ${loaiYeuCau}</span></td>
                            <td class="text-center"><span class="badge bg-warning">1</span></td>
                            <td><strong class="text-warning">${new Intl.NumberFormat('vi-VN').format(tienCustom)} đ</strong></td>
                        </tr>`;
                }
            }
        } catch (error) { console.error(error); }
    } else {
        if (customSection) customSection.style.display = "none";
    }

    // ========================================================
    // 🔏 THUẬT TOÁN ĐÓNG GÓI: KẾT HỢP DỮ LIỆU ĐỂ IN RA BẢNG HÓA ĐƠN
    // ========================================================
    let finalTableHtml = customRowHtml + itemsHtml;

    // Đọc trực tiếp dữ liệu mã và % giảm gốc từ API trả về
    let soTienGiamGoc = dh.soTienGiam || dh.SoTienGiam || 0;
    let tenMaCode = dh.tenMaCode || dh.TenMaCode || '';
    let phanTramGiamGoc = dh.phanTramGiamGoc || dh.PhanTramGiamGoc || 0;

    // Nếu đơn hàng có tiền giảm lớn hơn 0, in thẳng tên mã và % gốc ra
    if (soTienGiamGoc > 0) {
        let textHienThiKM = tenMaCode
            ? `Áp dụng Mã [ ${tenMaCode} ] (Giảm ${phanTramGiamGoc}%):`
            : `Áp dụng Khuyến Mãi (Giảm ${phanTramGiamGoc}%):`;

        finalTableHtml += `
            <tr style="background-color: #e8fadf; border-top: 2px dashed #71dd37;">
                <td colspan="3" class="text-end align-middle pb-2 pt-2">
                    <span class="fw-bold text-success"><i class="bx bxs-discount bx-tada me-1"></i> ${textHienThiKM}</span>
                </td>
                <td class="pb-2 pt-2">
                    <strong class="text-success">- ${new Intl.NumberFormat('vi-VN').format(soTienGiamGoc)} đ</strong>
                </td>
            </tr>`;
    }

    // Đổ dữ liệu vào HTML bảng Modal
    if (pTableBody) {
        pTableBody.innerHTML = finalTableHtml || `<tr><td colspan="4" class="text-center text-muted py-2">Đơn hàng trống!</td></tr>`;
    }

    // Mở Modal bung lụa
    const myModal = new bootstrap.Modal(document.getElementById('orderDetailModal'));
    myModal.show();
}
function executeCombinedFilter() {
    let filtered = [...globalOrders];
    if (currentTypeFilter === "standard") { filtered = filtered.filter(d => d.laDonCustom === false || d.LaDonCustom === false); }
    else if (currentTypeFilter === "custom") { filtered = filtered.filter(d => d.laDonCustom === true || d.LaDonCustom === true); }
    if (currentStatusFilter !== "All") { filtered = filtered.filter(d => (d.trangThai === currentStatusFilter || d.TrangThai === currentStatusFilter)); }
    const searchInput = document.getElementById('searchOrderInput');
    if (searchInput) {
        const searchText = searchInput.value.toLowerCase().trim();
        if (searchText !== "") {
            filtered = filtered.filter(d => {
                const maHienThi = (d.maDhHienThi || `DH${d.donHangId || d.DonHangId}`).toLowerCase();
                const tenKhach = (d.tenNguoiNhan || d.TenNguoiNhan || 'Khách vãng lai').toLowerCase();
                return maHienThi.includes(searchText) || tenKhach.includes(searchText);
            });
        }
    }
    renderOrderGrid(filtered);
}