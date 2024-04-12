"use strict";

async function swalLoadingModal(title) {
    Swal.fire({
        title,
        didOpen: () => {
            Swal.showLoading();
        },
    });
}

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
        let userId = event.currentTarget.getAttribute('user-id');
        event.preventDefault();
        let currentPassword = document.getElementById('currentPassword').value;
        let newPassword = document.getElementById('newPassword').value;
        let confirmPassword = document.getElementById('confirmPassword').value;
        let reqData = JSON.stringify({
            currentPassword, newPassword, confirmPassword, userId
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

document.getElementById('btn--add--mailbox')
    ?.addEventListener('click', async (event) => {
        let userId = event.currentTarget.getAttribute('user-id');
        let result = await Swal.fire({
            title: 'Add Mailbox',
            html: `
                <input type="text" id="displayName" name="displayName" class="form-control mb-2" placeholder="Display Name">
                <input type="text" id="email" name="email" class="form-control" placeholder="Email">
            `,
            confirmButtonText: 'Create',
            focusConfirm: false,
            didOpen: () => {
            },
            preConfirm: () => {
                const popup = Swal.getPopup();
                const displayNameEl = popup.querySelector('#displayName');
                const emailEl = popup.querySelector('#email');
                const displayName = displayNameEl.value;
                const email = emailEl.value;
                if (!displayName || !email) {
                    Swal.showValidationMessage(`Please enter a display name and an email address.`);
                }
                return { displayName, email, userId }
            },
        });

        const data = result.value;
        let reqData = JSON.stringify(data);

        Swal.fire({
            title: "Creating Mailbox",
            didOpen: () => {
                Swal.showLoading();
            },
        });

        const response = await fetch(`/api/settings/mailboxes/add`, {
            method: 'post',
            headers: {
                "Content-Type": "application/json",
            },
            body: reqData
        });

        Swal.close();

        let success = response.status == 200;

        if (success) {
            await Swal.fire({
                title: "Mailbox created"
            });
            window.location.href = window.location.href;
        } else {
            alert('Mailbox creation failed, try again.');
        }

    });


document.getElementById('btn--add--identity')
    ?.addEventListener('click', async (event) => {
        let userId = event.currentTarget.getAttribute('user-id');
        let result = await Swal.fire({
            title: 'Add Identity',
            html: `
                <input type="text" id="displayName" name="displayName" class="form-control mb-2" placeholder="Display Name">
                <input type="text" id="email" name="email" class="form-control" placeholder="Email">
            `,
            confirmButtonText: 'Create',
            focusConfirm: false,
            didOpen: () => {
            },
            preConfirm: () => {
                const popup = Swal.getPopup();
                const displayNameEl = popup.querySelector('#displayName');
                const emailEl = popup.querySelector('#email');
                const displayName = displayNameEl.value;
                const email = emailEl.value;
                if (!displayName || !email) {
                    Swal.showValidationMessage(`Please enter a display name and an email address.`);
                }
                return { displayName, email, userId }
            },
        });

        const data = result.value;
        let reqData = JSON.stringify(data);

        Swal.fire({
            title: "Creating Identity",
            didOpen: () => {
                Swal.showLoading();
            },
        });

        const response = await fetch(`/api/settings/identities/add`, {
            method: 'post',
            headers: {
                "Content-Type": "application/json",
            },
            body: reqData
        });

        Swal.close();

        let success = response.status == 200;

        if (success) {
            await Swal.fire({
                title: "Identity created"
            });
            window.location.href = window.location.href;
        } else {
            alert('Mailbox creation failed, try again.');
        }

    });


document.querySelectorAll('.btn--remove--identity')
    .forEach(el => {
        el.addEventListener('click', async (event) => {

            const userId = event.currentTarget.getAttribute('user-id');
            const mailboxId = event.currentTarget.getAttribute('identity-id');
            const req = { userId, mailboxId };
            const reqData = JSON.stringify(req);

            swalLoadingModal("Removing Identity");

            const response = await fetch(`/api/settings/administration/remove-user-identity`, {
                method: 'post',
                headers: {
                    "Content-Type": "application/json",
                },
                body: reqData
            });

            Swal.close();

            let success = response.status == 200;

            if (success) {
                await Swal.fire({
                    title: "Identity removed"
                });
                window.location.href = window.location.href;
            } else {
                alert('OTP validation failed, try again.');
            }
        });
    });


document.querySelectorAll('.btn--remove--mailbox')
.forEach(el => {
    el.addEventListener('click', async (event) => {

        const userId = event.currentTarget.getAttribute('user-id');
        const mailboxId = event.currentTarget.getAttribute('mailbox-id');
        const req = { userId, mailboxId };
        const reqData = JSON.stringify(req);

        swalLoadingModal("Removing Mailbox");

        const response = await fetch(`/api/settings/administration/remove-user-mailbox`, {
            method: 'post',
            headers: {
                "Content-Type": "application/json",
            },
            body: reqData
        });

        Swal.close();

        let success = response.status == 200;

        if (success) {
            await Swal.fire({
                title: "Mailboxs removed"
            });
            window.location.href = window.location.href;
        } else {
            alert('OTP validation failed, try again.');
        }
    });
});
