async function swalLoadingModal(title) {
    Swal.fire({
        title,
        didOpen: () => {
            Swal.showLoading();
        },
    });
}

document.querySelectorAll('.btn--add--mailbox')
    .forEach(el => {
        el.addEventListener('click', async (event) => {

            const userId = event.currentTarget.getAttribute('user-id');

            let result = await Swal.fire({
                title: 'Add Mailbox',
                html: `
                    <input type="text" id="displayName" name="displayName" class="form-control mb-2" placeholder="Display Name">
                    <input type="text" id="email" name="email" class="form-control" placeholder="Email">
                `,
                confirmButtonText: 'Add',
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
                    return { userId, displayName, email };
                },
            });

            const data = result.value;
            let reqData = JSON.stringify(data);

            swalLoadingModal("Adding Mailbox");

            const response = await fetch(`/api/settings/administration/add-user-mailbox`, {
                method: 'post',
                headers: {
                    "Content-Type": "application/json",
                },
                body: reqData
            });

            Swal.close();

            let success = response.status == 200;

            if (success) {
                window.location.href = window.location.href;
            } else {
                alert('Could not add user mailbox.');
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
                let mailboxEl = document.querySelector(`div[mailbox-id="${mailboxId}"]`);
                mailboxEl.parentElement.removeChild(mailboxEl);
            } else {
                alert('OTP validation failed, try again.');
            }
        });
    });


document.querySelectorAll('.btn--add--identity')
    .forEach(el => {
        el.addEventListener('click', async (event) => {

            const userId = event.currentTarget.getAttribute('user-id');

            let result = await Swal.fire({
                title: 'Add Identity',
                html: `
                    <input type="text" id="displayName" name="displayName" class="form-control mb-2" placeholder="Display Name">
                    <input type="text" id="email" name="email" class="form-control" placeholder="Email">
                `,
                confirmButtonText: 'Add',
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
                    return { userId, displayName, email };
                },
            });

            const data = result.value;
            let reqData = JSON.stringify(data);

            swalLoadingModal("Adding Mailbox");

            const response = await fetch(`/api/settings/administration/add-user-identity`, {
                method: 'post',
                headers: {
                    "Content-Type": "application/json",
                },
                body: reqData
            });

            Swal.close();

            let success = response.status == 200;

            if (success) {
                window.location.href = window.location.href;
            } else {
                alert('Could not add user identity.');
            }
        });
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
                let mailboxEl = document.querySelector(`div[identity-id="${mailboxId}"]`);
                mailboxEl.parentElement.removeChild(mailboxEl);
            } else {
                alert('OTP validation failed, try again.');
            }
        });
    });


document.querySelectorAll('.btn--change--password')
    .forEach(el => {
        el.addEventListener('click', async (event) => {

            const userId = event.currentTarget.getAttribute('user-id');

            let result = await Swal.fire({
                title: 'Change Password',
                html: `
                <input type="text" id="newPassword" name="newPassword" class="form-control mb-2" placeholder="New Password">
                <input type="text" id="confirmPassword" name="confirmPassword" class="form-control" placeholder="Confirm new password">
            `,
                confirmButtonText: 'Set',
                focusConfirm: false,
                didOpen: () => {
                },
                preConfirm: () => {
                    const popup = Swal.getPopup();
                    const newPasswordEl = popup.querySelector('#newPassword');
                    const confirmPasswordEl = popup.querySelector('#confirmPassword');
                    const newPassword = newPasswordEl.value;
                    const confirmPassword = confirmPasswordEl.value;
                    if (!newPassword || !confirmPassword) {
                        Swal.showValidationMessage(`Please enter a new password and confirm it.`);
                    }
                    return { userId, newPassword, confirmPassword };
                },
            });

            const data = result.value;
            let reqData = JSON.stringify(data);

            swalLoadingModal("Changing password");

            const response = await fetch(`/api/settings/administration/change-user-password`, {
                method: 'post',
                headers: {
                    "Content-Type": "application/json",
                },
                body: reqData
            });

            Swal.close();

            let success = response.status == 200;

            if (success) {
                alert('Password changed');
            } else {
                alert('Could not add user identity.');
            }
        });
    });

document.querySelectorAll('.btn--change--username')
    .forEach(el => {
        el.addEventListener('click', async (event) => {

            const userId = event.currentTarget.getAttribute('user-id');

            let result = await Swal.fire({
                title: 'Change Username',
                html: `
                <input type="text" id="username" name="username" class="form-control mb-2" placeholder="New Username">
            `,
                confirmButtonText: 'Change',
                focusConfirm: false,
                didOpen: () => {
                },
                preConfirm: () => {
                    const popup = Swal.getPopup();
                    const usernameEl = popup.querySelector('#username');
                    const username = usernameEl.value;
                    if (!username) {
                        Swal.showValidationMessage(`Please enter a new username.`);
                    }
                    return { userId, newUsername: username };
                },
            });

            const data = result.value;
            let reqData = JSON.stringify(data);

            swalLoadingModal("Changing username");

            const response = await fetch(`/api/settings/administration/change-user-name`, {
                method: 'post',
                headers: {
                    "Content-Type": "application/json",
                },
                body: reqData
            });

            Swal.close();

            let success = response.status == 200;

            if (success) {
                alert('Username changed');
                document.querySelector(`.lbl--username[user-id="${userId}"]`).innerHTML = data.newUsername;
            } else {
                alert('Could not add user identity.');
            }
        });
    });

document.getElementById('btn--add--user')
    ?.addEventListener('click', async (event) => {

        let result = await Swal.fire({
            title: 'Add User',
            html: `
                <input type="text" id="username" name="username" class="form-control mb-2" placeholder="New Username">
                <input type="text" id="password" name="password" class="form-control mb-2" placeholder="Password">
                <input type="text" id="realName" name="realName" class="form-control mb-2" placeholder="Real Name">
                <input type="text" id="email" name="email" class="form-control mb-2" placeholder="Email Address">
            `,
            confirmButtonText: 'Add',
            focusConfirm: false,
            didOpen: () => {
            },
            preConfirm: () => {
                const popup = Swal.getPopup();
                const usernameEl = popup.querySelector('#username');
                const username = usernameEl.value;
                const realNameEl = popup.querySelector('#realName');
                const realName = realNameEl.value;
                const emailEl = popup.querySelector('#email');
                const email = emailEl.value;
                const passwordEl = popup.querySelector('#password');
                const password = passwordEl.value;
                if (!username || !realName || !email || !password) {
                    Swal.showValidationMessage(`Please enter a username, real name and email.`);
                }
                return { username, realName, email, password };
            },
        });

        const data = result.value;
        let reqData = JSON.stringify(data);

        swalLoadingModal("Adding user");

        const response = await fetch(`/api/settings/administration/add-user`, {
            method: 'post',
            headers: {
                "Content-Type": "application/json",
            },
            body: reqData
        });

        Swal.close();

        let success = response.status == 200;

        if (success) {
            alert('User Added');
            window.location.href = window.location.href;
        } else {
            alert('Could not add user identity.');
        }
    });