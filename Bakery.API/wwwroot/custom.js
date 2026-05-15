(function () {
    const form = document.getElementById('customCakeForm');
    if (!form) return;

    const priceEl = document.getElementById('customEstimatedPrice');
    const submitBtn = document.getElementById('submitCustomOrderBtn');
    const statusEl = document.getElementById('customOrderStatus');
    const resultIdEl = document.getElementById('customOrderId');
    const resultMessageEl = document.getElementById('customOrderMessage');
    const resultModalEl = document.getElementById('customOrderResultModal');

    const fields = {
        name: document.getElementById('customName'),
        phone: document.getElementById('customPhone'),
        address: document.getElementById('customAddress'),
        size: document.getElementById('customSize'),
        color: document.getElementById('customColor'),
        pickupDate: document.getElementById('customPickupDate'),
        description: document.getElementById('customDescription')
    };

    const today = new Date();
    today.setDate(today.getDate() + 1);
    fields.pickupDate.min = today.toISOString().slice(0, 10);

    function estimatePrice() {
        let price = 250000;
        const size = fields.size.value.toLowerCase();
        const description = fields.description.value.toLowerCase();

        if (size.includes('20cm') || size.includes('lớn')) price += 100000;
        if (size.includes('16cm') || size.includes('vừa')) price += 50000;
        if (description.includes('2 tầng') || description.includes('vẽ hình') || description.includes('phức tạp')) price += 200000;
        if (description.includes('ghi chữ') || description.includes('hoa') || description.includes('đơn giản')) price += 50000;

        return price;
    }

    function renderPrice() {
        priceEl.textContent = `${estimatePrice().toLocaleString('vi-VN')} VNĐ`;
    }

    function getReportParams() {
        return new URLSearchParams(window.location.search);
    }

    function fillReportCustomForm() {
        const params = getReportParams();
        if (params.get('reportCustom') !== '1') return;

        const pickupDate = new Date();
        pickupDate.setDate(pickupDate.getDate() + 3);

        fields.name.value = 'Nguyễn Minh Anh';
        fields.phone.value = '0912345678';
        fields.address.value = '12 Nguyễn Trãi, Quận 1, TP. Hồ Chí Minh';
        fields.size.value = 'Lớn 20cm';
        fields.color.value = 'Trắng kem và hồng pastel';
        fields.pickupDate.value = pickupDate.toISOString().slice(0, 10);
        fields.description.value = 'Bánh sinh nhật 2 tầng, trang trí hoa kem, ghi chữ Chúc Mừng Sinh Nhật.';
        renderPrice();
    }

    function showReportCustomSuccess() {
        const params = getReportParams();
        const orderId = params.get('reportCustomSuccess');
        if (!orderId || !resultModalEl) return;

        resultIdEl.textContent = `#${orderId}`;
        resultMessageEl.textContent = `Đặt thành công mã đơn #${orderId}.`;

        setTimeout(function () {
            const modal = bootstrap.Modal.getInstance(resultModalEl) || new bootstrap.Modal(resultModalEl);
            modal.show();
        }, 800);
    }

    function setStatus(message, isError) {
        statusEl.textContent = message;
        statusEl.className = isError ? 'text-danger small mt-3' : 'text-muted small mt-3';
    }

    async function readApiResponse(response) {
        const text = await response.text();
        if (!text) return null;

        try {
            return JSON.parse(text);
        } catch {
            return { message: text };
        }
    }

    function getApiMessage(result) {
        return result?.message || result?.Message || result?.title || '';
    }

    function getFetchErrorMessage(error) {
        if (error instanceof TypeError && error.message === 'Failed to fetch') {
            return 'Không kết nối được API. Hãy chạy project bằng http://localhost:5094 rồi thử lại.';
        }

        return error.message || 'Không thể gửi yêu cầu đặt bánh custom.';
    }

    Object.values(fields).forEach((field) => {
        field.addEventListener('input', renderPrice);
        field.addEventListener('change', renderPrice);
    });

    form.addEventListener('submit', async function (event) {
        event.preventDefault();
        form.classList.add('was-validated');

        if (!fields.size.value) {
            setStatus('Vui lòng chọn kích thước bánh trước khi đặt.', true);
            fields.size.focus();
            return;
        }

        if (!form.checkValidity()) {
            setStatus('Vui lòng kiểm tra lại các thông tin bắt buộc.', true);
            return;
        }

        const payload = {
            Khach_Hang_ID: null,
            Ten_Nguoi_Nhan: fields.name.value.trim(),
            SDT_Nguoi_Nhan: fields.phone.value.trim(),
            Dia_Chi_Giao: fields.address.value.trim(),
            Mo_Ta_Yeu_Cau: fields.description.value.trim(),
            Kich_Thuoc: fields.size.value,
            Mau_Sac: fields.color.value.trim(),
            Ngay_Lay_Banh: fields.pickupDate.value,
            Gia_Du_Kien: estimatePrice()
        };

        submitBtn.disabled = true;
        setStatus('Đang gửi yêu cầu đặt bánh custom...', false);

        try {
            const response = await fetch('/api/DonBanhCustom/TaoDonHangDatBanhCustom', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            const result = await readApiResponse(response);
            if (!response.ok || result.success === false) {
                throw new Error(getApiMessage(result) || 'Không thể gửi yêu cầu đặt bánh custom.');
            }

            const orderId = result.donHangID || result.donHangId || result.DonHangID;
            const successMessage = orderId
                ? `Đặt thành công mã đơn #${orderId}.`
                : (getApiMessage(result) || 'Đặt bánh custom thành công.');

            resultIdEl.textContent = orderId ? `#${orderId}` : 'Đang cập nhật';
            resultMessageEl.textContent = successMessage;
            const modal = bootstrap.Modal.getInstance(resultModalEl) || new bootstrap.Modal(resultModalEl);
            modal.show();

            form.reset();
            form.classList.remove('was-validated');
            renderPrice();
            setStatus(successMessage, false);
        } catch (error) {
            setStatus(getFetchErrorMessage(error), true);
        } finally {
            submitBtn.disabled = false;
        }
    });

    fillReportCustomForm();
    showReportCustomSuccess();
    renderPrice();
})();
