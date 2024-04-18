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
    ?.addEventListener("click", async (event) => {
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
    window.parent.closeMsgView(true);
}
async function deleteMsg() {
    let { deleted } = await window.parent.deleteMessages([msgId]);
    if (!deleted) return;
    document.getElementById('btn--undelete').classList.remove('d-none');
    document.getElementById('btn--delete').classList.add('d-none');
}
async function undeleteMsg() {
    let { undeleted } = await window.parent.undeleteMessages([msgId]);
    if (!undeleted) return;
    document.getElementById('btn--undelete').classList.add('d-none');
    document.getElementById('btn--delete').classList.remove('d-none');
}
async function markAsRead() {
    let { marked } = await window.parent.markMessagesAsRead([msgId]);
    if (!marked) return;
    document.getElementById('btn--unread').classList.remove('d-none');
    document.getElementById('btn--read').classList.add('d-none');
}
async function markAsUnread() {
    let { marked } = await window.parent.markMessagesAsUnread([msgId]);
    if (!marked) return;
    document.getElementById('btn--unread').classList.add('d-none');
    document.getElementById('btn--read').classList.remove('d-none');
}

async function trainSpam(isSpam) {
    const result = await window.parent.trainSpam([msgId], isSpam);

    if (result.isDismissed || !result.value) return;

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

document.getElementById('html-content').contentWindow.addEventListener('DOMContentLoaded', () => {
    resizeIframe();
 });

function resizeIframe() {
    let iframeEl = document.getElementById('html-content');
    let height = iframeEl.contentDocument.documentElement.scrollHeight;
    iframeEl.height = height;
}
