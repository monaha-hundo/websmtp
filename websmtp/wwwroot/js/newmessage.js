document.getElementById('new--message-expand-btn')
    ?.addEventListener("click", () => {
        expandWindow();
    });
document.getElementById('new--message-close-btn')
    ?.addEventListener("click", () => {
        saveAndCloseDraft();
    });
document.getElementById('new--message-form')
    ?.addEventListener("formSubmit", () => {
        onFormSubmit();
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
        closeWindow();
    });
document.getElementById('new--msg--back')
    ?.addEventListener("click", () => {
        history.back()
    });


const toolbarOptions = [

    [{ 'font': [] }],


    ['bold', 'italic', 'underline', 'strike'],        // toggled buttons
    ['code-block', 'link', 'image'],
    [{ 'align': [] }],

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
            quill.on('text-change', function(delta, oldDelta, source) {
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
    event.preventDefault();
    let sectionEl = window.parent.document.getElementById('new--message');
    let iframeEl = window.parent.document.getElementById('new--message-frame');
    sectionEl.classList.add('d-none');
    iframeEl.src = 'about:blank';
}
function saveAndCloseDraft() {
    event.preventDefault();
    let sectionEl = window.parent.document.getElementById('new--message');
    let iframeEl = window.parent.document.getElementById('new--message-frame');
    sectionEl.classList.add('d-none');
    iframeEl.src = 'about:blank';
}
function onFormSubmit() {
    //event.preventDefault();
    //console.log('form submit');

    saveMessage();

    let composerEl = document.getElementById('new--message-body');
    let loadingEl = document.getElementById('new--message-sending');
    composerEl.classList.add('d-none');
    loadingEl.classList.remove('d-none');
    let iframeEl = window.parent.document.getElementById('new--message-frame');
    iframeEl.style.height = '0px';
}
function saveMessage() {
    let formEls = document.querySelectorAll("#fromEmail, #fromName, #to, #cc, #bcc, #subject, #body");
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
    let formEls = document.querySelectorAll("#fromEmail, #fromName, #to, #cc, #bcc, #subject, #body");
    let rawStorage = localStorage.getItem('previous-draft');
    let data = JSON.parse(rawStorage);

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



resizeIframe();
restoreDraft();
//setInterval(resizeIframe, 10);
