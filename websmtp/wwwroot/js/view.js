var msgId = document.getElementById('msg--id').value;

document.getElementById('btn--close')
    .addEventListener("click", () => {
        closeMsg();
    });
document.getElementById('btn--previous')
    .addEventListener("click", () => {
        previousMsg();
    });

document.getElementById('btn--next')
    .addEventListener("click", () => {
        nextMsg();
    });

document.getElementById('btn--raw')
    .addEventListener("click", () => {
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
    window.parent.postMessage(`close-msg:_`, window.location.origin);
}
function deleteMsg() {
    window.parent.postMessage(`delete-msg:${msgId}`, window.location.origin);
}
function undeleteMsg() {
    window.parent.postMessage(`undelete-msg:${msgId}`, window.location.origin);
}
function previousMsg() {
    window.parent.postMessage(`previous-msg:${msgId}`, window.location.origin);
}
function nextMsg() {
    window.parent.postMessage(`next-msg:${msgId}`, window.location.origin);
}
function showRawMsg() {
    window.parent.postMessage(`raw-msg:${msgId}`, window.location.origin);
    //document.location.href = document.location.href + '&showRaw=true';
}

var viewResizeInterval = setInterval(() => window.parent.postMessage(`set-size:${document.documentElement.scrollHeight}`, window.location.origin), 100);

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