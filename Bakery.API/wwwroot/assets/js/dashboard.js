document.addEventListener('DOMContentLoaded', async () => {
    const txtDoanhThu = document.getElementById('txtTongDoanhThu');
    const txtDonHang = document.getElementById('txtTongDonHang');
    const tableBody = document.getElementById('dashboardOrderTableBody');
    const filterSelect = document.getElementById('filterChartType');

    // Kết nối chính xác tới 6 ô trạng thái đếm trên giao diện HTML
    const lblChoXuLy = document.getElementById('countChoXuLy');
    const lblDaDuyet = document.getElementById('countDaDuyet');
    const lblDangLam = document.getElementById('countDangLam');
    const lblHoanThanh = document.getElementById('countHoanThanh');
    const lblDaHuy = document.getElementById('countDaHuy');
    const lblDaGiao = document.getElementById('countDaGiao');

    if (!tableBody) return;

    let revenueChart = null;
    let validOrders = [];

    function normalizeToYMD(dateStr) {
        if (!dateStr) return '';
        let pureDate = dateStr.split('T')[0].split(' ')[0].trim();
        if (pureDate.includes('-') && pureDate.indexOf('-') === 4) return pureDate;
        if (pureDate.includes('/') && pureDate.indexOf('/') === 4) return pureDate.replace(/\//g, '-');
        let separators = pureDate.includes('/') ? '/' : '-';
        let parts = pureDate.split(separators);
        if (parts.length === 3) {
            if (parts[0].length <= 2 && parts[2].length === 4) {
                return `${parts[2]}-${parts[1].padStart(2, '0')}-${parts[0].padStart(2, '0')}`;
            }
        }
        return pureDate;
    }

    try {
        const response = await fetch(`${API_URL}/api/DonHang/XemToanBoDonHang`);
        if (!response.ok) {
            tableBody.innerHTML = `<tr><td colspan="5" class="text-center text-danger py-3">❌ Lỗi phản hồi từ API Server!</td></tr>`;
            return;
        }

        const orders = await response.json();

        // Khởi tạo các biến đếm cho 6 trạng thái tiến độ
        let tongDoanhThuThucTe = 0;
        let tongSoLuongDonHang = orders.length;

        let vChoXuLy = 0;
        let vDaDuyet = 0;
        let vDangLam = 0;
        let vHoanThanh = 0;
        let vDaHuy = 0;
        let vDaGiao = 0;

        orders.forEach(dh => {
            const trangThai = dh.trang_thai || dh.trangThai || dh.TrangThai || 'Chờ xử lý';
            const soTien = dh.tong_tien || dh.tongTien || dh.TongTien || 0;

            if (trangThai === "Chờ xử lý") {
                vChoXuLy++;
            } else if (trangThai === "Đã duyệt") {
                vDaDuyet++;
            } else if (trangThai === "Đang làm") {
                vDangLam++;
            } else if (trangThai === "Hoàn thành") {
                vHoanThanh++;
            } else if (trangThai === "Đã hủy" || trangThai === "Giao không thành công") {
                vDaHuy++;
            } else if (trangThai === "Đã giao/Đã thanh toán" || trangThai === "Đã giao" || trangThai === "Đã thanh toán") {
                vDaGiao++;
                tongDoanhThuThucTe += soTien;
                validOrders.push(dh);
            }
        });

        if (txtDoanhThu) txtDoanhThu.innerText = new Intl.NumberFormat('vi-VN').format(tongDoanhThuThucTe) + ' đ';
        if (txtDonHang) txtDonHang.innerText = tongSoLuongDonHang + ' đơn';

        if (lblChoXuLy) lblChoXuLy.innerText = vChoXuLy;
        if (lblDaDuyet) lblDaDuyet.innerText = vDaDuyet;
        if (lblDangLam) lblDangLam.innerText = vDangLam;
        if (lblHoanThanh) lblHoanThanh.innerText = vHoanThanh;
        if (lblDaHuy) lblDaHuy.innerText = vDaHuy;
        if (lblDaGiao) lblDaGiao.innerText = vDaGiao;

        // 📈 PHẦN A: VẼ BIỂU ĐỒ TRÒN 6 MẢNH SONG HÀNH BÊN PHẢI (BẢN UPDATE MAX SIZE)
        if (document.querySelector("#statusPieChartContainer")) {
            const pieOptions = {
                series: [vChoXuLy, vDaDuyet, vDangLam, vHoanThanh, vDaHuy, vDaGiao],
                chart: {
                    type: 'pie',
                    height: 300, // Đồng bộ độ cao tuyệt đối với biểu đồ diện tích bên trái
                    fontFamily: 'Public Sans',
                    offsetY: -10 // Đẩy tâm vòng tròn lên trên một chút để nhường không gian cho Legend
                },
                // 🔥 THUẬT TOÁN KÉO BÃN KÍNH: Ép vòng tròn bung to hết cỡ, không bị bóp nghẹt
                plotOptions: {
                    pie: {
                        customScale: 1, // Phóng lớn kích thước chiếc bánh lên 115%
                        dataLabels: {
                            offset: -15 // Đẩy các con số nhãn vào sâu trong tâm mảng bánh một chút để chống tràn chữ
                        }
                    }
                },
                labels: ['Chờ Xử Lý', 'Đã Duyệt', 'Đang Làm', 'Hoàn Thành', 'Đã Hủy', 'Đã Giao'],
                colors: ['#03c3ec', '#696cff', '#ffab00', '#71dd37', '#ff3e1d', '#233446'],
                legend: {
                    position: 'bottom',
                    horizontalAlign: 'center',
                    labels: { colors: '#566a7f' },
                    fontSize: '12px',
                    boxWidth: 10,
                    boxHeight: 10,
                    itemMargin: {
                        horizontal: 8,
                        vertical: 4
                    }
                },
                dataLabels: {
                    enabled: true,
                    style: {
                        fontSize: '13px',
                        fontWeight: 'bold',
                        colors: ['#fff']
                    },
                    dropShadow: { enabled: false }, // Tắt bóng mờ chữ giúp hiển thị text sắc nét rõ ràng
                    formatter: function (val, opts) {
                        // Chỉ hiển thị nhãn chữ số nếu phân khúc đó có đơn thực tế phát sinh (tránh đè số 0 lung tung)
                        const totalOrdersInPiece = opts.w.config.series[opts.seriesIndex];
                        return totalOrdersInPiece > 0 ? totalOrdersInPiece + " đơn" : "";
                    }
                }
            };
            const pieChart = new ApexCharts(document.querySelector("#statusPieChartContainer"), pieOptions);
            pieChart.render();
        }

        // 🕒 PHẦN B: HIỂN THỊ TOÀN BỘ DANH SÁCH BẢNG ĐƠN HÀNG
        const sortedAllOrders = [...orders].sort((a, b) => (b.donHangId || b.DonHangId || 0) - (a.donHangId || a.DonHangId || 0));
        tableBody.innerHTML = '';
        sortedAllOrders.forEach(dh => {
            const id = dh.donHangId || dh.DonHangId;
            const maHienThi = dh.maDhHienThi || `DH${id}`;
            const tenKhach = dh.tenNguoiNhan || dh.TenNguoiNhan || '👤 Khách hàng vãng lai';
            const tienFormat = new Intl.NumberFormat('vi-VN').format(dh.tong_tien || dh.tongTien || dh.TongTien || 0) + ' đ';
            const trangThai = dh.trang_thai || dh.trangThai || dh.TrangThai || 'Chờ xử lý';
            const laCustom = dh.laDonCustom || dh.LaDonCustom || dh.la_don_custom;

            let colorBadgeClass = 'bg-label-secondary';
            if (trangThai === "Đã duyệt") colorBadgeClass = 'bg-label-primary';
            if (trangThai === "Đang làm") colorBadgeClass = 'bg-label-warning';
            if (trangThai === "Hoàn thành") colorBadgeClass = 'bg-label-success';
            if (trangThai === "Đã giao/Đã thanh toán" || trangThai === "Đã giao") colorBadgeClass = 'bg-label-info';
            if (trangThai === "Giao không thành công") colorBadgeClass = 'bg-label-danger';
            if (trangThai === "Đã hủy") colorBadgeClass = 'bg-label-dark';

            const loaiDonBadge = laCustom
                ? `<span class="badge bg-label-warning"><i class="bx bx-star me-1"></i>Bánh Thiết Kế</span>`
                : `<span class="badge bg-label-secondary">Món Sẵn Có</span>`;

            tableBody.innerHTML += `
                <tr>
                    <td><span class="text-primary fw-bold">#${maHienThi}</span></td>
                    <td><strong>${tenKhach}</strong></td>
                    <td>${loaiDonBadge}</td>
                    <td><strong class="text-success">${tienFormat}</strong></td>
                    <td><span class="badge ${colorBadgeClass} fw-bold">${trangThai}</span></td>
                </tr>`;
        });

        // 📈 PHẦN C: THUẬT TOÁN CO GIÃN ĐƯỜNG BIỂU ĐỒ AREA DOANH THU BÊN TRÁI
        function updateChartData() {
            const filterType = filterSelect ? filterSelect.value : 'ngay';
            let categories = []; let dataSeries = []; let labelName = "Doanh thu thực nhận";
            const now = new Date();

            if (filterType === 'ngay') {
                for (let i = 6; i >= 0; i--) {
                    const d = new Date(); d.setDate(now.getDate() - i);
                    categories.push(`${String(d.getDate()).padStart(2, '0')}/${String(d.getMonth() + 1).padStart(2, '0')}`);
                    const targetYMD = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
                    let sumDay = 0;
                    validOrders.forEach(dh => {
                        const keyCoChuNgay = Object.keys(dh).find(k => k.toLowerCase().includes('ngay') || k.toLowerCase().includes('date'));
                        if (normalizeToYMD(dh[keyCoChuNgay]) === targetYMD) sumDay += (dh.tong_tien || dh.tongTien || dh.TongTien || 0);
                    });
                    dataSeries.push(sumDay);
                }
            } else if (filterType === 'thang') {
                for (let m = 1; m <= 12; m++) {
                    categories.push(`Tháng ${m}`); let sumMonth = 0;
                    validOrders.forEach(dh => {
                        const keyCoChuNgay = Object.keys(dh).find(k => k.toLowerCase().includes('ngay') || k.toLowerCase().includes('date'));
                        const oYMD = normalizeToYMD(dh[keyCoChuNgay]);
                        if (oYMD && parseInt(oYMD.split('-')[1]) === m && parseInt(oYMD.split('-')[0]) === now.getFullYear()) sumMonth += (dh.tong_tien || dh.tongTien || dh.TongTien || 0);
                    });
                    dataSeries.push(sumMonth);
                }
            } else if (filterType === 'nam') {
                let yearsSet = new Set([now.getFullYear()]);
                validOrders.forEach(dh => { const keyCoChuNgay = Object.keys(dh).find(k => k.toLowerCase().includes('ngay') || k.toLowerCase().includes('date')); const oYMD = normalizeToYMD(dh[keyCoChuNgay]); if (oYMD) yearsSet.add(parseInt(oYMD.split('-')[0])); });
                categories = Array.from(yearsSet).sort((a, b) => a - b);
                categories.forEach(y => {
                    let sumYear = 0;
                    validOrders.forEach(dh => { const keyCoChuNgay = Object.keys(dh).find(k => k.toLowerCase().includes('ngay') || k.toLowerCase().includes('date')); if (parseInt(normalizeToYMD(dh[keyCoChuNgay]).split('-')[0]) === y) sumYear += (dh.tong_tien || dh.tongTien || dh.TongTien || 0); });
                    dataSeries.push(sumYear);
                });
            }

            const chartOptions = {
                series: [{ name: labelName, data: dataSeries }],
                chart: { type: 'area', height: 300, parentHeightOffset: 0, toolbar: { show: false }, fontFamily: 'Public Sans' },
                dataLabels: { enabled: false }, stroke: { curve: 'smooth', width: 3 }, colors: ['#696cff'],
                fill: { type: 'gradient', gradient: { shadeIntensity: 1, opacityFrom: 0.5, opacityTo: 0.1, stops: [0, 90, 100] } },
                xaxis: { categories: categories },
                yaxis: { labels: { formatter: val => new Intl.NumberFormat('vi-VN').format(val) + ' đ' } },
                tooltip: { y: { formatter: val => new Intl.NumberFormat('vi-VN').format(val) + ' đ' } },
                grid: { borderColor: '#f1f1f1', padding: { top: -20, bottom: -10 } }
            };
            if (revenueChart) { revenueChart.updateOptions(chartOptions); }
            else { revenueChart = new ApexCharts(document.querySelector("#revenueChartContainer"), chartOptions); revenueChart.render(); }
        }

        if (document.querySelector("#revenueChartContainer")) {
            updateChartData();
            if (filterSelect) filterSelect.addEventListener('change', updateChartData);
        }

    } catch (err) {
        console.error("Lỗi đồng bộ Dashboard:", err);
    }
});