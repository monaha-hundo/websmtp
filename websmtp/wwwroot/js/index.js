
window.addEventListener("popstate", (event) => {
    closeMsgView();
});

let openMsgViewBtn = document.querySelectorAll('[open-msg-view]');
openMsgViewBtn.forEach(btn => {
    btn.addEventListener("click", (event) => {
        let msgId = btn.getAttribute('open-msg-view');
        openwMsgView(msgId);
    });    
});

//new--msg--btn
let newMsgBtn = document.getElementById('new--msg--btn');
newMsgBtn.addEventListener("click", () => {
    newMessage();
});

function initNavbar() {
    let inbox = window.location.href.endsWith('/inbox');
    let all = window.location.href.endsWith('/all');
    let spam = window.location.href.endsWith('/spam');
    let trash = window.location.href.endsWith('/trash');

    if (trash) {
        const selector = `#btn-mailbox-trash`;
        const mailboxEl = document.querySelector(selector);
        mailboxEl.classList.remove('btn-transparent-primary');
        mailboxEl.classList.add('btn-dark', 'active');
        return;
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
    event.preventDefault();

    markMessageAsRead(msgId);
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

function closeMsgView() {
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
}

async function markMessageAsRead(msgId) {
    const response = await fetch(`/api/messages/${msgId}/mark-as-read/`, {
        method: 'post'
    });
    const success = response.status == 200;
    if (success) {
        const selector = `[msg-id='${msgId}']`;
        const checkMarkEl = document.querySelector(selector);
        checkMarkEl.classList.remove('unread');
    }
}

function updateTrashCount(count) {
    if (count == null) count = 1;
    const selector = 'sidebar--trash--count';
    const trashCountEl = document.getElementById(selector);
    const currentCount = parseInt(trashCountEl.innerText);
    trashCountEl.innerText = currentCount + 1;
}

async function undeleteMessage(msgId) {
    const call = async () => {
        const response = await fetch(`/api/messages/${msgId}/undelete/`, {
            method: 'post'
        });
        const success = response.status == 200;
        if (success) {
            const selector = `[msg-id='${msgId}']`;
            const checkMarkEl = document.querySelector(selector);
            checkMarkEl.parentElement.removeChild(checkMarkEl);
            updateTrashCount(-1);
            //closeMsgView();
            let msgViewEl = document.getElementById('msg-view');
            let url = msgViewEl.getAttribute('src');
            msgViewEl.setAttribute('src', url);
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


async function deleteMessage(msgId) {
    const call = async () => {
        const response = await fetch(`/api/messages/${msgId}/delete/`, {
            method: 'post'
        });
        const success = response.status == 200;
        if (success) {
            const selector = `[msg-id='${msgId}']`;
            const checkMarkEl = document.querySelector(selector);
            checkMarkEl.parentElement.removeChild(checkMarkEl);
            updateTrashCount(1);
            closeMsgView();
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

function handleMessage(event) {
    if (event.origin != window.location.origin) { return; }
    let msgType = event.data.split(':')[0];
    let msgParam = event.data.split(':')[1];
    switch (msgType) {
        case 'set-size':
            document.querySelector('#msg-view').style.height = msgParam + 'px';
            return;
        case 'close-msg':
            //closeMsgView();
            history.back();
            return;
        case 'undelete-msg':
            undeleteMessage(msgParam);
            return;
        case 'delete-msg':
            deleteMessage(msgParam);
            return;
        case 'previous-msg':
            previousMessage(msgParam);
            return;
        case 'next-msg':
            nextMessage(msgParam);
            return;
        case 'raw-msg':
            closeMsgView();
            openwMsgView(msgParam, true);
            return;
    }
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

window.addEventListener('message', handleMessage, false);
initNavbar();