async function swalLoadingModal(title) {
    Swal.fire({
        title,
        didOpen: () => {
            Swal.showLoading();
        },
    });
}

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