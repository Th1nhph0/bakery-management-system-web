const API_URL = "https://localhost:7122";
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

// 2. PHÂN TÍCH TIẾN ĐỘ TIẾP THEO ĐỂ HOVER BIẾN ĐỔI
function getStatusConfig(currentStatus) {
    switch (currentStatus) {
        case "Chờ xử lý":
            return { next: "Đã duyệt", class: "bg-label-secondary", btnClass: "btn-primary", icon: "bx-check-shield" };
        case "Đã duyệt":
            return { next: "Đang làm", class: "bg-label-primary", btnClass: "btn-warning", icon: "bx-wrench" };
        case "Đang làm":
            return { next: "Hoàn thành", class: "bg-label-warning", btnClass: "btn-success", icon: "bx-package" };
        case "Hoàn thành":
            return { next: "Đã giao/Đã thanh toán", class: "bg-label-success", btnClass: "btn-info", icon: "bx-money" };
        default:
            return { next: null, class: "bg-label-info", btnClass: "", icon: "" };
    }
}

// 3. HIỂN THỊ DỮ LIỆU RA LƯỚI CARD (CẬP NHẬT THÊM NÚT THAO TÁC SỬA/XÓA)
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
        const tenKhach = dh.tenNguoiNhan || 'Khách vãng lai';
        const tongTien = new Intl.NumberFormat('vi-VN').format(dh.tongTien || 0) + ' đ';
        const trangThaiHienTai = dh.trangThai || dh.TrangThai || 'Chờ xử lý';
        const laCustom = dh.laDonCustom || dh.LaDonCustom;

        const cfg = getStatusConfig(trangThaiHienTai);

        let xmlBtnHtml = '';
        if (trangThaiHienTai !== "Chờ xử lý") {
            xmlBtnHtml = `
                <a href="${API_URL}/api/DonHang/XuatHoaDonXMLDonHang/${id}" target="_blank" class="btn btn-xs btn-outline-secondary w-100 mt-2">
                    <i class="bx bx-file-blank me-1"></i> Xuất Hóa Đơn XML
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
                    
                    <div class="d-flex gap-2 mt-2 justify-content-center">
                        <button class="btn btn-xs btn-outline-warning" onclick="editOrder(${id})"><i class="bx bx-edit-alt me-1"></i>Sửa</button>
                        <button class="btn btn-xs btn-outline-danger" onclick="deleteOrder(${id})"><i class="bx bx-trash me-1"></i>Xóa</button>
                    </div>
                    ${xmlBtnHtml}
                </div>
            </div>`;
    });
}

// 🔥 BỔ SUNG: HÀM XÓA ĐƠN HÀNG GỌI API DELETE
async function deleteOrder(id) {
    if (confirm(`Bạn có chắc chắn muốn xóa hoàn toàn đơn hàng #${id} khỏi hệ thống không?`)) {
        try {
            const response = await fetch(`${API_URL}/api/DonHang/XoaDonHang/${id}`, { method: 'DELETE' });
            if (response.ok) {
                alert("Đã xóa đơn hàng thành công!");
                fetchAllOrders(); // Reload lưới dữ liệu
            } else {
                const err = await response.json();
                alert(err.message || "Xóa đơn hàng thất bại!");
            }
        } catch (error) { console.error("Lỗi xóa đơn:", error); }
    }
}

// 🔥 ĐIỀU HƯỚNG SANG TRANG CẬP NHẬT ĐƠN HÀNG
function editOrder(id) {
    window.location.href = `add-order.html?id=${id}`;
}

// 🔥 4. CẬP NHẬT: HÀM NẠP VÀ HIỂN THỊ CHI TIẾT ĐƠN HÀNG LÊN MODAL POP-UP
async function showOrderDetails(donHangId, laCustom) {
    // A. Dò tìm thông tin đơn hàng có sẵn trong mảng globalOrders
    const dh = globalOrders.find(o => (o.donHangId === donHangId || o.DonHangId === donHangId));
    if (!dh) return;

    // B. Đổ các dữ liệu đơn hàng cơ bản vào các ô input trong Modal
    document.getElementById('modalOrderTitle').innerText = `Chi Tiết Đơn Hàng: #${dh.maDhHienThi || 'DH' + donHangId}`;
    document.getElementById('modalTenNguoiNhan').value = dh.tenNguoiNhan || 'Khách vãng lai';
    document.getElementById('modalSdtNguoiNhan').value = dh.sdtNguoiNhan || 'Không có';
    document.getElementById('modalDiaChiGiao').value = dh.diaChiGiao || 'Nhận tại tiệm bánh';
    document.getElementById('modalTongTien').value = new Intl.NumberFormat('vi-VN').format(dh.tongTien || 0) + ' đ';
    document.getElementById('modalNgayDat').value = new Date(dh.ngayDatHang).toLocaleString('vi-VN');

    const customSection = document.getElementById('modalCustomSection');

    // C. Kiểm tra nếu là đơn đặt Custom -> Bật vùng hiển thị bổ sung và gọi API KiemTraDonHang
    if (laCustom) {
        customSection.style.display = "block";
        try {
            const response = await fetch(`${API_URL}/api/DonHang/KiemTraDonHang/${donHangId}`);
            if (response.ok) {
                const resData = await response.json();
                if (resData.laDonCustom && resData.data) {
                    const cData = resData.data;
                    document.getElementById('modalLoaiYeuCau').value = cData.loaiYeuCau || 'Bánh kem tự thiết kế';
                    document.getElementById('modalKichThuoc').value = cData.kichThuocSoluong || 'Chưa rõ quy cách';
                    document.getElementById('modalMauSac').value = cData.mauSacChuDao || 'Theo mẫu gửi';
                    document.getElementById('modalGhiChuCustom').value = cData.ghiChu || 'Không có yêu cầu ghi chú viết chữ';
                    document.getElementById('modalNgayLay').value = cData.ngayLayHang ? new Date(cData.ngayLayHang).toLocaleDateString('vi-VN') : 'Lấy trong ngày';
                }
            }
        } catch (error) { console.error("Lỗi nạp dữ liệu đơn Custom:", error); }
    } else {
        // Nếu là đơn hàng thường mua bánh có sẵn thì đóng phân vùng Custom lại cho gọn mắt
        customSection.style.display = "none";
    }

    // D. Lệnh kích hoạt nổ Modal lên màn hình
    const myModal = new bootstrap.Modal(document.getElementById('orderDetailModal'));
    myModal.show();
}

// 5. BỘ LỌC KẾT HỢP
function filterByType(type) {
    currentTypeFilter = type;
    document.getElementById('btnTabAll').classList.remove('active');
    document.getElementById('btnTabThuong').classList.remove('active');
    document.getElementById('btnTabCustom').classList.remove('active');

    if (type === 'all') document.getElementById('btnTabAll').classList.add('active');
    if (type === 'standard') document.getElementById('btnTabThuong').classList.add('active');
    if (type === 'custom') document.getElementById('btnTabCustom').classList.add('active');

    executeCombinedFilter();
}

function filterByStatus() {
    currentStatusFilter = document.getElementById('filterStatus').value;
    executeCombinedFilter();
}

function executeCombinedFilter() {
    let filtered = [...globalOrders];

    if (currentTypeFilter === "standard") {
        filtered = filtered.filter(d => d.laDonCustom === false || d.LaDonCustom === false);
    } else if (currentTypeFilter === "custom") {
        filtered = filtered.filter(d => d.laDonCustom === true || d.LaDonCustom === true);
    }

    if (currentStatusFilter !== "All") {
        filtered = filtered.filter(d => (d.trangThai === currentStatusFilter || d.TrangThai === currentStatusFilter));
    }

    renderOrderGrid(filtered);
}

// 6. CHUYỂN TRẠNG THÁI TIẾN ĐỘ KHÔNG CẦN F5 TRANG
async function nextStepStatus(donHangId, trangThaiMoi) {
    try {
        const response = await fetch(`${API_URL}/api/DonHang/CapNhatTrangThaiDonHang/${donHangId}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                TrangThaiMoi: trangThaiMoi,
                TenNhanVien: "Võ Hoàng Thịnh"
            })
        });

        if (response.ok) {
            alert(`Đã chuyển đơn sang trạng thái: [${trangThaiMoi}]`);
            fetchAllOrders();
        } else {
            alert("Cập nhật trạng thái thất bại!");
        }
    } catch (error) { console.error("Lỗi cập nhật trạng thái:", error); }
}

// 7. SẮP XẾP ĐƠN HÀNG
function sortOrders() {
    const sort = document.getElementById('sortOrder').value;
    if (sort === "newest") {
        globalOrders.sort((a, b) => b.donHangId - a.donHangId);
    } else {
        globalOrders.sort((a, b) => a.donHangId - b.donHangId);
    }
    executeCombinedFilter();
}

document.addEventListener('DOMContentLoaded', fetchAllOrders);

// 🔥 CẬP NHẬT: Hàm nạp toàn bộ danh sách bánh và ảnh mẫu Custom lên Modal Pop-up (Bản Vá Lỗi Trống Thông Tin)
async function showOrderDetails(donHangId, laCustom) {
    const dh = globalOrders.find(o => (o.donHangId === donHangId || o.DonHangId === donHangId));
    if (!dh) return;

    // A. Đổ thông tin cơ bản
    document.getElementById('modalOrderTitle').innerText = `Chi Tiết Đơn Hàng: #${dh.maDhHienThi || 'DH' + donHangId}`;
    document.getElementById('modalTenNguoiNhan').value = dh.tenNguoiNhan || 'Khách vãng lai';
    document.getElementById('modalSdtNguoiNhan').value = dh.sdtNguoiNhan || 'Không có';
    document.getElementById('modalDiaChiGiao').value = dh.diaChiGiao || 'Nhận tại tiệm bánh';
    document.getElementById('modalTongTien').value = new Intl.NumberFormat('vi-VN').format(dh.tongTien || 0) + ' đ';
    document.getElementById('modalNgayDat').value = new Date(dh.ngayDatHang).toLocaleString('vi-VN');

    const customSection = document.getElementById('modalCustomSection');

    // C. Kiểm tra nếu là đơn đặt Custom -> Bật vùng hiển thị bổ sung và gọi API KiemTraDonHang
    if (laCustom) {
        customSection.style.display = "block";
        try {
            const response = await fetch(`${API_URL}/api/DonHang/KiemTraDonHang/${donHangId}`);
            if (response.ok) {
                const resData = await response.json();

                // 🛡️ BẢO VỆ RADAR: Kiểm tra bọc lót cả chữ hoa lẫn chữ thường từ API trả về
                const checkCustom = resData.laDonCustom !== undefined ? resData.laDonCustom : resData.LaDonCustom;
                const customData = resData.data !== undefined ? resData.data : resData.Data;

                if (checkCustom && customData) {
                    // Áp dụng chống phân biệt hoa thường cho toàn bộ thuộc tính con của bảng DonBanhCustom
                    const loaiYeuCau = customData.loaiYeuCau !== undefined ? customData.loaiYeuCau : customData.LoaiYeuCau;
                    const kichThuoc = customData.kichThuocSoluong !== undefined ? customData.kichThuocSoluong : customData.KichThuocSoluong;
                    const mauSac = customData.mauSacChuDao !== undefined ? customData.mauSacChuDao : customData.MauSacChuDao;
                    const ghiChu = customData.ghiChu !== undefined ? customData.ghiChu : customData.GhiChu;
                    const ngayLay = customData.ngayLayHang !== undefined ? customData.ngayLayHang : customData.NgayLayHang;

                    document.getElementById('modalLoaiYeuCau').value = loaiYeuCau || 'Bánh đặt theo yêu cầu';
                    document.getElementById('modalKichThuoc').value = kichThuoc || 'Chưa rõ quy cách';
                    document.getElementById('modalMauSac').value = mauSac || 'Theo mẫu gửi';
                    document.getElementById('modalGhiChuCustom').value = ghiChu || 'Không có yêu cầu ghi chú viết chữ';
                    document.getElementById('modalNgayLay').value = ngayLay ? new Date(ngayLay).toLocaleDateString('vi-VN') : 'Lấy trong ngày';

                    // HIỂN THỊ HÌNH ẢNH MẪU CUSTOM ĐẸP MẮT
                    const imgContainer = document.getElementById('modalCustomImgContainer');
                    let imgUrl = item.hinhAnh || item.HinhAnh || '';
                    if (imgUrl && !imgUrl.startsWith('http') && !imgUrl.startsWith('../')) {
                        imgUrl = '../' + imgUrl;
                    } else if (!imgUrl) {
                        imgUrl = '../assets/img/illustrations/page-misc-under-maintenance.png'; // Ảnh bọc lót nếu món đó không có ảnh
                    }
                    if (imgContainer) {
                        if (customImgUrl) {
                            imgContainer.innerHTML = `<img src="${customImgUrl}" alt="Ảnh mẫu từ khách" class="img-fluid rounded" style="max-height: 180px; object-fit: contain; box-shadow: 0 4px 8px rgba(0,0,0,0.1);">`;
                        } else {
                            imgContainer.innerHTML = `<span class="text-muted small"><i class="bx bx-image-alt me-1"></i> Khách hàng không gửi kèm hình ảnh mẫu</span>`;
                        }
                    }
                }
            }
        } catch (error) { console.error("Lỗi nạp dữ liệu đơn Custom:", error); }
    } else {
        if (customSection) customSection.style.display = "none";
    }

    // B. GỌI API LẤY DANH SÁCH SẢN PHẨM ĐÃ MUA (Dành cho cả đơn hàng thường và đơn custom)
    try {
        const resItems = await fetch(`${API_URL}/api/DonHang/LayChiTietSanPhams/${donHangId}`);
        if (resItems.ok) {
            const items = await resItems.json();
            const pTableBody = document.getElementById('modalProductsTableBody');
            // 🔥 BỔ SUNG: Nếu đơn hàng cũ có lưu đường link ảnh, ép khung hình hiện ảnh lên ngay
            if (hinhAnh) {
                const previewContainer = document.getElementById('editPreviewContainer');
                const previewImg = document.getElementById('customImgPreview');
                if (previewContainer && previewImg) {
                    previewImg.src = hinhAnh;
                    previewContainer.style.display = "block"; // Mở hiển thị khối ảnh
                }
            }
            if (pTableBody) {
                pTableBody.innerHTML = '';

                if (items.length === 0) {
                    pTableBody.innerHTML = `<tr><td colspan="4" class="text-center text-muted py-2">Đơn hàng trống hoặc chưa lưu món!</td></tr>`;
                } else {
                    items.forEach(item => {
                        const imgUrl = item.hinhAnh || item.HinhAnh || '../assets/img/illustrations/page-misc-under-maintenance.png';
                        const name = item.tenSanPham || item.TenSanPham || 'Sản phẩm bánh';
                        const qty = item.soLuong || item.SoLuong || 0;
                        const price = new Intl.NumberFormat('vi-VN').format(item.donGia || item.DonGia || 0) + ' đ';

                        pTableBody.innerHTML += `
                            <tr>
                                <td><img src="${imgUrl}" width="35" height="35" class="rounded" style="object-fit:cover;"></td>
                                <td><span class="fw-semibold">${name}</span></td>
                                <td class="text-center"><span class="badge bg-label-secondary">${qty}</span></td>
                                <td><strong class="text-dark">${price}</strong></td>
                            </tr>`;
                    });
                }
            }
        }
    } catch (error) { console.error("Lỗi nạp danh sách sản phẩm của đơn hàng:", error); }

    // D. Lệnh kích hoạt nổ Modal lên màn hình
    const myModal = new bootstrap.Modal(document.getElementById('orderDetailModal'));
    myModal.show();
}