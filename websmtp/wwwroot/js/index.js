"use strict";
var newMessageDirty = false;

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
    let inbox = window.location.pathname.startsWith('/inbox');
    let all = window.location.pathname.startsWith('/all');
    let favorites = window.location.pathname.startsWith('/favorites');
    let sent = window.location.pathname.startsWith('/sent');
    let spam = window.location.pathname.startsWith('/spam');
    let trash = window.location.pathname.startsWith('/trash');
    let account = window.location.pathname.startsWith('/account');
    let users = window.location.pathname.startsWith('/users');

    if (trash) {
        const selector = `#btn-mailbox-trash`;
        const mailboxEl = document.querySelector(selector);
        mailboxEl.classList.remove('btn-transparent-primary');
        mailboxEl.classList.add('btn-proton', 'active');
        return;
    }

    if (users) {
        const selector = `#btn-mailbox-users`;
        const mailboxEl = document.querySelector(selector);
        mailboxEl.classList.remove('btn-transparent-primary');
        mailboxEl.classList.add('btn-proton', 'active');
        return;
    }

    if (account) {
        const selector = `#btn-mailbox-account`;
        const mailboxEl = document.querySelector(selector);
        mailboxEl.classList.remove('btn-transparent-primary');
        mailboxEl.classList.add('btn-proton', 'active');
        return;
    }

    if (favorites) {
        const selector = `#btn-mailbox-fav`;
        const mailboxEl = document.querySelector(selector);
        mailboxEl.classList.remove('btn-transparent-primary');
        mailboxEl.classList.add('btn-proton', 'active');
    }

    if (sent) {
        const selector = `#btn-mailbox-sent`;
        const mailboxEl = document.querySelector(selector);
        mailboxEl.classList.remove('btn-transparent-primary');
        mailboxEl.classList.add('btn-proton', 'active');
    }

    if (inbox) {
        const selector = `#btn-mailbox-inbox`;
        const mailboxEl = document.querySelector(selector);
        mailboxEl.classList.remove('btn-transparent-primary');
        mailboxEl.classList.add('btn-proton', 'active');
    }

    if (all) {
        const selector = `#btn-mailbox-all`;
        const mailboxEl = document.querySelector(selector);
        mailboxEl.classList.remove('btn-transparent-primary');
        mailboxEl.classList.add('btn-proton', 'active');
    }

    if (spam) {
        const selector = `#btn-mailbox-spam`;
        const mailboxEl = document.querySelector(selector);
        mailboxEl.classList.remove('btn-transparent-primary');
        mailboxEl.classList.add('btn-proton', 'active');
    }
}
window.addEventListener('hashchange', (event) => {
    const { oldURL, newURL } = event;
    console.log({ oldUrl: oldURL, newURL, hash: window.location.hash });
    if (window.location.hash == "#index") {
        console.log('hash: index, closing message view');
        closeMsgView();
        return;
    } else if (window.location.hash.startsWith("#view-msg")) {
        let msgId = window.location.hash.split(':')[1];
        let raw = window.location.hash.split(':')[2] == 'raw';
        console.log('hash: view-msg, opening message view, raw:' + raw);
        closeMsgView();
        openwMsgView(msgId, raw, true);
    } else {
        console.log('hash: doing nothing');
    }
});
var previousListingScrollPos = 0;
async function openwMsgView(msgId, showRaw, pushState) {

    //markMessagesAsRead([msgId]);
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
}

function updateTrashCount(count) {
    if (count == null) count = 1;
    const selector = 'sidebar--trash--count';
    const trashCountEl = document.getElementById(selector);
    const currentCount = parseInt(trashCountEl.innerText);
    trashCountEl.innerText = currentCount + count;
}

function closeMsgView(pushState) {
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
    if (pushState) {
        //history.pushState({ page: 'index' }, '');
        window.location.hash = "index";
    }
}

async function markMessagesAsRead(msgsIds) {
    var success = false;
    const response = await fetch(`/api/messages/mark-as-read/`, {
        method: 'post',
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify(msgsIds)
    });
    success = response.status == 200;
    if (success) {
        for (let i = 0; i < msgsIds.length; i++) {
            const msgId = msgsIds[i];
            const selector = `[msg-id='${msgId}']`;
            const checkMarkEl = document.querySelector(selector);
            checkMarkEl.classList.remove('unread');
        }
    }
    return { marked: success };
}

async function markMessagesAsUnread(msgsIds) {
    var success = false;
    const response = await fetch(`/api/messages/mark-as-unread/`, {
        method: 'post',
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify(msgsIds)
    });
    success = response.status == 200;
    if (success) {
        for (let i = 0; i < msgsIds.length; i++) {
            const msgId = msgsIds[i];
            const selector = `[msg-id='${msgId}']`;
            const checkMarkEl = document.querySelector(selector);
            checkMarkEl.classList.add('unread');
        }
    }
    return { marked: success };
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

async function undeleteMessages(msgsIds) {
    var success = false;

    const call = async () => {
        const response = await fetch(`/api/messages/undelete/`, {
            method: 'post',
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(msgsIds)
        });
        success = response.status == 200;
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

    return { undeleted: success };
}

async function deleteMessages(msgsIds) {
    var success = false;

    const call = async () => {
        const response = await fetch(`/api/messages/delete/`, {
            method: 'post',
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(msgsIds)
        });
        success = response.status == 200;
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

    return { deleted: success };
}


async function trainSpam(msgsIds, spam) {
    const call = async () => {
        const response = await fetch(`/api/messages/train/`, {
            method: 'post',
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify({ msgsIds, spam })
        });
        const success = response.status == 200;
        if (!success) {
            Swal.fire({
                title: `Error`,
                text: 'Could not process request.'
            });
        }
        return success;
    };
    const title = spam ? "Report this message as spam?" : "Clear spam status from message?"

    const result = await Swal.fire({
        title,
        showCancelButton: true,
        confirmButtonText: "Yes",
        showLoaderOnConfirm: true,
        preConfirm: call,
        allowOutsideClick: () => !Swal.isLoading()
    });

    return result;
}

function previousMessage(msgId) {
    const selector = `[msg-id='${msgId}']`;
    const checkMarkEl = document.querySelector(selector);
    const prevMsgId = checkMarkEl.previousElementSibling.getAttribute('msg-id');
    if (prevMsgId == null) return;
    closeMsgView();
    //openwMsgView(prevMsgId, false, true);
    window.location.hash = "#view-msg:" + prevMsgId;
}

function nextMessage(msgId) {
    const selector = `[msg-id='${msgId}']`;
    const checkMarkEl = document.querySelector(selector);
    const prevMsgId = checkMarkEl.nextElementSibling.getAttribute('msg-id');
    closeMsgView();
    //openwMsgView(prevMsgId, false, true);
    window.location.hash = "#view-msg:" + prevMsgId;
}

function openRawMsg(msgId) {
    closeMsgView();
    //openwMsgView(msgId, true, false);
    window.location.hash = "#view-msg:" + msgId + ':raw';
}

async function newMessage(to) {
    let cancel = false;
    try {
        cancel = await closeNewMsgWindow();
    } catch (error) {

    }
    if (cancel) return;

    let sectionEl = window.parent.document.getElementById('new--message');
    let iframeEl = document.createElement('iframe');
    iframeEl.id = 'new--message-frame';

    if (to == null) {
        iframeEl.src = '/NewMessage';
    } else {
        iframeEl.src = '/NewMessage?initialTo=' + to;
    }
    sectionEl.appendChild(iframeEl);
    sectionEl.classList.remove('expanded');
    sectionEl.classList.remove('d-none');
}

function setNewMessageDirtyState() {
    newMessageDirty = true;
}
function setNewMessageCleanState() {
    newMessageDirty = false;
}


///
/// If new message windows has changed (e.g. is dirty), 
/// then prompt the user before closing it.
///
/// Return false if the user chose to keep the current message.
async function closeNewMsgWindow() {
    let closeIt = () => {
        let sectionEl = document.getElementById('new--message');
        if (sectionEl?.firstChild != null) {
            sectionEl.removeChild(sectionEl.firstChild);
            sectionEl.classList.add('d-none');
        }
        newMessageDirty = false;
        Swal.close();
        return true;
    };
    if (!newMessageDirty) {
        closeIt();
        return false;
    }
    const result = await Swal.fire({
        text: "You will loose the currently unsent message, proceed?",
        showCancelButton: true,
        confirmButtonText: "Close",
        cancelButtonText: "Keep",
        showLoaderOnConfirm: false,
        preConfirm: closeIt,
        allowOutsideClick: () => !Swal.isLoading()
    });
    return result.isDismissed;
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
        return;
    };
    event.preventDefault();
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
        return;
    };
    event.preventDefault();
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

async function loadSidebarStats() {
    var success = false;
    const response = await fetch(`/api/messages/stats/`, {
        method: 'post',
    });
    success = response.status == 200;
    if (success) {
        const data = await response.json();
        document.getElementById('mailbox-inbox-count').innerText = data.inbox;
        document.getElementById('mailbox-all-count').innerText = data.all;
        document.getElementById('mailbox-favs-count').innerText = data.favs;
        document.getElementById('mailbox-spam-count').innerText = data.spam;
        document.getElementById('mailbox-trash-count').innerText = data.trash;
        document.getElementById('mailbox-inbox-count').classList.remove('d-none');
        document.getElementById('mailbox-all-count').classList.remove('d-none');
        document.getElementById('mailbox-favs-count').classList.remove('d-none');
        document.getElementById('mailbox-spam-count').classList.remove('d-none');
        document.getElementById('mailbox-trash-count').classList.remove('d-none');

        if (parseInt(data.inbox) > 0) {
            document.getElementById('mailbox-inbox-count').classList.add('text-bg-primary');
            document.getElementById('mailbox-inbox-count').classList.remove('text-bg-dark');
        }
        if (data.allHasNew) {
            document.getElementById('mailbox-all-count').classList.add('text-bg-primary');
            document.getElementById('mailbox-all-count').classList.remove('text-bg-dark');
        }
        if (data.spamHasNew) {
            document.getElementById('mailbox-spam-count').classList.add('text-bg-primary');
            document.getElementById('mailbox-spam-count').classList.remove('text-bg-dark');
        }
        if (data.trashHasNew) {
            document.getElementById('mailbox-trash-count').classList.add('text-bg-primary');
            document.getElementById('mailbox-trash-count').classList.remove('text-bg-dark');
        }
    }
    return { marked: success };
}


document.querySelectorAll('[open-msg-view]')
    .forEach(btn => {
        btn.addEventListener("click", (event) => {
            let msgId = btn.getAttribute('open-msg-view');
            //openwMsgView(msgId, false, true);
            window.location.hash = "view-msg:" + msgId;
        });
    });

document.querySelectorAll('[delete-msg-id]')
    .forEach(btn => {
        btn.addEventListener("click", async (event) => {
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
    ?.addEventListener("click", async () => {
        newMessage();
    });

document.getElementById('msg--list-checkbox_all')
    ?.addEventListener("click", async (event) => {
        event.preventDefault();
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
window.location.hash = "index";
loadSidebarStats();