
// 1. HÀM ĐỔ DỮ LIỆU RA BẢNG KHO SẢN PHẨM
async function loadProducts() {
    const tableBody = document.getElementById('productTableBody');
    if (!tableBody) return;

    try {
        const response = await fetch(`${API_URL}/api/SanPham/HienThiDanhSachSanPham`);
        const data = await response.json();

        tableBody.innerHTML = '';
        data.forEach(sp => {
            const tenCotId = Object.keys(sp).find(key => key.toLowerCase().includes('id'));
            const idChuẩn = String(tenCotId ? sp[tenCotId] : '');

            // ĐỒNG BỘ TUYỆT ĐỐI RADAR CHỮ "SẢN PHẨM"
            const tenSP = sp.tenSanPham || sp.TenSanPham || sp.ten_San_Pham || sp.tenBanh || 'Lỗi tên SP';
            const giaSP = sp.donGiaBan || sp.DonGiaBan || sp.giaBan || sp.donGia || sp.GiaBan || 0;
            const soLuongSP = sp.soLuongTon || sp.SoLuongTon || sp.soLuong || 0;
            const loaiSP = sp.phanLoai || sp.PhanLoai || 'Chưa phân loại';
            const hinhSP = sp.hinhAnh || sp.HinhAnh || '';

            const giaFormat = new Intl.NumberFormat('vi-VN').format(giaSP);
            const anhHienThi = hinhSP ? `<img src="${hinhSP}" alt="SP" class="rounded" style="width: 50px; height: 50px; object-fit: cover;">` : `<span class="badge bg-label-secondary">No Image</span>`;

            tableBody.innerHTML += `               
                <tr>
                    <td><strong>SP${idChuẩn}</strong></td>
                    <td>${anhHienThi}</td>
                    <td><strong>${tenSP}</strong></td>
                    <td><span class="badge bg-label-info">${loaiSP}</span></td>
                    <td>${giaFormat} đ</td>
                    <td><span class="badge bg-label-success">${soLuongSP}</span></td>
                    <td>
                        <button class="btn btn-sm btn-icon btn-outline-warning" onclick="editProduct(${idChuẩn})">
                            <i class="bx bx-edit"></i>
                        </button>
                        <button class="btn btn-sm btn-icon btn-outline-danger" onclick="deleteProduct(${idChuẩn})">
                            <i class="bx bx-trash"></i>
                        </button>
                    </td>
                </tr>`;
        });
    } catch (error) { console.error("Lỗi load bảng Sản phẩm:", error); }
}

// 2. HÀM THÊM / SỬA SẢN PHẨM
async function handleAddProduct(event) {
    event.preventDefault();

    const urlParams = new URLSearchParams(window.location.search);
    const editId = urlParams.get('id');

    // ĐỒNG BỘ ID HTML THÀNH tenSanPham KHỚP DTO BACKEND
    const productData = {
        TenSanPham: document.getElementById('tenSanPham').value,
        GiaBan: Number(document.getElementById('giaBan').value),
        SoLuong: document.getElementById('soLuong') ? Number(document.getElementById('soLuong').value) : 0,
        PhanLoai: document.getElementById('phanLoai').value,
        MoTa: document.getElementById('moTa') ? document.getElementById('moTa').value : '',
        HinhAnh: document.getElementById('linkHinhAnh') ? document.getElementById('linkHinhAnh').value : ''    };

    const method = editId ? 'PUT' : 'POST';
    const apiUrl = editId
        ? `${API_URL}/api/SanPham/CapNhatSanPham/${editId}`
        : `${API_URL}/api/SanPham/ThemSanPhamMoi`;

    try {
        const response = await fetch(apiUrl, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(productData)
        });

        if (response.ok) {
            alert(editId ? "Cập nhật sản phẩm thành công!" : "Thêm sản phẩm mới thành công!");
            window.location.href = 'list-product.html';
        } else {
            alert("Thất bại! Vui lòng kiểm tra lại dữ liệu.");
        }
    } catch (error) { console.error("Lỗi:", error); }
}

// 3. HÀM XÓA
async function deleteProduct(id) {
    if (confirm("Chắc chắn muốn xóa sản phẩm này khỏi hệ thống?")) {
        try {
            const response = await fetch(`${API_URL}/api/SanPham/XoaSanPham/${id}`, { method: 'DELETE' });
            if (response.ok) {
                alert("Xóa thành công!");
                loadProducts();
            } else {
                alert("Không thể xóa sản phẩm này vì lý do ràng buộc dữ liệu!");
            }
        } catch (error) { console.error(error); }
    }
}

// 4. CHUYỂN TRANG SỬA
function editProduct(id) {
    window.location.href = `add-product.html?id=${id}`;
}

// 5. GỘP SỰ KIỆN KHI TRANG LOAD
document.addEventListener('DOMContentLoaded', async () => {
    if (document.getElementById('productTableBody')) {
        loadProducts();
    }

    const formProduct = document.getElementById('formAddProduct');
    if (formProduct) formProduct.addEventListener('submit', handleAddProduct);

    const urlParams = new URLSearchParams(window.location.search);
    const editId = urlParams.get('id');
    const formTitle = document.querySelector('.card-header h5');
    const submitBtn = document.querySelector('button[type="submit"]');

    if (editId) {
        if (formTitle) formTitle.innerText = "Cập Nhật Thông Tin Sản Phẩm";
        if (submitBtn) {
            submitBtn.innerText = "Lưu Cập Nhật";
            submitBtn.classList.replace('btn-primary', 'btn-warning');
        }

        try {
            const response = await fetch(`${API_URL}/api/SanPham/LaySanPham/${editId}`);
            if (response.ok) {
                let data = await response.json();
                if (Array.isArray(data)) { data = data[0]; }

                if (data) {
                    document.getElementById('tenSanPham').value = data.tenSanPham || data.TenSanPham || data.ten_San_Pham || '';
                    document.getElementById('giaBan').value = data.donGiaBan || data.DonGiaBan || data.giaBan || 0;
                    document.getElementById('phanLoai').value = data.phanLoai || data.PhanLoai || '';

                    if (document.getElementById('soLuong')) {
                        document.getElementById('soLuong').value = data.soLuongTon || data.SoLuongTon || 0;
                    }
                    if (document.getElementById('moTa')) document.getElementById('moTa').value = data.moTa || data.MoTa || '';
                    if (document.getElementById('linkHinhAnh')) document.getElementById('linkHinhAnh').value = data.hinhAnh || data.HinhAnh || '';                    if (data.hinhAnh) {
                        const previewContainer = document.getElementById('previewContainerSP');
                        const previewImg = document.getElementById('previewImgSP');
                        if (previewContainer && previewImg) {
                            previewImg.src = data.hinhAnh;
                            previewContainer.style.display = "block";
                        }
                    }
                }
            }
        } catch (error) { console.error("Lỗi lấy chi tiết sản phẩm:", error); }
    } else if (formProduct) {
        if (formTitle) formTitle.innerText = "Thêm Sản Phẩm Mới";
        if (submitBtn) { submitBtn.innerText = "Lưu Sản Phẩm"; }
    }
});

// 🔥 HÀM UPLOAD ẢNH SẢN PHẨM (Xài ké API của Đơn Hàng)
async function uploadAnhSanPham() {
    const fileInput = document.getElementById('fileAnhSanPham');
    const statusText = document.getElementById('uploadStatusSP');
    const hiddenInput = document.getElementById('linkHinhAnh');

    if (fileInput.files.length === 0) return;

    const formData = new FormData();
    formData.append('file', fileInput.files[0]);

    statusText.innerText = "⏳ Đang tải ảnh lên hệ thống...";
    statusText.className = "text-warning d-block mt-1";

    try {
        // Tái sử dụng API cũ cho an toàn và nhanh gọn
        const response = await fetch(`https://localhost:7122/api/DonHang/UploadAnhMauCustom`, {
            method: 'POST',
            body: formData
        });

        if (response.ok) {
            const result = await response.json();
            const linkAnhReal = result.linkAnh !== undefined ? result.linkAnh : result.LinkAnh;

            hiddenInput.value = linkAnhReal; // Nhét link vào ô ẩn

            statusText.innerText = "✅ Tải ảnh thành công!";
            statusText.className = "text-success d-block mt-1";

            // Ép ảnh hiện lên khung xem trước
            const previewContainer = document.getElementById('previewContainerSP');
            const previewImg = document.getElementById('previewImgSP');
            if (previewContainer && previewImg && linkAnhReal) {
                previewImg.src = linkAnhReal;
                previewContainer.style.display = "block";
            }
        } else {
            statusText.innerText = "❌ Tải ảnh thất bại!";
            statusText.className = "text-danger d-block mt-1";
        }
    } catch (error) {
        console.error("Lỗi upload:", error);
        statusText.innerText = "❌ Lỗi kết nối server!";
    }
}