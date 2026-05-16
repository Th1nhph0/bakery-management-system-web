const API_URL = "https://localhost:7122";

// 1. HÀM ĐỔ DANH SÁCH KHÁCH HÀNG RA BẢNG
async function loadCustomers() {
    const tableBody = document.getElementById('customerTableBody');
    if (!tableBody) return;

    try {
        const response = await fetch(`${API_URL}/api/KhachHang/DanhSachKhachHang`);
        const data = await response.json();

        tableBody.innerHTML = '';
        data.forEach(kh => {
            const tenCotId = Object.keys(kh).find(key => key.toLowerCase().includes('id'));
            const idChuẩn = String(tenCotId ? kh[tenCotId] : '');

            const tenKH = kh.tenKhachHang || kh.TenKhachHang || 'Chưa có tên';
            const sdtKH = kh.sdt || kh.Sdt || 'Chưa có SDT'; // Đồng bộ trường Sdt
            const emailKH = kh.email || kh.Email || 'Không có';
            const diaChiKH = kh.diaChi || kh.DiaChi || 'Chưa cập nhật';

            tableBody.innerHTML += `               
                <tr>
                    <td><strong>KH${idChuẩn}</strong></td>
                    <td><strong>${tenKH}</strong></td>
                    <td><span class="badge bg-label-primary">${sdtKH}</span></td>
                    <td>${emailKH}</td>
                    <td>${diaChiKH}</td>
                    <td>
                        <button class="btn btn-sm btn-icon btn-outline-info me-1" title="Lịch sử mua hàng" onclick="viewHistory(${idChuẩn})">
                            <i class="bx bx-history"></i>
                        </button>
                        <button class="btn btn-sm btn-icon btn-outline-warning me-1" title="Sửa thông tin" onclick="editCustomer(${idChuẩn})">
                            <i class="bx bx-edit"></i>
                        </button>
                        <button class="btn btn-sm btn-icon btn-outline-danger" title="Xóa" onclick="deleteCustomer(${idChuẩn})">
                            <i class="bx bx-trash"></i>
                        </button>
                    </td>
                </tr>`;
        });
    } catch (error) { console.error("Lỗi tải danh sách khách hàng:", error); }
}

// 2. HÀM XỬ LÝ SUBMIT FORM (THÊM HOẶC SỬA)
async function handleAddCustomer(event) {
    event.preventDefault();

    const urlParams = new URLSearchParams(window.location.search);
    const editId = urlParams.get('id');

    // ĐÓNG GÓI ĐÚNG TRƯỜNG 'Sdt' ĐỂ BACKEND KHÔNG BÁO LỖI 400
    const customerData = {
        TenKhachHang: document.getElementById('tenKhachHang').value,
        Sdt: document.getElementById('soDienThoai').value,
        Email: document.getElementById('emailKH').value,
        DiaChi: document.getElementById('diaChiKH').value
    };

    const method = editId ? 'PUT' : 'POST';
    const apiUrl = editId
        ? `${API_URL}/api/KhachHang/CapNhatThongTinKhachHang/${editId}`
        : `${API_URL}/api/KhachHang/TaoKhachHangMoi`;

    try {
        const response = await fetch(apiUrl, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(customerData)
        });

        if (response.ok) {
            alert(editId ? "Cập nhật thông tin khách hàng thành công!" : "Thêm khách hàng thành công!");
            window.location.href = 'list-customer.html';
        } else {
            alert("Thất bại! Vui lòng kiểm tra lại thông tin đầu vào.");
        }
    } catch (error) { console.error("Lỗi lưu khách hàng:", error); }
}

// 3. HÀM XÓA KHÁCH HÀNG
async function deleteCustomer(id) {
    if (confirm("Bạn có chắc chắn muốn xóa khách hàng này khỏi hệ thống?")) {
        try {
            const response = await fetch(`${API_URL}/api/KhachHang/XoaKhachHang/${id}`, { method: 'DELETE' });
            if (response.ok) {
                alert("Xóa thành công!");
                loadCustomers();
            } else {
                const err = await response.json();
                alert(err.message || "Không thể xóa khách hàng này!");
            }
        } catch (error) { console.error(error); }
    }
}

// 4. ĐIỀU HƯỚNG TRANG
function editCustomer(id) { window.location.href = `add-customer.html?id=${id}`; }
function viewHistory(id) { window.location.href = `purchase-history.html?id=${id}`; }

// 5. TỰ ĐỘNG ĐỔ DỮ LIỆU CŨ VÀO FORM KHI ĐANG Ở TRANG SỬA
document.addEventListener('DOMContentLoaded', async () => {
    if (document.getElementById('customerTableBody')) loadCustomers();

    const formCustomer = document.getElementById('formAddCustomer');
    if (formCustomer) formCustomer.addEventListener('submit', handleAddCustomer);

    const urlParams = new URLSearchParams(window.location.search);
    const editId = urlParams.get('id');
    const formTitle = document.querySelector('.card-header h5');
    const submitBtn = document.querySelector('button[type="submit"]');

    if (editId) {
        if (formTitle) formTitle.innerText = "Cập Nhật Thông Tin Khách Hàng";
        if (submitBtn) {
            submitBtn.innerText = "Lưu Cập Nhật";
            submitBtn.classList.replace('btn-primary', 'btn-warning');
        }

        try {
            const response = await fetch(`${API_URL}/api/KhachHang/LayKhachHang/${editId}`);
            if (response.ok) {
                const data = await response.json();
                document.getElementById('tenKhachHang').value = data.tenKhachHang || data.TenKhachHang || '';
                document.getElementById('soDienThoai').value = data.sdt || data.Sdt || '';
                document.getElementById('emailKH').value = data.email || data.Email || '';
                document.getElementById('diaChiKH').value = data.diaChi || data.DiaChi || '';
            }
        } catch (error) { console.error("Lỗi lấy chi tiết khách hàng:", error); }
    }
});