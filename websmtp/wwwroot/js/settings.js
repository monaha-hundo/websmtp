"use strict";

document.getElementById('otp--form')
    ?.addEventListener('submit', async (event) => {
        event.preventDefault();
        let res = await fetch('/api/settings/otp/initiate');
        let blob = await res.blob();
        let dataUrl = URL.createObjectURL(blob)
        let img = document.createElement('img');
        img.setAttribute('src', dataUrl);
        document.getElementById('qrcode--holder').appendChild(img);
        document.getElementById('otp--qrcode').classList.remove('d-none');
        document.getElementById('otp--form').classList.add('d-none');
    });

document.getElementById('otp--qrcode')
    ?.addEventListener('submit', async (event) => {
        event.preventDefault();
        let otpEl = document.getElementById('otpValidation');
        let otp = otpEl.value;

        const response = await fetch(`/api/settings/otp/validate`, {
            method: 'post',
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify({ "otp": otp })
        });

        let success = response.status == 200;

        if (success) {
            document.getElementById('otp--qrcode').classList.add('d-none');
            document.getElementById('otp--qrcode--success').classList.remove('d-none');
        } else {
            alert('OTP validation failed, try again.');
        }

    });

document.getElementById('btn--change--pwd')
    ?.addEventListener('click', async (event) => {
        event.preventDefault();
        let currentPassword = document.getElementById('currentPassword').value;
        let newPassword = document.getElementById('newPassword').value;
        let confirmPassword = document.getElementById('confirmPassword').value;
        let reqData = JSON.stringify({
            currentPassword, newPassword, confirmPassword
        });

        Swal.fire({
            title: "Changing password",
            didOpen: () => {
                Swal.showLoading();
            },
        });

        const response = await fetch(`/api/settings/pwd/change`, {
            method: 'post',
            headers: {
                "Content-Type": "application/json",
            },
            body: reqData
        });

        Swal.close();

        let success = response.status == 200;

        if (success) {
            document.getElementById('pwd--change--form').classList.add('d-none');
            document.getElementById('pwd--change--success').classList.remove('d-none');
        } else {
            alert('OTP validation failed, try again.');
        }
    });