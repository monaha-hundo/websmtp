"using strict";

function updateSelectedMessages() {
    let multiSelectActionsEl = document.getElementById('multiple--selection');
    let selectedMsgEls = [...document.querySelectorAll('[id^=msg--list-checkbox_]')]
        .filter(el => el.getAttribute('checked') === 'true');
    if (selectedMsgEls.length > 0) {
        multiSelectActionsEl.classList.remove('d-none');
    } else {
        multiSelectActionsEl.classList.add('d-none');
    }
}

function initNavbar() {
    let inbox = window.location.href.endsWith('/inbox');
    let all = window.location.href.endsWith('/all');
    let favorites = window.location.href.endsWith('/favorites');
    let sent = window.location.href.endsWith('/sent');
    let spam = window.location.href.endsWith('/spam');
    let trash = window.location.href.endsWith('/trash');

    if (trash) {
        const selector = `#btn-mailbox-trash`;
        const mailboxEl = document.querySelector(selector);
        mailboxEl.classList.remove('btn-transparent-primary');
        mailboxEl.classList.add('btn-dark', 'active');
        return;
    }

    if (favorites) {
        const selector = `#btn-mailbox-fav`;
        const mailboxEl = document.querySelector(selector);
        mailboxEl.classList.remove('btn-transparent-primary');
        mailboxEl.classList.add('btn-dark', 'active');
    }

    if (sent) {
        const selector = `#btn-mailbox-sent`;
        const mailboxEl = document.querySelector(selector);
        mailboxEl.classList.remove('btn-transparent-primary');
        mailboxEl.classList.add('btn-dark', 'active');
    }

    if (inbox) {
        const selector = `#btn-mailbox-inbox`;
        const mailboxEl = document.querySelector(selector);
        mailboxEl.classList.remove('btn-transparent-primary');
        mailboxEl.classList.add('btn-dark', 'active');
    }

    if (all) {
        const selector = `#btn-mailbox-all`;
        const mailboxEl = document.querySelector(selector);
        mailboxEl.classList.remove('btn-transparent-primary');
        mailboxEl.classList.add('btn-dark', 'active');
    }

    if (spam) {
        const selector = `#btn-mailbox-spam`;
        const mailboxEl = document.querySelector(selector);
        mailboxEl.classList.remove('btn-transparent-primary');
        mailboxEl.classList.add('btn-dark', 'active');
    }
}

var previousListingScrollPos = 0;
async function openwMsgView(msgId, showRaw) {
    event?.preventDefault();

    markMessagesAsRead([msgId]);
    let listEl = document.querySelector('.list');
    let listingEl = document.querySelector('.listing');

    previousListingScrollPos = listEl.scrollTop;
    listingEl.classList.add('d-none');

    let sectionEl = document.querySelector('#msg');
    let containerEl = document.querySelector('#msg-view-container');

    let msgViewEl = document.createElement('iframe');
    msgViewEl.id = 'msg-view';
    msgViewEl.style.width = '100%';
    msgViewEl.style.height = '100%';
    msgViewEl.style.backgroundColor = 'transparent';

    containerEl.appendChild(msgViewEl);

    let url = '/View?msgId=' + msgId;
    if (showRaw === true) {
        url = url + '&showRaw=true';
    }
    msgViewEl.setAttribute('src', url);
    sectionEl.classList.remove('d-none');
    containerEl.classList.remove('d-none');

    if (showRaw !== true) {
        history.pushState({ page: 'view', msgId }, "");
    }
}

function updateTrashCount(count) {
    if (count == null) count = 1;
    const selector = 'sidebar--trash--count';
    const trashCountEl = document.getElementById(selector);
    const currentCount = parseInt(trashCountEl.innerText);
    trashCountEl.innerText = currentCount + count;
}

function closeMsgView() {
    try {
        let msgViewEl = document.getElementById('msg-view');
        msgViewEl.parentElement.removeChild(msgViewEl);
        let sectionEl = document.querySelector('#msg');
        sectionEl.classList.add('d-none');
        let listingEl = document.querySelector('.listing');
        listingEl.classList.remove('d-none');
        let containerEl = document.querySelector('#msg-view-container');
        containerEl.classList.add('d-none');
        let listEl = document.querySelector('.list');
        listEl.scroll(0, previousListingScrollPos);
    } catch (ex) {
        console.info("Tried to close msg view, but no msg view found.");
    }
}

async function markMessagesAsRead(msgsIds) {
    const response = await fetch(`/api/messages/mark-as-read/`, {
        method: 'post',
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify(msgsIds)
    });
    const success = response.status == 200;
    if (success) {
        for (let i = 0; i < msgsIds.length; i++) {
            const msgId = msgsIds[i];
            const selector = `[msg-id='${msgId}']`;
            const checkMarkEl = document.querySelector(selector);
            checkMarkEl.classList.remove('unread');
        }
    }
}

async function markMessagesAsUnread(msgsIds) {
    const response = await fetch(`/api/messages/mark-as-unread/`, {
        method: 'post',
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify(msgsIds)
    });
    const success = response.status == 200;
    if (success) {
        for (let i = 0; i < msgsIds.length; i++) {
            const msgId = msgsIds[i];
            const selector = `[msg-id='${msgId}']`;
            const checkMarkEl = document.querySelector(selector);
            checkMarkEl.classList.add('unread');
        }
    }
}

//


async function starMessages(msgsIds) {
    const response = await fetch(`/api/messages/star/`, {
        method: 'post',
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify(msgsIds)
    });
    const success = response.status == 200;
    return success;
}

async function unstarMessages(msgsIds) {
    const response = await fetch(`/api/messages/unstar/`, {
        method: 'post',
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify(msgsIds)
    });
    const success = response.status == 200;
    return success;
}

//

async function undeleteMessages(msgsIds) {
    const call = async () => {
        const response = await fetch(`/api/messages/undelete/`, {
            method: 'post',
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(msgsIds)
        });
        const success = response.status == 200;
        if (success) {
            for (let i = 0; i < msgsIds.length; i++) {
                const msgId = msgsIds[i];
                const selector = `[msg-id='${msgId}']`;
                const checkMarkEl = document.querySelector(selector);
                checkMarkEl.parentElement.removeChild(checkMarkEl);
                updateTrashCount(-1);
            }
            //closeMsgView();
        } else {
            Swal.fire({
                title: `Error`,
                text: 'Could not process request.'
            });
        }
    };

    let result = await Swal.fire({
        title: "Restore message?",
        showCancelButton: true,
        confirmButtonText: "Yes",
        showLoaderOnConfirm: true,
        preConfirm: call,
        allowOutsideClick: () => !Swal.isLoading()
    });

}

async function deleteMessages(msgsIds) {
    const call = async () => {
        const response = await fetch(`/api/messages/delete/`, {
            method: 'post',
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(msgsIds)
        });
        const success = response.status == 200;
        if (success) {
            for (let i = 0; i < msgsIds.length; i++) {
                const msgId = msgsIds[i];
                const selector = `[msg-id='${msgId}']`;
                const checkMarkEl = document.querySelector(selector);
                checkMarkEl.parentElement.removeChild(checkMarkEl);
                updateTrashCount(1);
            }
            //closeMsgView();
        } else {
            Swal.fire({
                title: `Error`,
                text: 'Could not process request.'
            });
        }
    };

    let result = await Swal.fire({
        title: "Move message to trash?",
        showCancelButton: true,
        confirmButtonText: "Yes",
        showLoaderOnConfirm: true,
        preConfirm: call,
        allowOutsideClick: () => !Swal.isLoading()
    });

}

function previousMessage(msgId) {
    const selector = `[msg-id='${msgId}']`;
    const checkMarkEl = document.querySelector(selector);
    const prevMsgId = checkMarkEl.previousElementSibling.getAttribute('msg-id');
    closeMsgView();
    openwMsgView(prevMsgId);
}

function nextMessage(msgId) {
    const selector = `[msg-id='${msgId}']`;
    const checkMarkEl = document.querySelector(selector);
    const prevMsgId = checkMarkEl.nextElementSibling.getAttribute('msg-id');
    closeMsgView();
    openwMsgView(prevMsgId);
}

function openRawMsg(msgId) {
    closeMsgView();
    openwMsgView(msgId, true);
}

function newMessage(to) {
    let sectionEl = window.parent.document.getElementById('new--message');
    let iframeEl = window.parent.document.getElementById('new--message-frame');
    if (to == null) {
        iframeEl.src = '/NewMessage';
    } else {
        iframeEl.src = '/NewMessage?initialTo=' + to;
    }
    sectionEl.classList.remove('expanded');
    sectionEl.classList.remove('d-none');
}

const handleMarkSelectedAsRead = async () => {
    let selectedMsgIds = [...document.querySelectorAll('[id^=msg--list-checkbox_]:not(#msg--list-checkbox_all)')]
        .filter(el => el.getAttribute('checked') === 'true')
        .map(el => el.getAttribute('msg-id'));
    await markMessagesAsRead(selectedMsgIds);
};

const handleStarClick = async (event) => {
    if (event.type == 'keyup' &&
        (event.key != 'Enter' || event.key == 'Space')) {
        console.log('skipping keyup');
        return;
    };
    event.preventDefault();
    event.bubbles = false;
    let containerEl = event.currentTarget;
    let msgId = containerEl.id.replace('msg--list-star_', '');
    let isStared = containerEl.getAttribute('checked') === "true";
    let newStatus = !isStared;
    containerEl.setAttribute('checked', newStatus);
    if (newStatus) {
        if (await starMessages([msgId])) {
            containerEl.querySelectorAll('.bi.bi-star')[0].classList.add('d-none');
            containerEl.querySelectorAll('.bi.bi-star-fill')[0].classList.remove('d-none');
        }
    } else {
        if (await unstarMessages([msgId])) {
            containerEl.querySelectorAll('.bi.bi-star')[0].classList.remove('d-none');
            containerEl.querySelectorAll('.bi.bi-star-fill')[0].classList.add('d-none');
        }
    }
};

const handleCheckboxClick = (event) => {
    if (event.type == 'keyup' &&
        (event.key != 'Enter' || event.key == 'Space')) {
        console.log('skipping keyup');
        return;
    };
    event.preventDefault();
    event.bubbles = false;
    let containerEl = event.currentTarget;
    let isStared = containerEl.getAttribute('checked') === "true";
    let newStatus = !isStared;
    containerEl.setAttribute('checked', newStatus);
    if (newStatus) {
        containerEl.querySelectorAll('.bi.bi-square')[0].classList.add('d-none');
        containerEl.querySelectorAll('.bi.bi-check-square')[0].classList.remove('d-none');
    } else {
        containerEl.querySelectorAll('.bi.bi-square')[0].classList.remove('d-none');
        containerEl.querySelectorAll('.bi.bi-check-square')[0].classList.add('d-none');
    }
    updateSelectedMessages();
}

const handleMarkSelectedAsUnread = async () => {
    let selectedMsgIds = [...document.querySelectorAll('[id^=msg--list-checkbox_]:not(#msg--list-checkbox_all)')]
        .filter(el => el.getAttribute('checked') === 'true')
        .map(el => el.getAttribute('msg-id'));
    await markMessagesAsUnread(selectedMsgIds);
};
window.addEventListener("popstate", (event) => {
    closeMsgView();
});

document.querySelectorAll('[open-msg-view]')
    .forEach(btn => {
        btn.addEventListener("click", (event) => {
            let msgId = btn.getAttribute('open-msg-view');
            openwMsgView(msgId);
        });
    });

document.querySelectorAll('[delete-msg-id]')
    .forEach(btn => {
        btn.addEventListener("click", (event) => {
            let msgId = btn.getAttribute('delete-msg-id');
            deleteMessages([msgId]);
        });
    });

document.querySelectorAll('[undelete-msg-id]')
    .forEach(btn => {
        btn.addEventListener("click", (event) => {
            let msgId = btn.getAttribute('undelete-msg-id');
            undeleteMessages([msgId]);
        });
    });

document.querySelectorAll('[read-msg-id]')
    .forEach(btn => {
        btn.addEventListener("click", (event) => {
            let msgId = btn.getAttribute('read-msg-id');
            markMessagesAsRead([msgId]);
            btn.classList.add('d-none');
            let inverseBtnEl = document.querySelector(`[unread-msg-id="${msgId}"]`);
            inverseBtnEl.classList.remove('d-none');
        });
    });

document.querySelectorAll('[unread-msg-id]')
    .forEach(btn => {
        btn.addEventListener("click", (event) => {
            let msgId = btn.getAttribute('unread-msg-id');
            markMessagesAsUnread([msgId]);
            btn.classList.add('d-none');
            let inverseBtnEl = document.querySelector(`[read-msg-id="${msgId}"]`);
            inverseBtnEl.classList.remove('d-none');
        });
    });

document.getElementById('new--msg--btn')
    ?.addEventListener("click", () => {
        newMessage();
    });

document.getElementById('msg--list-checkbox_all')
    ?.addEventListener("click", async (event) => {
        event.preventDefault();
        event.bubbles = false;
        const clickEvent = new Event("click");
        let selectedMsgIds = [...document.querySelectorAll('[id^=msg--list-checkbox_]')]
            .filter(el => el.id != 'msg--list-checkbox_all');
        selectedMsgIds.forEach(el => el.dispatchEvent(clickEvent));
    });

document.getElementById('delete-selected')
    ?.addEventListener("click", async () => {
        let selectedMsgIds = [...document.querySelectorAll('[id^=msg--list-checkbox_]:not(#msg--list-checkbox_all)')]
            .filter(el => el.getAttribute('checked') === 'true')
            .map(el => el.getAttribute('msg-id'));
        await deleteMessages(selectedMsgIds);
    });

document.getElementById('mark-selected-as-read')
    ?.addEventListener("click", handleMarkSelectedAsRead);


document.getElementById('mark-selected-as-unread')
    ?.addEventListener("click", handleMarkSelectedAsUnread);

document.querySelectorAll('[id^=msg--list-checkbox_]')
    .forEach(el => {
        el.addEventListener("click", handleCheckboxClick);
        el.addEventListener("keyup", handleCheckboxClick);
    });


document.querySelectorAll('[id^=msg--list-star_]')
    .forEach(el => {
        el.addEventListener("click", handleStarClick);
        el.addEventListener("keyup", handleStarClick);
    });

document.querySelectorAll('.hamburger--btn')
    .forEach(el => {
        el.addEventListener("click", (event) => {
            let invisible = document.querySelector('.sidebar').classList.contains('d-none');
            if (invisible) {
                document.querySelector('.sidebar').classList.remove('d-none');
            } else {
                document.querySelector('.sidebar').classList.add('d-none');
            }
        });
    });


initNavbar();