

// 1. ĐỔ DỮ LIỆU RA BẢNG KHO KHUYẾN MÃI
async function loadPromotions() {
    const tableBody = document.getElementById('promotionTableBody');
    if (!tableBody) return;

    try {
        // Gọi chính xác hàm HienThiCacMaKhuyenMai của ông
        const response = await fetch(`${API_URL}/api/KhuyenMai/HienThiCacMaKhuyenMai`);
        const data = await response.json();

        tableBody.innerHTML = '';
        data.forEach(km => {
            const tenCotId = Object.keys(km).find(key => key.toLowerCase().includes('id'));
            const idChuẩn = String(tenCotId ? km[tenCotId] : '');

            const code = km.maCode || km.MaCode || 'UNKNOWN';
            const phanTram = km.phanTramGiam || km.PhanTramGiam || 0;
            const batDau = new Date(km.ngayBatDau || km.NgayBatDau).toLocaleDateString('vi-VN');
            const ketThuc = new Date(km.ngayKetThuc || km.NgayKetThuc).toLocaleDateString('vi-VN');

            const role = localStorage.getItem('userRole') || '';
            let actionButtonsHtml = '';

            if (role === 'Admin' || role === 'Chủ quán' || role === 'Quản trị web') {
                actionButtonsHtml = `
            <button class="btn btn-sm btn-icon btn-outline-warning" onclick="editPromotion(${idChuẩn})"><i class="bx bx-edit-alt"></i></button>
            <button class="btn btn-sm btn-icon btn-outline-danger ms-1" onclick="deletePromotion(${idChuẩn})"><i class="bx bx-trash"></i></button>
        `;
            } else {
                actionButtonsHtml = `<span class="badge bg-label-secondary">Không có quyền</span>`;
            }
            tableBody.innerHTML += `               
                <tr>
                    <td><strong>KM${idChuẩn}</strong></td>
                    <td><span class="badge bg-label-success fw-bold">${code}</span></td>
                    <td><strong>Giảm ${phanTram}%</strong></td>
                    <td>${batDau}</td>
                    <td>${ketThuc}</td>
                    <td>
                        <button class="btn btn-sm btn-icon btn-outline-warning me-1" onclick="editPromotion(${idChuẩn})">
                            <i class="bx bx-edit"></i>
                        </button>
                        <button class="btn btn-sm btn-icon btn-outline-danger" onclick="deletePromotion(${idChuẩn})">
                            <i class="bx bx-trash"></i>
                        </button>
                    </td>
                </tr>`;
        });
    } catch (error) { console.error("Lỗi tải danh sách khuyến mãi:", error); }
}

// 2. XỬ LÝ SUBMIT FORM (TẠO MỚI HOẶC CẬP NHẬT)
async function handleAddPromotion(event) {
    event.preventDefault();

    const urlParams = new URLSearchParams(window.location.search);
    const editId = urlParams.get('id');

    const promotionData = {
        MaCode: document.getElementById('maCode').value.toUpperCase(),
        PhanTramGiam: Number(document.getElementById('phanTramGiam').value),
        NgayBatDau: document.getElementById('ngayBatDau').value,
        NgayKetThuc: document.getElementById('ngayKetThuc').value
    };

    const method = editId ? 'PUT' : 'POST';
    const apiUrl = editId
        ? `${API_URL}/api/KhuyenMai/CapNhatKhuyenMai/${editId}`
        : `${API_URL}/api/KhuyenMai/TaoKhuyenMaiMoi`;

    try {
        const response = await fetch(apiUrl, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(promotionData)
        });

        if (response.ok) {
            alert(editId ? "Cập nhật mã giảm giá thành công!" : "Tạo mã giảm giá thành công!");
            window.location.href = 'list-khuyenMai.html';
        } else {
            alert("Thất bại! Vui lòng kiểm tra lại thông tin đầu vào.");
        }
    } catch (error) { console.error("Lỗi lưu khuyến mãi:", error); }
}

// 3. XỬ LÝ XÓA MÃ KHUYẾN MÃI
async function deletePromotion(id) {
    if (confirm("Bạn có chắc chắn muốn xóa mã giảm giá này không?")) {
        try {
            const response = await fetch(`${API_URL}/api/KhuyenMai/XoaKhuyenMai/${id}`, { method: 'DELETE' });
            if (response.ok) {
                alert("Xóa thành công!");
                loadPromotions();
            } else {
                const err = await response.json();
                alert(err.message || "Không thể xóa mã này!");
            }
        } catch (error) { console.error(error); }
    }
}

function editPromotion(id) { window.location.href = `add-khuyenMai.html?id=${id}`; }

// 4. TỰ ĐỔ DỮ LIỆU CŨ VÀO FORM KHI SỬA
document.addEventListener('DOMContentLoaded', async () => {
    if (document.getElementById('promotionTableBody')) loadPromotions();

    const formPromo = document.getElementById('formAddPromotion');
    if (formPromo) formPromo.addEventListener('submit', handleAddPromotion);

    const urlParams = new URLSearchParams(window.location.search);
    const editId = urlParams.get('id');
    const formTitle = document.querySelector('.card-header h5');
    const submitBtn = document.querySelector('button[type="submit"]');

    if (editId) {
        if (formTitle) formTitle.innerText = "Cập Nhật Chương Trình Khuyến Mãi";
        if (submitBtn) {
            submitBtn.innerText = "Lưu Cập Nhật";
            submitBtn.classList.replace('btn-primary', 'btn-warning');
        }

        try {
            // Gọi hàm LayKhuyenMai vừa bổ sung ở C#
            const response = await fetch(`${API_URL}/api/KhuyenMai/LayKhuyenMai/${editId}`);
            if (response.ok) {
                const data = await response.json();
                document.getElementById('maCode').value = data.maCode || data.MaCode || '';
                document.getElementById('phanTramGiam').value = data.phanTramGiam || data.PhanTramGiam || 0;

                if (data.ngayBatDau || data.NgayBatDau) document.getElementById('ngayBatDau').value = (data.ngayBatDau || data.NgayBatDau).split('T')[0];
                if (data.ngayKetThuc || data.NgayKetThuc) document.getElementById('ngayKetThuc').value = (data.ngayKetThuc || data.NgayKetThuc).split('T')[0];
            }
        } catch (error) { console.error("Lỗi lấy dữ liệu sửa:", error); }
    }
});