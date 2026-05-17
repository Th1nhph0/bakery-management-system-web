const API_URL = "https://localhost:7122";
let gioHangTam = [];
let isCustomOrder = false; // Biến cờ kiểm soát trạng thái gửi đi toàn cục

// 1. KHỞI ĐỘNG HỆ THỐNG TRANG FORM
document.addEventListener('DOMContentLoaded', async () => {
    // Ép hệ thống phải đợi tải xong hết toàn bộ các Dropdown dữ liệu nền từ DB lên trước
    await Promise.all([loadKhachHangs(), loadSanPhams(), loadKhuyenMais()]);

    // 🔥 KÍCH HOẠT TÍNH NĂNG GÕ TÌM KIẾM CHO CÁC Ô CHỌN (SỬ DỤNG JQUERY SELECT2)
    if ($.fn.select2) {
        $('#selectKhachHang').select2({ width: '100%' });
        $('#selectSanPham').select2({ width: '100%' });
        $('#selectKhuyenMai').select2({ width: '100%' });
    }

    // Gắn sự kiện lắng nghe công tắc gạt Custom Order
    const toggleSwitch = document.getElementById('checkIsCustomOrder');
// ... (Toàn bộ code bên dưới giữ nguyên) ...
    if (toggleSwitch) {
        toggleSwitch.addEventListener('change', function () {
            isCustomOrder = this.checked; // ĐỒNG BỘ TRẠNG THÁI VỚI BIẾN TOÀN CỤC KHI GẠT
            const customSection = document.getElementById('formCustomOrderFields');
            const previewContainer = document.getElementById('editPreviewContainer');

            if (this.checked) {
                customSection.style.display = "block";
                // Nếu có link ảnh sẵn trong input ẩn thì cho hiện lại khung preview
                const currentImg = document.getElementById('customHinhAnh').value;
                if (currentImg && previewContainer) previewContainer.style.display = "block";
            } else {
                customSection.style.display = "none";
                if (previewContainer) previewContainer.style.display = "none";
                clearCustomFields();
            }
        });
    }

    // Tiến hành check dữ liệu sửa đơn
    checkEditMode();

    const form = document.getElementById('formAddOrder');
    if (form) form.addEventListener('submit', submitOrderForm);
});

// 2. HÀM CHUYỂN ĐỔI CHẾ ĐỘ EDIT MODE - CHỐNG LỖI HOA/THƯỜNG & HIỂN THỊ ẢNH CŨ
async function checkEditMode() {
    const urlParams = new URLSearchParams(window.location.search);
    const editId = urlParams.get('id');

    const formTitle = document.querySelector('.card-header h5');
    const submitBtn = document.querySelector('button[type="submit"]');

    if (!editId) return;

    if (formTitle) formTitle.innerText = "Cập Nhật Thông Tin Đơn Hàng";
    if (submitBtn) {
        submitBtn.innerText = "Lưu Cập Nhật Đơn Hàng";
        submitBtn.classList.replace('btn-primary', 'btn-warning');
    }

    // A. Lấy thông tin đơn hàng chung hành chính
    // A. Lấy thông tin đơn hàng chung hành chính
    try {
        const response = await fetch(`${API_URL}/api/DonHang/LayDonHangTheoId/${editId}`);
        if (response.ok) {
            const dh = await response.json();

            const khachHangId = dh.khachHangId !== undefined ? dh.khachHangId : (dh.KhachHangId || '');
            const khuyenMaiId = dh.khuyenMaiId !== undefined ? dh.khuyenMaiId : (dh.KhuyenMaiId || '');

            $('#selectKhachHang').val(khachHangId).trigger('change');
            $('#selectKhuyenMai').val(khuyenMaiId).trigger('change');

            document.getElementById('tenNguoiNhan').value = dh.tenNguoiNhan !== undefined ? dh.tenNguoiNhan : (dh.TenNguoiNhan || '');
            document.getElementById('sdtNguoiNhan').value = dh.sdtNguoiNhan !== undefined ? dh.sdtNguoiNhan : (dh.SdtNguoiNhan || '');
            document.getElementById('diaChiGiao').value = dh.diaChiGiao !== undefined ? dh.diaChiGiao : (dh.DiaChiGiao || '');

            const rowTrangThai = document.getElementById('rowTrangThaiDonHang');
            if (rowTrangThai) rowTrangThai.style.display = 'block'; // Hiện khung đỏ lên

            const trangThaiHienTai = dh.trangThai !== undefined ? dh.trangThai : (dh.TrangThai || 'Chờ xử lý');
            document.getElementById('selectTrangThai').value = trangThaiHienTai;
            // 🔥 VÁ LỖI HIỂN THỊ TIỀN BÁO GIÁ: Trả lại tiền khuyến mãi vào tổng tiền gốc trước khi tách
            const tongTienTra = dh.tongTien !== undefined ? dh.tongTien : (dh.TongTien || 0);
            const tienGiam = dh.soTienGiam !== undefined ? dh.soTienGiam : (dh.SoTienGiam || 0);

            // Cộng ngược lại tiền đã giảm để ra tổng giá trị thật của đơn hàng
            const tongTienGoc = tongTienTra + tienGiam;

            // Cất cái tổng tiền gốc này vào để khúc dưới bốc ra tính toán
            document.getElementById('customBaoGia').setAttribute('data-tongtien', tongTienGoc);
        }
    } catch (e) { console.error("Lỗi nạp đơn hàng chung:", e); }

    // B. Kiểm tra và bốc dữ liệu Đơn bánh Custom lên Form
    try {
        const resCheck = await fetch(`${API_URL}/api/DonHang/KiemTraDonHang/${editId}`);
        if (resCheck.ok) {
            const resData = await resCheck.json();

            const laDonCustom = resData.laDonCustom !== undefined ? resData.laDonCustom : resData.LaDonCustom;
            const customData = resData.data !== undefined ? resData.data : resData.Data;

            if (laDonCustom && customData) {
                isCustomOrder = true;

                // Bật công tắc gạt sang trạng thái kích hoạt và mở phân vùng
                document.getElementById('checkIsCustomOrder').checked = true;
                document.getElementById('formCustomOrderFields').style.display = "block";

                // Trích xuất an toàn các thuộc tính từ API
                const loaiYeuCau = customData.loaiYeuCau !== undefined ? customData.loaiYeuCau : customData.LoaiYeuCau;
                const kichThuoc = customData.kichThuocSoluong !== undefined ? customData.kichThuocSoluong : customData.KichThuocSoluong;
                const mauSac = customData.mauSacChuDao !== undefined ? customData.mauSacChuDao : customData.MauSacChuDao;
                const ghiChu = customData.ghiChu !== undefined ? customData.ghiChu : customData.GhiChu;
                const ngayLay = customData.ngayLayHang !== undefined ? customData.ngayLayHang : customData.NgayLayHang;
                const hinhAnh = customData.hinhAnh !== undefined ? customData.hinhAnh : customData.HinhAnh;

                // Điền thông tin chữ vào form
                document.getElementById('customLoaiYeuCau').value = loaiYeuCau || '';
                document.getElementById('customKichThuoc').value = kichThuoc || '';
                document.getElementById('customMauSac').value = mauSac || '';
                document.getElementById('customGhiChu').value = ghiChu || '';
                if (ngayLay) {
                    document.getElementById('customNgayLay').value = ngayLay.split('T')[0];
                }
                document.getElementById('customHinhAnh').value = hinhAnh || '';

                // 🔥 VÁ LỖI: Ép hiển thị bức ảnh mẫu cũ đã lưu trong DB lên khung Preview hình ảnh
                if (hinhAnh && hinhAnh !== 'string') {
                    const previewContainer = document.getElementById('editPreviewContainer');
                    const previewImg = document.getElementById('customImgPreview');
                    if (previewContainer && previewImg) {
                        previewImg.src = hinhAnh;
                        previewContainer.style.display = "block";
                    }
                }
            }
        }
    } catch (e) { console.error("Lỗi nạp thông tin bánh Custom:", e); }

    // C. Đổ lại giỏ hàng bánh tiêu chuẩn đi kèm (nếu có) VÀ TÍNH TIỀN BÁO GIÁ
    try {
        const resItems = await fetch(`${API_URL}/api/DonHang/LayChiTietSanPhams/${editId}`);
        if (resItems.ok) {
            const items = await resItems.json();
            gioHangTam = [];
            let tongTienGioHang = 0; // Biến tính tổng tiền các món bánh tiêu chuẩn

            items.forEach(item => {
                const sl = item.soLuong !== undefined ? item.soLuong : item.SoLuong;
                const gia = item.donGia !== undefined ? item.donGia : item.DonGia;

                gioHangTam.push({
                    SanPham_ID: item.sanPhamId !== undefined ? item.sanPhamId : item.SanPhamId,
                    Ten: item.tenSanPham !== undefined ? item.tenSanPham : item.TenSanPham,
                    So_Luong: sl
                });
                tongTienGioHang += (sl * gia); // Cộng tiền giỏ hàng
            });
            renderCartTable();

            // 🔥 VÁ LỖI 2: Tách tiền báo giá Custom = (Tổng tiền đơn hàng) - (Tổng tiền giỏ hàng)
            const tongTienDon = Number(document.getElementById('customBaoGia').getAttribute('data-tongtien')) || 0;
            let tienBaoGia = tongTienDon - tongTienGioHang;
            if (tienBaoGia < 0) tienBaoGia = 0;
            document.getElementById('customBaoGia').value = tienBaoGia; // Đổ đúng tiền báo giá ra ô nhập liệu
        }
    } catch (e) { console.error(e); }
}

// 3. HÀM KÍCH HOẠT UPLOAD FILE TỰ ĐỘNG VÀ ĐỔ ẢNH PREVIEW MỚI LÊN FORM
async function uploadFileTuDong() {
    const fileInput = document.getElementById('fileInputCustom');
    const statusText = document.getElementById('uploadStatus');
    const hiddenInput = document.getElementById('customHinhAnh');

    if (fileInput.files.length === 0) return;

    const file = fileInput.files[0];
    const formData = new FormData();
    formData.append('file', file);

    statusText.innerText = "⏳ Đang tải ảnh lên hệ thống...";
    statusText.className = "text-warning d-block mt-1";

    try {
        const response = await fetch(`${API_URL}/api/DonHang/UploadAnhMauCustom`, {
            method: 'POST',
            body: formData
        });

        if (response.ok) {
            const result = await response.json();
            // Bọc lót radar bắt chuẩn cả thuộc tính chữ hoa lẫn chữ thường từ API trả về
            const linkAnhReal = result.linkAnh !== undefined ? result.linkAnh : result.LinkAnh;

            hiddenInput.value = linkAnhReal;

            statusText.innerText = "✅ Tải ảnh mẫu thành công!";
            statusText.className = "text-success d-block mt-1";

            // 🔥 VÁ LỖI: Ép khung hiển thị ảnh nạp ngay bức ảnh vừa upload thành công lên màn hình Form
            const previewContainer = document.getElementById('editPreviewContainer');
            const previewImg = document.getElementById('customImgPreview');
            if (previewContainer && previewImg && linkAnhReal) {
                previewImg.src = linkAnhReal;
                previewContainer.style.display = "block";
            }
        } else {
            statusText.innerText = "❌ Tải ảnh thất bại. Vui lòng thử lại!";
            statusText.className = "text-danger d-block mt-1";
        }
    } catch (error) {
        console.error("Lỗi upload:", error);
        statusText.innerText = "❌ Lỗi kết nối server!";
    }
}

// 4. ĐÓNG GÓI DỮ LIỆU KHI SUBMIT GỬI LÊN API
async function submitOrderForm(event) {
    event.preventDefault();

    const tenNguoiDangNhap = localStorage.getItem('userName') || "Quản trị viên";
    const idNguoiDangNhap = localStorage.getItem('userId') || 1;

    if (gioHangTam.length === 0) {
        alert("Giỏ hàng trống! Vui lòng chọn bánh.");
        return;
    }

    const urlParams = new URLSearchParams(window.location.search);
    const editId = urlParams.get('id');
    const isCustomChecked = document.getElementById('checkIsCustomOrder').checked;

    // 🔥 VÁ LỖI KHÔNG LƯU ĐƯỢC DỮ LIỆU MỚI: Bốc trực tiếp giá trị từ Select2 thông qua jQuery
    const khachHangVal = $('#selectKhachHang').val();
    const promoValue = $('#selectKhuyenMai').val();

    const orderData = {
        // Nếu select rỗng (Khách vãng lai) thì gửi null xuống DB, ngược lại ép kiểu Số
        Khach_Hang_ID: khachHangVal ? Number(khachHangVal) : null,
        Nhan_Vien_ID: idNguoiDangNhap ? Number(idNguoiDangNhap) : 1,
        Khuyen_Mai_ID: promoValue ? Number(promoValue) : null,
        TrangThai: document.getElementById('selectTrangThai') ? document.getElementById('selectTrangThai').value : "Chờ xử lý",
        Ten_Nguoi_Nhan: document.getElementById('tenNguoiNhan').value.trim() || `Khách mua tại quầy (${tenNguoiDangNhap})`,
        SDT_Nguoi_Nhan: document.getElementById('sdtNguoiNhan').value.trim() || 'Không có',
        Dia_Chi_Giao: document.getElementById('diaChiGiao').value.trim() || 'Nhận tại tiệm bánh',

        ChiTietGioHang: gioHangTam.map(item => ({
            SanPham_ID: item.SanPham_ID,
            So_Luong: item.So_Luong
        })),

        LaDonCustom: isCustomChecked,
        LoaiYeuCau: isCustomChecked ? document.getElementById('customLoaiYeuCau').value : null,
        KichThuocSoluong: isCustomChecked ? document.getElementById('customKichThuoc').value : null,
        MauSacChuDao: isCustomChecked ? document.getElementById('customMauSac').value : null,
        GhiChu: isCustomChecked ? document.getElementById('customGhiChu').value : null,
        NgayLayHang: isCustomChecked && document.getElementById('customNgayLay').value ? document.getElementById('customNgayLay').value : null,
        HinhAnh: isCustomChecked ? document.getElementById('customHinhAnh').value : null,
        TongTienCustom: isCustomChecked ? Number(document.getElementById('customBaoGia').value) : null,
        TenNhanVienCapNhat: tenNguoiDangNhap,
    };

    const method = editId ? 'PUT' : 'POST';
    const apiUrl = editId
        ? `${API_URL}/api/DonHang/CapNhatThongTinDonHang/${editId}`
        : `${API_URL}/api/DonHang/TaoDonHang`;

    try {
        const response = await fetch(apiUrl, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(orderData)
        });

        if (response.ok) {
            alert(editId ? "Cập nhật đơn hàng tổng hợp thành công!" : "Tạo đơn hàng thành công!");
            window.location.href = 'list-order.html';
        } else {
            const err = await response.json();
            alert(err.message || "Thao tác cập nhật thất bại!");
        }
    } catch (e) { console.error(e); }
}

function clearCustomFields() {
    document.getElementById('customLoaiYeuCau').value = '';
    document.getElementById('customKichThuoc').value = '';
    document.getElementById('customMauSac').value = '';
    document.getElementById('customGhiChu').value = '';
    document.getElementById('customNgayLay').value = '';
    document.getElementById('customHinhAnh').value = '';
}

// CÁC HÀM TẢI DROPDOWN BAN ĐẦU
async function loadKhachHangs() { const select = document.getElementById('selectKhachHang'); try { const res = await fetch(`${API_URL}/api/KhachHang/DanhSachKhachHang`); const data = await res.json(); data.forEach(kh => { select.innerHTML += `<option value="${kh.khachHangId || kh.Id}">${kh.tenKhachHang || kh.TenKhachHang} - ${kh.sdt || kh.Sdt}</option>`; }); } catch (e) { console.error(e); } }
// 🔥 SỬA 3: Hiện kèm mã Sản Phẩm để nhân viên gõ tìm kiếm cực nhanh
async function loadSanPhams() {
    const select = document.getElementById('selectSanPham');
    try {
        const res = await fetch(`${API_URL}/api/SanPham/HienThiDanhSachSanPham`);
        const data = await res.json();
        data.forEach(sp => {
            const id = sp.sanPhamId || sp.Id || Object.values(sp)[0];
            const ten = sp.tenSanPham || sp.TenSanPham;
            // Tạo mã ảo 3 số cho đẹp (VD: SP001, SP012)
            const maSP = `SP${id.toString().padStart(3, '0')}`;

            select.innerHTML += `<option value="${id}">[${maSP}] - ${ten}</option>`;
        });
    } catch (e) { console.error(e); }
}
async function loadKhuyenMais() { const select = document.getElementById('selectKhuyenMai'); try { const res = await fetch(`${API_URL}/api/KhuyenMai/HienThiCacMaKhuyenMai`); const data = await res.json(); data.forEach(km => { select.innerHTML += `<option value="${km.khuyenMaiId || km.Id}">${km.maCode || km.MaCode} (Giảm ${km.phanTramGiam}%)</option>`; }); } catch (e) { console.error(e); } }
function addItemToCart() { const selectSp = document.getElementById('selectSanPham'); const inputSl = document.getElementById('inputSoLuong'); const spId = Number(selectSp.value); const tenSp = selectSp.options[selectSp.selectedIndex].text; const soLuong = Number(inputSl.value); if (!spId || soLuong < 1) { alert("Vui lòng chọn loại bánh và nhập số lượng!"); return; } const trungMon = gioHangTam.find(item => item.SanPham_ID === spId); if (trungMon) { trungMon.So_Luong += soLuong; } else { gioHangTam.push({ SanPham_ID: spId, Ten: tenSp, So_Luong: soLuong }); } renderCartTable(); }
function removeItemFromCart(index) { gioHangTam.splice(index, 1); renderCartTable(); }
function renderCartTable() { const tbody = document.getElementById('cartTableBody'); tbody.innerHTML = ''; if (gioHangTam.length === 0) { tbody.innerHTML = `<tr><td colspan="3" class="text-center text-muted py-2">Chưa có món nào trong giỏ hàng</td></tr>`; return; } gioHangTam.forEach((item, index) => { tbody.innerHTML += `<tr><td><strong>${item.Ten}</strong></td><td class="text-center"><span class="badge bg-label-info">${item.So_Luong}</span></td><td><button type="button" class="btn btn-sm btn-icon btn-outline-danger" onclick="removeItemFromCart(${index})"><i class="bx bx-trash"></i></button></td></tr>`; }); }