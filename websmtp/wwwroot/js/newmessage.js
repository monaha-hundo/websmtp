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


        function makeMessageHtml(){
            // tinymce.init({
            //     selector: '#body',
            //     skin: 'polaris',
            //     promotion: false,
            //     branding: false,
            //     plugins: 'lists link emoticons image code',
            //     toolbar: 'undo redo | formatgroup | link emoticons image | code removeformat',

            //     toolbar_groups: {
            //         formatgroup: {
            //             icon: 'format',
            //             tooltip: 'Formatting',
            //             items: 'fontselect | formatselect | bold italic underline strikethrough forecolor | align bullist numlist outdent indent blockquote'
            //         }
            //     },
            //     forced_root_block: 'p',
            //     forced_root_block_attrs: { 'style': 'font-size: 14px; font-family: helvetica, arial, sans-serif;' },
            //     automatic_uploads: true,
            //     images_upload_handler: (blobInfo) => {
            //         const base64str =
            //             "data:" +
            //             blobInfo.blob().type +
            //             ";base64," +
            //             blobInfo.base64();
            //         return Promise.resolve(base64str);
            //     },
            //     file_picker_callback: (cb, value, meta) => {
            //         const input = document.createElement('input');
            //         input.setAttribute('type', 'file');
            //         input.setAttribute('accept', 'image/*');

            //         input.addEventListener('change', (e) => {
            //             const file = e.target.files[0];

            //             const reader = new FileReader();
            //             reader.addEventListener('load', () => {
            //                 /*
            //                   Note: Now we need to register the blob in TinyMCEs image blob
            //                   registry. In the next release this part hopefully won't be
            //                   necessary, as we are looking to handle it internally.
            //                 */
            //                 const id = 'blobid' + (new Date()).getTime();
            //                 const blobCache = tinymce.activeEditor.editorUpload.blobCache;
            //                 const base64 = reader.result.split(',')[1];
            //                 const blobInfo = blobCache.create(id, file, base64);
            //                 blobCache.add(blobInfo);

            //                 /* call the callback and populate the Title field with the file name */
            //                 cb(blobInfo.blobUri(), { title: file.name });
            //             });
            //             reader.readAsDataURL(file);
            //         });

            //         input.click();
            //     },
            // });
            
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