"use strict";

document.getElementById('new--message-expand-btn')
    ?.addEventListener("click", () => {
        expandWindow();
    });
document.getElementById('new--message-close-btn')
    ?.addEventListener("click", () => {
        window.parent.closeNewMsgWindow();
    });
document.getElementById('new--message-form')
    ?.addEventListener("submit", (event) => {
        saveMessage();
        onFormSubmit(event);
    });
document.getElementById('show--cc')
    ?.addEventListener("click", () => {
        console.log('show-cc');
        showCC();
    });
document.getElementById('show--bcc')
    ?.addEventListener("click", () => {
        showBCC();
    });

document.getElementById('new--msg--close')
    ?.addEventListener("click", () => {
        deleteMessageBackup();
        window.parent.setNewMessageCleanState();
        window.parent.closeNewMsgWindow();
    });

document.querySelectorAll('.identity--dropdown--value')
    .forEach(el => {
        el.addEventListener("click", (event) => {
            let idId = event.currentTarget.getAttribute('identity-id'),
                idString = event.currentTarget.innerHTML;
            document.getElementById('identityId').value = idId;
            document.getElementById('btn--identity--dropdown').innerHTML = idString;
        });
    });

document.querySelectorAll('#to, #subject, #body')
    .forEach(el => el.addEventListener('change', (event) => {
        console.log('change', event);
        window.parent.setNewMessageDirtyState();
    }));

const toolbarOptions = [

    [{ 'font': [] }],


    ['bold', 'italic', 'underline', 'strike'],        // toggled buttons
    ['code-block', 'link', 'image'],

    [{ 'list': 'ordered' }, { 'list': 'bullet' }, { 'list': 'check' }],
    //[{ 'script': 'sub'}, { 'script': 'super' }],      // superscript/subscript
    //[{ 'indent': '-1'}, { 'indent': '+1' }],          // outdent/indent
    [{ 'header': [1, 2, 3, 4, 5, 6] }],
    //[{ 'size': ['small', false, 'large', 'huge'] }],  // custom dropdown

    [{ 'color': [] }, { 'background': [] }],          // dropdown with defaults from theme

    [{ 'direction': 'rtl' }],                         // text direction
    ['clean']                                      // remove formatting button

];
document.getElementById('html')
    ?.addEventListener("click", () => {
        let isHtml = document.getElementById('html').checked == true;
        if (isHtml) {
            let bodyEl = document.getElementById('body');
            let oldValue = bodyEl.value;
            bodyEl.parentElement.removeChild(bodyEl);
            let subjectRowEl = document.getElementById('subject-row');
            subjectRowEl.insertAdjacentHTML('afterend', `<div id="quill">${oldValue}</div>`);
            subjectRowEl.insertAdjacentHTML('afterend', `<input type="hidden" id="body" name="body" value="${oldValue}" />`);
            quill = new Quill("#quill", {
                theme: "snow",
                modules: {
                    toolbar: toolbarOptions
                }
            });
            quill.on('text-change', function (delta, oldDelta, source) {
                document.getElementById('body').value = quill.container.firstChild.innerHTML;
            });
        } else {
            destroy_quill(quill);
        }
    });

function destroy_quill() {
    quill.theme.modules.toolbar.container.parentElement.removeChild(quill.theme.modules.toolbar.container);
    let bodyEl = document.getElementById('body');
    let oldValue = bodyEl.textContent;
    console.log(oldValue);
    bodyEl.parentElement.removeChild(bodyEl);
    // set inner html
    let subjectRowEl = document.getElementById('subject-row');
    // let newBodyEl = document.createElement('textarea');
    // newBodyEl.id = 'body';
    // newBodyEl.name = 'body';
    // newBodyEl.className = 'form-control my-2 flex-1-1-100p';
    // newBodyEl.setAttribute('rows', '6');
    let textAreaHtml = `<textarea id="body" name="body" class="form-control my-2 flex-1-1-100p" rows="6">${oldValue}</textarea>`;
    subjectRowEl.insertAdjacentHTML('afterend', textAreaHtml);

}

function closeWindow() {
    window.parent.closeNewMsgWindow();
}
function saveAndCloseDraft() {
    window.parent.closeNewMsgWindow();
}
function onFormSubmit(event) {
    let composerEl = document.getElementById('new--message-body');
    let loadingEl = document.getElementById('new--message-sending');
    composerEl.classList.add('d-none');
    loadingEl.classList.remove('d-none');
    let iframeEl = window.parent.document.getElementById('new--message-frame');
    iframeEl.style.height = '0px';
}
function saveMessage() {
    let formEls = document.querySelectorAll("#to, #cc, #bcc, #subject, #body");
    let data = {};

    formEls.forEach(el => {
        data[el.id] = el.value;
    });
    localStorage.setItem('previous-draft', JSON.stringify(data));
}
function deleteMessageBackup() {
    localStorage.setItem('previous-draft', null);
}
function restoreDraft() {
    let formEls = document.querySelectorAll("#to, #cc, #bcc, #subject, #body");
    let rawStorage = localStorage.getItem('previous-draft');
    let data = JSON.parse(rawStorage);
    console.log('rawStorage:' + rawStorage);

    const changeEvent = new Event("change");
    if (data != null) {
        for (const field in data) {
            document.getElementById(field).value = data[field];
            document.getElementById(field).dispatchEvent(changeEvent);
        }
    }
}
function showCC() {
    let ccRowEl = document.getElementById('cc-row');
    let showCCEl = document.getElementById('show--cc');
    showCCEl.classList.add('d-none');
    ccRowEl.classList.remove('d-none');
}
function showBCC() {
    //cc-row
    let ccRowEl = document.getElementById('bcc-row');
    let showCCEl = document.getElementById('show--bcc');
    showCCEl.classList.add('d-none');
    ccRowEl.classList.remove('d-none');
}
function expandWindow() {
    event.preventDefault();
    let sectionEl = window.parent.document.getElementById('new--message');
    let isExpanded = sectionEl.classList.contains('expanded');
    if (isExpanded) {
        sectionEl.classList.remove('expanded');
    } else {
        sectionEl.classList.add('expanded');
    }
}
async function resizeIframe() {
    let sectionEl = window.parent.document.getElementById('new--message');
    let iframeEl = window.parent.document.getElementById('new--message-frame');
    if (sectionEl.classList.contains('expanded')) {
        iframeEl.style.height = '100%';
    } else {
        iframeEl.style.height = document.documentElement.scrollHeight + 'px';
    }
    requestAnimationFrame(resizeIframe);
}



requestAnimationFrame(resizeIframe);

// const isResultDisplay = document.querySelectorAll('.new--message--dialog').length > 0;
// if(!isResultDisplay){
//     restoreDraft();
// }
//setInterval(resizeIframe, 10);
