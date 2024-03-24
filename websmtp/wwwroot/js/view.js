"use strict";

var msgId = document.getElementById('msg--id').value;

document.getElementById('btn--close')
    ?.addEventListener("click", () => {
        closeMsg();
    });
document.getElementById('btn--previous')
    ?.addEventListener("click", () => {
        previousMsg();
    });

document.getElementById('btn--next')
    ?.addEventListener("click", () => {
        nextMsg();
    });

document.getElementById('btn--raw')
    ?.addEventListener("click", () => {
        showRawMsg();
    });

document.getElementById('btn--delete')
    ?.addEventListener("click", () => {
        deleteMsg();
    });

document.getElementById('btn--undelete')
    ?.addEventListener("click", () => {
        undeleteMsg();
    });

document.getElementById('btn--read')
    ?.addEventListener("click", () => {
        markAsRead();
    });

document.getElementById('btn--unread')
    ?.addEventListener("click", () => {
        markAsUnread();
    });

document.getElementById('btn--report--spam')
    ?.addEventListener("click", () => {
        trainSpam(true);
    });
document.getElementById('btn--unreport--spam')
    ?.addEventListener("click", () => {
        trainSpam(false);
    });
document.getElementById('btn--reply')
    ?.addEventListener("click", (event) => {
        const to = event.currentTarget.getAttribute('reply-to');
        window.parent.newMessage(to);
    });

function showHtmlContent(el) {
    var el = document.getElementById("html-content");
    let isVisible = el.style.display == 'block';
    let visibility = isVisible ? 'none' : 'block';
    el.style.display = visibility;
}

function showSource(el) {
    var el = document.getElementById("source-content");
    let isVisible = el.style.display == 'block';
    let visibility = isVisible ? 'none' : 'block';
    el.style.display = visibility;
}
function closeMsg() {
    history.back();
}
async function deleteMsg() {
    await window.parent.deleteMessages([msgId]);
    document.getElementById('btn--undelete').classList.remove('d-none');
    document.getElementById('btn--delete').classList.add('d-none');
}
async function undeleteMsg() {
    await window.parent.undeleteMessages([msgId]);
    document.getElementById('btn--undelete').classList.add('d-none');
    document.getElementById('btn--delete').classList.remove('d-none');
}
async function markAsRead() {
    await window.parent.markMessagesAsRead([msgId]);
    document.getElementById('btn--unread').classList.remove('d-none');
    document.getElementById('btn--read').classList.add('d-none');
}
async function markAsUnread() {
    await window.parent.markMessagesAsUnread([msgId]);
    document.getElementById('btn--unread').classList.add('d-none');
    document.getElementById('btn--read').classList.remove('d-none');
}

async function trainSpam(isSpam) {
    await window.parent.trainSpam([msgId], isSpam);
    if (isSpam) {
        document.getElementById('btn--report--spam').classList.add('d-none');
        document.getElementById('btn--unreport--spam').classList.remove('d-none');
        document.getElementById('label--is--spam').classList.remove('d-none');
        document.getElementById('label--not--spam').classList.add('d-none');
    } else {
        document.getElementById('btn--report--spam').classList.remove('d-none');
        document.getElementById('btn--unreport--spam').classList.add('d-none');
        document.getElementById('label--is--spam').classList.add('d-none');
        document.getElementById('label--not--spam').classList.remove('d-none');
    }
}

function previousMsg() {
    window.parent.previousMessage(msgId);
}
function nextMsg() {
    window.parent.nextMessage(msgId);
}
function showRawMsg() {
    window.parent.openRawMsg(msgId);
}

var viewResizeInterval = setInterval(() => {
    let height = document.documentElement.scrollHeight;
    if (height == 0) return;
    window.parent.document.querySelector('#msg-view').style.height = height + 'px';
}, 100);

var innerViewResizeInterval = setInterval(() => {
    let iframeEl = document.getElementById('html-content');
    if (iframeEl == null) return;
    let height = iframeEl.contentDocument.documentElement.scrollHeight;
    if (height == 0) return;
    iframeEl.height = height;
}, 100);

document.addEventListener('unload', () => {
    console.log('clearing intervals');
    clearInterval(viewResizeInterval);
    clearInterval(innerViewResizeInterval);
});